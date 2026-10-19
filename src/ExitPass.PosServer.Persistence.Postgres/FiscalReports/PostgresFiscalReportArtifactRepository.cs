using System.Data;
using System.Security.Cryptography;
using ExitPass.PosServer.Runtime.FiscalReports;
using Npgsql;
using NpgsqlTypes;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class PostgresFiscalReportArtifactRepository(
    NpgsqlDataSource dataSource,
    IFiscalReportArtifactStore store) : IFiscalReportArtifactRepository
{
    public async Task<FiscalReportArtifactResult> GetOrCreateAsync(
        FiscalReportArtifactCommand command,
        Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
        CancellationToken cancellationToken = default)
    {
        if (!Valid(command))
            return Failure(FiscalReportArtifactOutcome.InvalidRequest, "The fiscal report artifact request is incomplete or invalid.");
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await using (var advisory = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended(@key, 0))", connection, transaction))
            {
                advisory.Parameters.AddWithValue("key", $"fiscal-artifact:v1:{command.SitePosServerId:D}:{command.FiscalIdentityId:D}:{command.CurrencyCode}:{command.ReportType}:{command.ScopeReference}:{command.Format}");
                await advisory.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            var current = await ReadCurrentAsync(connection, transaction, command, cancellationToken).ConfigureAwait(false);
            if (current is not null && string.Equals(current.SourceFingerprint, command.SourceFingerprint, StringComparison.Ordinal))
            {
                var replay = await ReadVerifiedAsync(current, cancellationToken).ConfigureAwait(false);
                if (replay is null) return Failure(FiscalReportArtifactOutcome.IntegrityFailure, "The persisted fiscal report artifact failed integrity verification.");
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new(FiscalReportArtifactOutcome.Reused, current, replay);
            }

            var payload = await render(cancellationToken).ConfigureAwait(false);
            if (payload.Bytes.Length == 0 || string.IsNullOrWhiteSpace(payload.ContentType) || !ValidFileName(payload.FileName))
                return Failure(FiscalReportArtifactOutcome.InvalidRequest, "The generated fiscal report artifact is invalid.");
            var sha = Convert.ToHexString(SHA256.HashData(payload.Bytes)).ToLowerInvariant();
            if (current is not null && string.Equals(current.Sha256, sha, StringComparison.Ordinal) && current.ByteLength == payload.Bytes.LongLength)
            {
                var replay = await ReadVerifiedAsync(current, cancellationToken).ConfigureAwait(false);
                if (replay is null) return Failure(FiscalReportArtifactOutcome.IntegrityFailure, "The persisted fiscal report artifact failed integrity verification.");
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return new(FiscalReportArtifactOutcome.Reused, current, replay);
            }

            var storageKey = await store.PublishAsync(command.SitePosServerId, command.ReportType, command.Format, payload.Bytes, sha, cancellationToken).ConfigureAwait(false);
            var id = Guid.NewGuid();
            var revision = current is null ? 1 : checked(current.Revision + 1);
            const string sql = """
            INSERT INTO pos.fiscal_report_artifacts(
              fiscal_report_artifact_id,site_pos_server_id,fiscal_identity_id,currency_code,report_type,scope_reference,
              format,artifact_version,source_fingerprint,fiscal_reporting_period_id,calendar_year,calendar_month,business_day_date,
              revision,supersedes_artifact_id,artifact_storage_key,artifact_sha256,artifact_byte_length,mime_type,file_name,generated_at)
            VALUES(@id,@site,@identity,@currency,@type,@scope,@format,@version,@source,@period,@year,@month,@business,
              @revision,@supersedes,@storage,@sha,@length,@mime,@file,clock_timestamp())
            """;
            await using (var insert = new NpgsqlCommand(sql, connection, transaction))
            {
                insert.Parameters.AddWithValue("id", id);
                insert.Parameters.AddWithValue("site", command.SitePosServerId);
                AddNullable(insert, "identity", command.FiscalIdentityId, NpgsqlDbType.Uuid);
                insert.Parameters.AddWithValue("currency", command.CurrencyCode);
                insert.Parameters.AddWithValue("type", command.ReportType);
                insert.Parameters.AddWithValue("scope", command.ScopeReference);
                insert.Parameters.AddWithValue("format", command.Format);
                insert.Parameters.AddWithValue("version", command.ArtifactVersion);
                insert.Parameters.AddWithValue("source", command.SourceFingerprint);
                AddNullable(insert, "period", command.FiscalReportingPeriodId, NpgsqlDbType.Uuid);
                AddNullable(insert, "year", command.CalendarYear, NpgsqlDbType.Integer);
                AddNullable(insert, "month", command.CalendarMonth, NpgsqlDbType.Integer);
                AddNullable(insert, "business", command.BusinessDayDate, NpgsqlDbType.Date);
                insert.Parameters.AddWithValue("revision", revision);
                AddNullable(insert, "supersedes", current?.ArtifactId, NpgsqlDbType.Uuid);
                insert.Parameters.AddWithValue("storage", storageKey);
                insert.Parameters.AddWithValue("sha", sha);
                insert.Parameters.AddWithValue("length", payload.Bytes.LongLength);
                insert.Parameters.AddWithValue("mime", payload.ContentType);
                insert.Parameters.AddWithValue("file", payload.FileName);
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            var record = await ReadByIdAsync(connection, transaction, id, cancellationToken).ConfigureAwait(false);
            if (record is null) return Failure(FiscalReportArtifactOutcome.PersistenceUnavailable, "The persisted fiscal report artifact metadata could not be read back.");
            var bytes = await ReadVerifiedAsync(record, cancellationToken).ConfigureAwait(false);
            if (bytes is null) return Failure(FiscalReportArtifactOutcome.IntegrityFailure, "The persisted fiscal report artifact failed integrity verification.");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new(FiscalReportArtifactOutcome.Created, record, bytes);
        }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.UniqueViolation)
        {
            return Failure(FiscalReportArtifactOutcome.RetryableConcurrencyFailure, "Fiscal report artifact publication encountered a retryable concurrency conflict.");
        }
        catch (NpgsqlException) { return Failure(FiscalReportArtifactOutcome.PersistenceUnavailable, "Fiscal report artifact metadata persistence failed safely."); }
        catch (IOException) { return Failure(FiscalReportArtifactOutcome.PersistenceUnavailable, "Fiscal report artifact storage failed safely."); }
        catch (UnauthorizedAccessException) { return Failure(FiscalReportArtifactOutcome.PersistenceUnavailable, "Fiscal report artifact storage is not writable."); }
    }

    private async Task<byte[]?> ReadVerifiedAsync(FiscalReportArtifactRecord record, CancellationToken cancellationToken)
    {
        var bytes = await store.ReadAsync(record.StorageKey, cancellationToken).ConfigureAwait(false);
        if (bytes is null || bytes.LongLength != record.ByteLength) return null;
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return string.Equals(hash, record.Sha256, StringComparison.Ordinal) ? bytes : null;
    }

    private static async Task<FiscalReportArtifactRecord?> ReadCurrentAsync(NpgsqlConnection c, NpgsqlTransaction t, FiscalReportArtifactCommand q, CancellationToken ct)
    {
        const string sql = """
        SELECT fiscal_report_artifact_id,site_pos_server_id,fiscal_identity_id,currency_code,report_type,scope_reference,
               format,artifact_version,source_fingerprint,fiscal_reporting_period_id,calendar_year,calendar_month,business_day_date,
               revision,supersedes_artifact_id,artifact_storage_key,artifact_sha256,artifact_byte_length,mime_type,file_name,generated_at
        FROM pos.fiscal_report_artifacts
        WHERE site_pos_server_id=@site AND fiscal_identity_id IS NOT DISTINCT FROM @identity AND currency_code=@currency
          AND report_type=@type AND scope_reference=@scope AND format=@format
        ORDER BY revision DESC LIMIT 1 FOR UPDATE
        """;
        await using var cmd = new NpgsqlCommand(sql, c, t);
        cmd.Parameters.AddWithValue("site", q.SitePosServerId); AddNullable(cmd, "identity", q.FiscalIdentityId, NpgsqlDbType.Uuid);
        cmd.Parameters.AddWithValue("currency", q.CurrencyCode); cmd.Parameters.AddWithValue("type", q.ReportType);
        cmd.Parameters.AddWithValue("scope", q.ScopeReference); cmd.Parameters.AddWithValue("format", q.Format);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? Map(reader) : null;
    }

    private static async Task<FiscalReportArtifactRecord?> ReadByIdAsync(NpgsqlConnection c, NpgsqlTransaction t, Guid id, CancellationToken ct)
    {
        const string sql = """
        SELECT fiscal_report_artifact_id,site_pos_server_id,fiscal_identity_id,currency_code,report_type,scope_reference,
               format,artifact_version,source_fingerprint,fiscal_reporting_period_id,calendar_year,calendar_month,business_day_date,
               revision,supersedes_artifact_id,artifact_storage_key,artifact_sha256,artifact_byte_length,mime_type,file_name,generated_at
        FROM pos.fiscal_report_artifacts WHERE fiscal_report_artifact_id=@id
        """;
        await using var cmd = new NpgsqlCommand(sql, c, t); cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        return await reader.ReadAsync(ct).ConfigureAwait(false) ? Map(reader) : null;
    }

    private static FiscalReportArtifactRecord Map(NpgsqlDataReader r) => new(
        r.GetGuid(0), r.GetGuid(1), r.IsDBNull(2) ? null : r.GetGuid(2), r.GetString(3), r.GetString(4), r.GetString(5),
        r.GetString(6), r.GetString(7), r.GetString(8), r.IsDBNull(9) ? null : r.GetGuid(9), r.IsDBNull(10) ? null : r.GetInt32(10),
        r.IsDBNull(11) ? null : r.GetInt32(11), r.IsDBNull(12) ? null : r.GetFieldValue<DateOnly>(12), r.GetInt32(13),
        r.IsDBNull(14) ? null : r.GetGuid(14), r.GetString(15), r.GetString(16), r.GetInt64(17), r.GetString(18), r.GetString(19), r.GetFieldValue<DateTimeOffset>(20));

    private static bool Valid(FiscalReportArtifactCommand c) => c.SitePosServerId != Guid.Empty &&
        (c.FiscalIdentityId is null || c.FiscalIdentityId != Guid.Empty) && c.CurrencyCode.Length == 3 &&
        c.ReportType.Length is > 0 and <= 80 && c.ReportType.All(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_') &&
        c.ScopeReference.Length is > 0 and <= 500 && !c.ScopeReference.Any(char.IsControl) &&
        c.Format.Length is > 0 and <= 20 && c.Format.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_') &&
        c.ArtifactVersion.Length is > 0 and <= 200 && c.SourceFingerprint.Length == 64 &&
        c.SourceFingerprint.All(ch => ch is >= '0' and <= '9' or >= 'a' and <= 'f') &&
        c.CalendarYear is null or >= 2000 and <= 9999 && c.CalendarMonth is null or >= 1 and <= 12;

    private static bool ValidFileName(string value) => value.Length is > 0 and <= 255 &&
        value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !value.Contains('/') && !value.Contains('\\');
    private static FiscalReportArtifactResult Failure(FiscalReportArtifactOutcome outcome, string message) => new(outcome, SafeMessage: message);
    private static void AddNullable<T>(NpgsqlCommand command, string name, T? value, NpgsqlDbType type) where T : struct
    {
        var parameter = command.Parameters.Add(name, type); parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }
}
