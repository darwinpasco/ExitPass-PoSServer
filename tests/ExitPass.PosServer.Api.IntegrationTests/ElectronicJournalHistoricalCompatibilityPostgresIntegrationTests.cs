using System.Text.Json;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Npgsql;
using Xunit;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class ElectronicJournalHistoricalCompatibilityPostgresIntegrationTests
{
    private const string ConnectionVariable = "POSSERVER_EJ_HISTORICAL_COMPATIBILITY_DB_URL";
    private const string ExpectedDatabase = "exitpass_ej_v1_v2_mixed_validation";

    [Fact]
    public async Task TenRealV1RowsRemainUnchangedAndVerifyAcrossNewV2Boundary()
    {
        if (!TryGetDisposableConnectionString(out var connectionString)) return;

        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        var repository = new PostgresElectronicJournalRepository(dataSource);
        var query = new ElectronicJournalQuery(
            Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"),
            Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a"),
            "PHP");
        var beforeManifest = await HistoricalManifestAsync(connectionString);
        var before = await repository.ReadAsync(query);

        Assert.Equal(ElectronicJournalOutcome.Success, before.Outcome);
        Assert.NotNull(before.Page);
        Assert.Equal(10, before.Page.Events.Count);
        Assert.Equal(Enumerable.Range(1, 10).Select(value => (long)value),
            before.Page.Events.Select(value => value.StreamSequence));
        Assert.All(before.Page.Events, value =>
        {
            Assert.Equal(ElectronicJournalContract.LegacySemanticHashVersion, value.SemanticHashVersion);
            Assert.Equal(ElectronicJournalContract.IntegrityHashVersion, value.IntegrityHashVersion);
            Assert.Null(value.PrintableSalesInvoiceText);
        });
        var historicalIntegrity = await repository.VerifyIntegrityAsync(query);
        Assert.Equal(ElectronicJournalOutcome.Success, historicalIntegrity.Outcome);
        Assert.Equal(10, historicalIntegrity.Result?.VerifiedEventCount);

        var first = before.Page.Events[0];
        var historicalReplay = ToAppendRequest(first) with
        {
            PrintableSalesInvoiceText = "historical printable text must not be added"
        };
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            Assert.Equal(first.EventReference, await PostgresElectronicJournalWriter.AppendAsync(
                connection, transaction, historicalReplay, default));
            await transaction.CommitAsync();
        }
        Assert.Equal(beforeManifest, await HistoricalManifestAsync(connectionString));

        const string printableText = "SALES INVOICE\r\nSemantic v2 mixed-boundary proof\r\n";
        var current = ToAppendRequest(first) with
        {
            SourceTransitionReference = "semantic-version-proof:fiscal-document-committed:v2",
            IdempotencyReference = "semantic-version-proof:fiscal-document-committed:v2",
            CorrelationReference = "semantic-version-proof",
            EffectiveAt = DateTimeOffset.Parse("2026-09-08T02:00:00Z"),
            PrintableSalesInvoiceText = printableText,
            Facts = new SortedDictionary<string, string?>(
                first.Facts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
                StringComparer.Ordinal)
            {
                ["fiscal_document_number"] = "SI-00000001-V2-PROOF"
            }
        };
        string currentReference;
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            currentReference = await PostgresElectronicJournalWriter.AppendAsync(connection, transaction, current, default);
            Assert.Equal(currentReference, await PostgresElectronicJournalWriter.AppendAsync(connection, transaction, current, default));
            await transaction.CommitAsync();
        }

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            await Assert.ThrowsAsync<ElectronicJournalSemanticConflictException>(() =>
                PostgresElectronicJournalWriter.AppendAsync(connection, transaction,
                    current with { PrintableSalesInvoiceText = "changed printable text" }, default));
            await transaction.RollbackAsync();
        }

        var mixed = await repository.ReadAsync(query);
        Assert.Equal(ElectronicJournalOutcome.Success, mixed.Outcome);
        Assert.NotNull(mixed.Page);
        Assert.Equal(11, mixed.Page.Events.Count);
        Assert.Equal(10, mixed.Page.Events.Count(value =>
            value.SemanticHashVersion == ElectronicJournalContract.LegacySemanticHashVersion));
        var appended = Assert.Single(mixed.Page.Events, value =>
            value.SemanticHashVersion == ElectronicJournalContract.CurrentSemanticHashVersion);
        Assert.Equal(11, appended.StreamSequence);
        Assert.Equal(currentReference, appended.EventReference);
        Assert.Equal(printableText, appended.PrintableSalesInvoiceText);
        Assert.Equal(beforeManifest, await HistoricalManifestAsync(connectionString));

        var mixedIntegrity = await repository.VerifyIntegrityAsync(query);
        Assert.Equal(ElectronicJournalOutcome.Success, mixedIntegrity.Outcome);
        Assert.Equal(11, mixedIntegrity.Result?.VerifiedEventCount);
        Assert.Equal(appended.IntegrityHash, await ScalarStringAsync(connectionString,
            "SELECT last_event_hash FROM pos.electronic_journal_streams"));
        Assert.Equal(11, await ScalarLongAsync(connectionString,
            "SELECT last_sequence_value FROM pos.electronic_journal_streams"));
    }

    private static bool TryGetDisposableConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString)) return false;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.Equals(builder.Database, ExpectedDatabase, StringComparison.Ordinal) ||
            !string.Equals(builder.Host, "127.0.0.1", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{ConnectionVariable} must target only local disposable database {ExpectedDatabase}.");
        }
        return true;
    }

    private static async Task<string> HistoricalManifestAsync(string connectionString) =>
        await ScalarStringAsync(connectionString, """
            SELECT md5(string_agg(concat_ws('|',stream_sequence_value,semantic_hash_version,semantic_hash,
              integrity_hash_version,previous_integrity_hash,integrity_hash,
              printable_sales_invoice_text IS NULL), E'\n' ORDER BY stream_sequence_value))
            FROM pos.electronic_journal_records
            WHERE is_canonical AND stream_sequence_value <= 10
            """);

    private static ElectronicJournalAppendRequest ToAppendRequest(ElectronicJournalEvent value) => new(
        value.SitePosServerId, value.FiscalIdentityId, value.CurrencyCode, value.FiscalReportingPeriodId,
        value.EventType, value.SourceTransitionReference, value.SourceTransitionVersion, value.EffectiveAt,
        value.ActorReference, value.ServiceIdentityReference, value.CorrelationReference,
        new SortedDictionary<string, string?>(
            value.Facts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal), value.FiscalDocumentId,
        value.FiscalReportRequestId, value.XZReportId, value.BirSalesSummaryReportId,
        value.FiscalSequencePolicyId, value.BusinessDayDate, value.IdempotencyReference,
        value.ReprintRequestId, value.PrintableSalesInvoiceText);

    private static async Task<string> ScalarStringAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<long> ScalarLongAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
