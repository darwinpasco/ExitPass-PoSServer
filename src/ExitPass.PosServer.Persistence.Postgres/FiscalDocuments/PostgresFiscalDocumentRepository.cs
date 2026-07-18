using ExitPass.PosServer.Runtime.FiscalDocuments;
using Npgsql;
using NpgsqlTypes;
using System.Globalization;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

public sealed class PostgresFiscalDocumentRepository : IFiscalDocumentRepository
{
    private static readonly HashSet<string> VoidableStatusCodeKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "issued",
        "recorded"
    };

    private readonly NpgsqlDataSource dataSource;
    private readonly bool requireCompleteSalesInvoiceHeaderProfile;
    private readonly SalesInvoiceHeaderProfileService salesInvoiceHeaderProfileService = new();

    public PostgresFiscalDocumentRepository(
        NpgsqlDataSource dataSource,
        bool requireCompleteSalesInvoiceHeaderProfile = false)
    {
        this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        this.requireCompleteSalesInvoiceHeaderProfile = requireCompleteSalesInvoiceHeaderProfile;
    }

    public async Task<FiscalDocumentPersistenceResult> CreateAsync(
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await using var insertIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertIdempotencyRecord,
                    connection,
                    transaction);
                AddInsertIdempotencyParameters(insertIdempotencyCommand, draft, idempotency);
                await insertIdempotencyCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var selectIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate,
                    connection,
                    transaction);
                AddIdempotencyKeyParameters(selectIdempotencyCommand, idempotency);
                await using var idempotencyReader = await selectIdempotencyCommand
                    .ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!await idempotencyReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Fiscal document idempotency record was not available after insert.");
                }

                var existingSemanticHash = idempotencyReader.GetString(0);
                var linkedFiscalDocumentId = idempotencyReader.IsDBNull(1)
                    ? (Guid?)null
                    : idempotencyReader.GetGuid(1);
                await idempotencyReader.CloseAsync().ConfigureAwait(false);

                if (!string.Equals(existingSemanticHash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                {
                    throw new FiscalDocumentIdempotencyConflictException();
                }

                if (linkedFiscalDocumentId is not null)
                {
                    var replayedDraft = await ReadReplayDraftAsync(
                        connection,
                        transaction,
                        draft with { FiscalDocumentId = linkedFiscalDocumentId.Value },
                        cancellationToken).ConfigureAwait(false);
                    await using var replayCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.UpdateIdempotencyRecordReplay,
                        connection,
                        transaction);
                    AddIdempotencyCompletionParameters(replayCommand, replayedDraft, idempotency);
                    await replayCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return FiscalDocumentPersistenceResult.Replayed(replayedDraft);
                }

                var resolvedContext = await ResolveFiscalContextAsync(
                    connection,
                    transaction,
                    draft,
                    cancellationToken).ConfigureAwait(false);
                var resolvedDraft = draft with
                {
                    ResolvedFiscalIdentityId = resolvedContext.FiscalIdentityId,
                    ResolvedFiscalSequencePolicyId = resolvedContext.FiscalSequencePolicyId
                };
                var assignment = await AllocateFiscalNumberAsync(
                    connection,
                    transaction,
                    resolvedContext,
                    cancellationToken).ConfigureAwait(false);
                resolvedDraft = resolvedDraft with
                {
                    FiscalSequenceValue = assignment.FiscalSequenceValue,
                    FiscalDocumentNumber = assignment.FiscalDocumentNumber,
                    FiscalSeries = assignment.FiscalSeries,
                    FiscalNumberPrefixText = assignment.FiscalNumberPrefixText,
                    FiscalNumberSuffixText = assignment.FiscalNumberSuffixText,
                    FiscalNumberAssignedAt = assignment.FiscalNumberAssignedAt,
                    FiscalNumberAssignedByRef = assignment.FiscalNumberAssignedByRef
                };

                var headerSnapshot = await ResolveSalesInvoiceHeaderSnapshotAsync(
                    connection,
                    transaction,
                    resolvedDraft,
                    assignment.FiscalNumberAssignedAt,
                    cancellationToken).ConfigureAwait(false);
                resolvedDraft = resolvedDraft with { SalesInvoiceHeaderSnapshot = headerSnapshot };

                await using var documentCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertFiscalDocument,
                    connection,
                    transaction);
                AddFiscalDocumentParameters(documentCommand, resolvedDraft);
                await documentCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                if (resolvedDraft.SalesInvoiceHeaderSnapshot is not null)
                {
                    await using var snapshotCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDocumentHeaderSnapshot,
                        connection,
                        transaction);
                    AddHeaderSnapshotParameters(snapshotCommand, resolvedDraft);
                    await snapshotCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await using var statusHistoryCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertFiscalDocumentStatusHistory,
                    connection,
                    transaction);
                AddStatusHistoryParameters(statusHistoryCommand, resolvedDraft);
                await statusHistoryCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                foreach (var documentLink in resolvedDraft.DocumentLinks)
                {
                    await using var linkCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDocumentLink,
                        connection,
                        transaction);
                    AddDocumentLinkParameters(linkCommand, resolvedDraft, documentLink);
                    await linkCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                var lineIdsBySequence = new Dictionary<int, Guid>();
                foreach (var documentLine in resolvedDraft.DocumentLines)
                {
                    var fiscalDocumentLineId = Guid.NewGuid();
                    lineIdsBySequence.Add(documentLine.LineSequence, fiscalDocumentLineId);

                    await using var lineCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDocumentLine,
                        connection,
                        transaction);
                    AddDocumentLineParameters(lineCommand, resolvedDraft, documentLine, fiscalDocumentLineId);
                    await lineCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var tender in resolvedDraft.Tenders)
                {
                    await using var tenderCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTender,
                        connection,
                        transaction);
                    AddTenderParameters(tenderCommand, resolvedDraft, tender);
                    await tenderCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var taxDetail in resolvedDraft.TaxDetails)
                {
                    await using var taxCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTaxDetail,
                        connection,
                        transaction);
                    AddTaxDetailParameters(taxCommand, resolvedDraft, taxDetail, lineIdsBySequence);
                    await taxCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var discountPrivilegeDetail in resolvedDraft.DiscountPrivilegeDetails)
                {
                    await using var discountPrivilegeCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalDiscountPrivilegeDetail,
                        connection,
                        transaction);
                    AddDiscountPrivilegeDetailParameters(
                        discountPrivilegeCommand,
                        resolvedDraft,
                        discountPrivilegeDetail,
                        lineIdsBySequence);
                    await discountPrivilegeCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                foreach (var total in resolvedDraft.Totals)
                {
                    await using var totalCommand = new NpgsqlCommand(
                        PostgresFiscalDocumentSql.InsertFiscalTotal,
                        connection,
                        transaction);
                    AddTotalParameters(totalCommand, resolvedDraft, total);
                    await totalCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await using var sequenceStateCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.UpdateFiscalSequenceStateIssued,
                    connection,
                    transaction);
                AddSequenceStateUpdateParameters(sequenceStateCommand, assignment);
                await sequenceStateCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var completeIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted,
                    connection,
                    transaction);
                AddIdempotencyCompletionParameters(completeIdempotencyCommand, resolvedDraft, idempotency);
                await completeIdempotencyCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return FiscalDocumentPersistenceResult.Created(resolvedDraft);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            throw new FiscalDocumentPersistenceException(
                "Fiscal document persistence write failed.",
                ex);
        }
    }

    public async Task<FiscalDocumentVoidPersistenceResult> VoidAsync(
        FiscalDocumentVoidCommand command,
        FiscalDocumentVoidIdempotency idempotency,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                var lockedDocument = await ReadFiscalDocumentForVoidUpdateAsync(
                    connection,
                    transaction,
                    command.FiscalDocumentId,
                    cancellationToken).ConfigureAwait(false);

                if (lockedDocument is null)
                {
                    throw new FiscalDocumentVoidNotFoundException();
                }

                if (IsAlreadyVoided(lockedDocument))
                {
                    if (string.Equals(lockedDocument.VoidIdempotencyKey, idempotency.Key, StringComparison.Ordinal) &&
                        string.Equals(lockedDocument.VoidSemanticRequestHash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                    {
                        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                        return FiscalDocumentVoidPersistenceResult.Replayed(ToVoidRecord(lockedDocument, idempotency.Key, command.CorrelationId));
                    }

                    throw new FiscalDocumentVoidIdempotencyConflictException();
                }

                if (!VoidableStatusCodeKeys.Contains(lockedDocument.CurrentStatusCodeKey))
                {
                    throw new FiscalDocumentVoidInvalidStateException(
                        "Only issued or recorded fiscal documents can be voided.");
                }

                var voidedStatusCodeId = await ResolveVoidedStatusCodeIdAsync(
                    connection,
                    transaction,
                    lockedDocument.CurrentStatusCodeSetId,
                    cancellationToken).ConfigureAwait(false);

                await using var insertIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertIdempotencyRecord,
                    connection,
                    transaction);
                AddInsertVoidIdempotencyParameters(insertIdempotencyCommand, command, idempotency, lockedDocument, voidedStatusCodeId);
                await insertIdempotencyCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var selectIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.SelectIdempotencyRecordForUpdate,
                    connection,
                    transaction);
                AddVoidIdempotencyKeyParameters(selectIdempotencyCommand, idempotency);
                await using var idempotencyReader = await selectIdempotencyCommand
                    .ExecuteReaderAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!await idempotencyReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Fiscal document void idempotency record was not available after insert.");
                }

                var existingSemanticHash = idempotencyReader.GetString(0);
                var linkedFiscalDocumentId = idempotencyReader.IsDBNull(1)
                    ? (Guid?)null
                    : idempotencyReader.GetGuid(1);
                await idempotencyReader.CloseAsync().ConfigureAwait(false);

                if (!string.Equals(existingSemanticHash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                {
                    throw new FiscalDocumentVoidIdempotencyConflictException();
                }

                if (linkedFiscalDocumentId is not null)
                {
                    if (linkedFiscalDocumentId.Value != command.FiscalDocumentId)
                    {
                        throw new FiscalDocumentVoidIdempotencyConflictException();
                    }

                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return FiscalDocumentVoidPersistenceResult.Replayed(ToVoidRecord(lockedDocument, idempotency.Key, command.CorrelationId));
                }

                var voidedAt = DateTimeOffset.UtcNow;

                await using var updateDocumentCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.UpdateFiscalDocumentVoided,
                    connection,
                    transaction);
                AddVoidUpdateParameters(updateDocumentCommand, command, idempotency, voidedStatusCodeId, voidedAt);
                await updateDocumentCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var statusHistoryCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.InsertFiscalDocumentVoidStatusHistory,
                    connection,
                    transaction);
                AddVoidStatusHistoryParameters(statusHistoryCommand, command, lockedDocument, voidedStatusCodeId, voidedAt);
                await statusHistoryCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var completeIdempotencyCommand = new NpgsqlCommand(
                    PostgresFiscalDocumentSql.UpdateIdempotencyRecordCompleted,
                    connection,
                    transaction);
                AddVoidIdempotencyCompletionParameters(completeIdempotencyCommand, command, idempotency);
                await completeIdempotencyCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                var record = new FiscalDocumentVoidRecord(
                    lockedDocument.FiscalDocumentId,
                    lockedDocument.FiscalDocumentNumber,
                    lockedDocument.FiscalSequenceValue,
                    "voided",
                    "recorded",
                    voidedAt,
                    command.ReasonCode,
                    command.ReasonText,
                    command.RequestedByRef,
                    idempotency.Key,
                    command.CorrelationId);

                return FiscalDocumentVoidPersistenceResult.NewlyVoided(record);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
        {
            throw new FiscalDocumentPersistenceException(
                "Fiscal document void persistence write failed.",
                ex);
        }
    }

    private static async Task<FiscalDocumentResolvedContext> ResolveFiscalContextAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        CancellationToken cancellationToken)
    {
        var fiscalIdentityIds = await ReadGuidListAsync(
            connection,
            transaction,
            PostgresFiscalDocumentSql.SelectEligibleFiscalIdentity,
            command => command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId),
            cancellationToken).ConfigureAwait(false);

        var fiscalIdentityId = fiscalIdentityIds.Count switch
        {
            1 => fiscalIdentityIds[0],
            > 1 => throw new FiscalDocumentFiscalContextException(
                FiscalDocumentCreationErrorCode.FiscalIdentityAmbiguous,
                "Fiscal document creation requires exactly one eligible fiscal identity for the Site POS Server."),
            _ => await ResolveMissingIdentityErrorAsync(connection, transaction, draft, cancellationToken).ConfigureAwait(false)
        };

        var fiscalSequencePolicies = await ReadFiscalSequencePoliciesAsync(
            connection,
            transaction,
            draft,
            cancellationToken).ConfigureAwait(false);

        var fiscalSequencePolicy = fiscalSequencePolicies.Count switch
        {
            1 => fiscalSequencePolicies[0],
            > 1 => throw new FiscalDocumentFiscalContextException(
                FiscalDocumentCreationErrorCode.FiscalSequencePolicyAmbiguous,
                "Fiscal document creation requires exactly one eligible fiscal sequence policy for the Site POS Server and document type."),
            _ => await ResolveMissingPolicyErrorAsync(connection, transaction, draft, cancellationToken).ConfigureAwait(false)
        };

        return new FiscalDocumentResolvedContext(
            fiscalIdentityId,
            fiscalSequencePolicy.FiscalSequencePolicyId,
            fiscalSequencePolicy.PolicyCode,
            fiscalSequencePolicy.PrefixText,
            fiscalSequencePolicy.SuffixText,
            fiscalSequencePolicy.PaddingLength);
    }

    private static async Task<FiscalDocumentNumberAssignment> AllocateFiscalNumberAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentResolvedContext resolvedContext,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectFiscalSequenceStateForUpdate,
            connection,
            transaction);
        command.Parameters.AddWithValue("fiscal_sequence_policy_id", resolvedContext.FiscalSequencePolicyId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.CloseAsync().ConfigureAwait(false);
            await ResolveMissingSequenceStateErrorAsync(
                connection,
                transaction,
                resolvedContext,
                cancellationToken).ConfigureAwait(false);
        }

        var currentSequenceValue = reader.GetInt64(0);
        var assignedAt = ReadDateTimeOffset(reader, 1);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new FiscalDocumentFiscalContextException(
                FiscalDocumentCreationErrorCode.FiscalNumberAllocationFailed,
                "Fiscal sequence state allocation returned multiple rows for one sequence policy.");
        }

        var nextSequenceValue = checked(currentSequenceValue + 1);
        var sequenceText = resolvedContext.PaddingLength is > 0
            ? nextSequenceValue.ToString(CultureInfo.InvariantCulture).PadLeft(resolvedContext.PaddingLength.Value, '0')
            : nextSequenceValue.ToString(CultureInfo.InvariantCulture);
        var prefixText = NormalizeOptionalPolicyText(resolvedContext.PrefixText);
        var suffixText = NormalizeOptionalPolicyText(resolvedContext.SuffixText);
        var fiscalDocumentNumber = string.Concat(prefixText, sequenceText, suffixText);
        var fiscalSeries = resolvedContext.PolicyCode.Trim();

        if (string.IsNullOrWhiteSpace(fiscalDocumentNumber) ||
            string.IsNullOrWhiteSpace(fiscalSeries))
        {
            throw new FiscalDocumentFiscalContextException(
                FiscalDocumentCreationErrorCode.FiscalDocumentNumberFormatFailed,
                "Fiscal document number could not be formatted from the selected fiscal sequence policy.");
        }

        return new FiscalDocumentNumberAssignment(
            resolvedContext.FiscalSequencePolicyId,
            nextSequenceValue,
            fiscalDocumentNumber,
            fiscalSeries,
            prefixText,
            suffixText,
            assignedAt,
            "pos-server:system");
    }

    private async Task<SalesInvoiceHeaderSnapshot?> ResolveSalesInvoiceHeaderSnapshotAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken)
    {
        if (draft.SiteId is null && requireCompleteSalesInvoiceHeaderProfile)
        {
            throw new FiscalDocumentFiscalContextException(
                FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileNotFound,
                "Sales Invoice header profile enforcement requires siteId in the issuance context.");
        }

        var profiles = await ReadEffectiveSalesInvoiceHeaderProfilesAsync(
            connection,
            transaction,
            draft,
            effectiveAt,
            cancellationToken).ConfigureAwait(false);

        var resolution = profiles.Count switch
        {
            0 => SalesInvoiceHeaderProfileResolutionResult.NotFound(),
            > 1 => SalesInvoiceHeaderProfileResolutionResult.Ambiguous(),
            _ => salesInvoiceHeaderProfileService.ResolveEffective(
                profiles,
                profiles[0].SiteId,
                draft.SitePosServerId,
                effectiveAt)
        };

        if (resolution.Status == SalesInvoiceHeaderProfileResolutionStatus.Resolved &&
            resolution.Profile is not null)
        {
            return salesInvoiceHeaderProfileService.CreateSnapshot(
                resolution.Profile,
                draft.RuntimeTerminalRef,
                effectiveAt,
                effectiveAt);
        }

        if (!requireCompleteSalesInvoiceHeaderProfile)
        {
            return null;
        }

        throw new FiscalDocumentFiscalContextException(
            resolution.Status switch
            {
                SalesInvoiceHeaderProfileResolutionStatus.Ambiguous => FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileAmbiguous,
                SalesInvoiceHeaderProfileResolutionStatus.Incomplete => FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileIncomplete,
                SalesInvoiceHeaderProfileResolutionStatus.UnsupportedVersion => FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileUnsupportedVersion,
                _ => FiscalDocumentCreationErrorCode.SalesInvoiceHeaderProfileNotFound
            },
            $"Sales Invoice header profile resolution failed: {string.Join(", ", resolution.Completeness.FailureCodes)}.");
    }

    private static async Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ReadEffectiveSalesInvoiceHeaderProfilesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectEffectiveSalesInvoiceHeaderProfileForUpdate,
            connection,
            transaction);
        command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId);
        command.Parameters.AddWithValue("site_id", (object?)draft.SiteId ?? DBNull.Value);
        command.Parameters.AddWithValue("effective_at", effectiveAt);

        var profiles = new List<SalesInvoiceHeaderProfile>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var fiscalIdentity = new FiscalIdentityProfile(
                reader.GetGuid(1),
                reader.GetString(27),
                reader.GetString(28),
                reader.GetString(29),
                reader.IsDBNull(30) ? null : reader.GetString(30),
                reader.GetString(31),
                ReadDateTimeOffset(reader, 32),
                ReadDateTimeOffset(reader, 33),
                reader.IsDBNull(34) ? null : reader.GetString(34),
                reader.IsDBNull(35) ? null : reader.GetString(35));

            profiles.Add(new SalesInvoiceHeaderProfile(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetGuid(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                ReadNullableDateOnly(reader, 11),
                ReadNullableDateOnly(reader, 12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                ReadNullableDateOnly(reader, 14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                ReadDateTimeOffset(reader, 17),
                reader.IsDBNull(18) ? null : ReadDateTimeOffset(reader, 18),
                reader.GetString(19),
                reader.IsDBNull(20) ? null : ReadDateTimeOffset(reader, 20),
                reader.IsDBNull(21) ? null : reader.GetString(21),
                reader.IsDBNull(22) ? null : ReadDateTimeOffset(reader, 22),
                ReadDateTimeOffset(reader, 23),
                ReadDateTimeOffset(reader, 24),
                reader.IsDBNull(25) ? null : reader.GetString(25),
                reader.IsDBNull(26) ? null : reader.GetString(26),
                fiscalIdentity));
        }

        return profiles;
    }

    private static async Task<Guid> ResolveMissingSequenceStateErrorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentResolvedContext resolvedContext,
        CancellationToken cancellationToken)
    {
        var stateCount = await ReadScalarInt64Async(
            connection,
            transaction,
            PostgresFiscalDocumentSql.CountFiscalSequenceStates,
            command => command.Parameters.AddWithValue("fiscal_sequence_policy_id", resolvedContext.FiscalSequencePolicyId),
            cancellationToken).ConfigureAwait(false);

        throw new FiscalDocumentFiscalContextException(
            stateCount > 0
                ? FiscalDocumentCreationErrorCode.FiscalSequenceStateNotEffective
                : FiscalDocumentCreationErrorCode.FiscalSequenceStateNotFound,
            stateCount > 0
                ? "No active and effective fiscal sequence state is eligible for the selected sequence policy."
                : "No fiscal sequence state exists for the selected sequence policy.");
    }

    private static async Task<FiscalDocumentDraft> ReadReplayDraftAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectReplayFiscalDocumentNumbering,
            connection,
            transaction);
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Idempotency record references a fiscal document that could not be found.");
        }

        var replayed = draft with
        {
            ResolvedFiscalIdentityId = reader.IsDBNull(0) ? null : reader.GetGuid(0),
            ResolvedFiscalSequencePolicyId = reader.IsDBNull(1) ? null : reader.GetGuid(1),
            FiscalSequenceValue = reader.IsDBNull(2) ? null : reader.GetInt64(2),
            FiscalDocumentNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
            FiscalSeries = reader.IsDBNull(4) ? null : reader.GetString(4),
            FiscalNumberPrefixText = reader.IsDBNull(5) ? null : reader.GetString(5),
            FiscalNumberSuffixText = reader.IsDBNull(6) ? null : reader.GetString(6),
            FiscalNumberAssignedAt = reader.IsDBNull(7) ? null : ReadDateTimeOffset(reader, 7),
            FiscalNumberAssignedByRef = reader.IsDBNull(8) ? null : reader.GetString(8)
        };

        await reader.CloseAsync().ConfigureAwait(false);
        return replayed with
        {
            SalesInvoiceHeaderSnapshot = await ReadHeaderSnapshotAsync(
                connection,
                transaction,
                draft.FiscalDocumentId,
                cancellationToken).ConfigureAwait(false)
        };
    }

    private static async Task<SalesInvoiceHeaderSnapshot?> ReadHeaderSnapshotAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectReplayFiscalDocumentHeaderSnapshot,
            connection,
            transaction);
        command.Parameters.AddWithValue("fiscal_document_id", fiscalDocumentId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new SalesInvoiceHeaderSnapshot(
            reader.GetGuid(0),
            reader.GetGuid(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.GetString(10),
            ReadDateOnly(reader, 11),
            ReadDateOnly(reader, 12),
            reader.GetString(13),
            ReadDateOnly(reader, 14),
            reader.GetString(15),
            reader.GetString(16),
            reader.GetString(17),
            reader.GetString(18),
            ReadDateTimeOffset(reader, 19),
            ReadDateTimeOffset(reader, 20));
    }

    private static async Task<IReadOnlyList<FiscalSequencePolicyCandidate>> ReadFiscalSequencePoliciesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectEligibleFiscalSequencePolicy,
            connection,
            transaction);
        command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId);
        command.Parameters.AddWithValue("fiscal_document_type_code_id", draft.FiscalDocumentTypeCodeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var values = new List<FiscalSequencePolicyCandidate>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(new FiscalSequencePolicyCandidate(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4)));
        }

        return values;
    }

    private static async Task<Guid> ResolveMissingIdentityErrorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        CancellationToken cancellationToken)
    {
        var relationshipCount = await ReadScalarInt64Async(
            connection,
            transaction,
            PostgresFiscalDocumentSql.CountFiscalIdentityRelationships,
            command => command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId),
            cancellationToken).ConfigureAwait(false);

        throw new FiscalDocumentFiscalContextException(
            relationshipCount > 0
                ? FiscalDocumentCreationErrorCode.FiscalIdentityNotEffective
                : FiscalDocumentCreationErrorCode.FiscalIdentityNotFound,
            relationshipCount > 0
                ? "No active and effective fiscal identity is eligible for the Site POS Server."
                : "No fiscal identity relationship exists for the Site POS Server.");
    }

    private static async Task<FiscalSequencePolicyCandidate> ResolveMissingPolicyErrorAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        FiscalDocumentDraft draft,
        CancellationToken cancellationToken)
    {
        var policyCount = await ReadScalarInt64Async(
            connection,
            transaction,
            PostgresFiscalDocumentSql.CountFiscalSequencePolicies,
            command =>
            {
                command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId);
                command.Parameters.AddWithValue("fiscal_document_type_code_id", draft.FiscalDocumentTypeCodeId);
            },
            cancellationToken).ConfigureAwait(false);

        throw new FiscalDocumentFiscalContextException(
            policyCount > 0
                ? FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotEffective
                : FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotFound,
            policyCount > 0
                ? "No active and effective fiscal sequence policy is eligible for the Site POS Server and document type."
                : "No fiscal sequence policy exists for the Site POS Server and document type.");
    }

    private static async Task<IReadOnlyList<Guid>> ReadGuidListAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Action<NpgsqlCommand> addParameters,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        addParameters(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var values = new List<Guid>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            values.Add(reader.GetGuid(0));
        }

        return values;
    }

    private static async Task<long> ReadScalarInt64Async(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Action<NpgsqlCommand> addParameters,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        addParameters(command);
        var value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(value);
    }

    private static async Task<LockedFiscalDocumentVoidState?> ReadFiscalDocumentForVoidUpdateAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            PostgresFiscalDocumentSql.SelectFiscalDocumentForVoidUpdate,
            connection,
            transaction);
        command.Parameters.AddWithValue("fiscal_document_id", fiscalDocumentId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new LockedFiscalDocumentVoidState(
            reader.GetGuid(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt64(2),
            reader.GetGuid(3),
            reader.GetGuid(4),
            reader.GetString(5),
            reader.GetGuid(6),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            reader.IsDBNull(8) ? null : reader.GetString(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.IsDBNull(11) ? null : ReadDateTimeOffset(reader, 11),
            reader.IsDBNull(12) ? null : reader.GetString(12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            reader.IsDBNull(14) ? null : reader.GetString(14));
    }

    private static async Task<Guid> ResolveVoidedStatusCodeIdAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid currentStatusCodeSetId,
        CancellationToken cancellationToken)
    {
        var statusIds = await ReadGuidListAsync(
            connection,
            transaction,
            PostgresFiscalDocumentSql.SelectVoidFiscalDocumentStatusCode,
            command => command.Parameters.AddWithValue("controlled_code_set_id", currentStatusCodeSetId),
            cancellationToken).ConfigureAwait(false);

        return statusIds.Count == 1
            ? statusIds[0]
            : throw new FiscalDocumentVoidInvalidStateException(
                "Fiscal document void requires exactly one active voided status code in the current fiscal document status code set.");
    }

    private static bool IsAlreadyVoided(LockedFiscalDocumentVoidState lockedDocument) =>
        string.Equals(lockedDocument.VoidStatus, "recorded", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(lockedDocument.CurrentStatusCodeKey, "voided", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(lockedDocument.CurrentStatusCodeKey, "cancelled", StringComparison.OrdinalIgnoreCase);

    private static FiscalDocumentVoidRecord ToVoidRecord(
        LockedFiscalDocumentVoidState lockedDocument,
        string idempotencyKey,
        string correlationId) =>
        new(
            lockedDocument.FiscalDocumentId,
            lockedDocument.FiscalDocumentNumber,
            lockedDocument.FiscalSequenceValue,
            "voided",
            lockedDocument.VoidStatus ?? "recorded",
            lockedDocument.VoidedAt ?? DateTimeOffset.UtcNow,
            lockedDocument.VoidReasonCode ?? "other",
            lockedDocument.VoidReasonText,
            lockedDocument.VoidRequestedByRef ?? "unknown",
            idempotencyKey,
            lockedDocument.VoidCorrelationId ?? correlationId);

    private static void AddInsertIdempotencyParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency)
    {
        command.Parameters.AddWithValue("idempotency_record_id", Guid.NewGuid());
        command.Parameters.AddWithValue("idempotency_scope", idempotency.Scope);
        command.Parameters.AddWithValue("idempotency_key", idempotency.Key);
        command.Parameters.AddWithValue("semantic_request_hash", idempotency.SemanticRequestHash);
        command.Parameters.AddWithValue("operation_type_code_id", draft.FiscalDocumentTypeCodeId);
        command.Parameters.AddWithValue("operation_status_code_id", draft.FiscalDocumentStatusCodeId);

        var contextParameter = command.Parameters.Add("idempotency_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = PostgresFiscalDocumentSql.CreateIdempotencyContextJson(draft, idempotency);
    }

    private static void AddInsertVoidIdempotencyParameters(
        NpgsqlCommand command,
        FiscalDocumentVoidCommand voidCommand,
        FiscalDocumentVoidIdempotency idempotency,
        LockedFiscalDocumentVoidState lockedDocument,
        Guid voidedStatusCodeId)
    {
        command.Parameters.AddWithValue("idempotency_record_id", Guid.NewGuid());
        command.Parameters.AddWithValue("idempotency_scope", idempotency.Scope);
        command.Parameters.AddWithValue("idempotency_key", idempotency.Key);
        command.Parameters.AddWithValue("semantic_request_hash", idempotency.SemanticRequestHash);
        command.Parameters.AddWithValue("operation_type_code_id", lockedDocument.FiscalDocumentTypeCodeId);
        command.Parameters.AddWithValue("operation_status_code_id", voidedStatusCodeId);

        var contextParameter = command.Parameters.Add("idempotency_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = PostgresFiscalDocumentSql.CreateVoidIdempotencyContextJson(voidCommand, idempotency);
    }

    private static void AddIdempotencyKeyParameters(NpgsqlCommand command, FiscalIssuanceIdempotency idempotency)
    {
        command.Parameters.AddWithValue("idempotency_scope", idempotency.Scope);
        command.Parameters.AddWithValue("idempotency_key", idempotency.Key);
    }

    private static void AddVoidIdempotencyKeyParameters(NpgsqlCommand command, FiscalDocumentVoidIdempotency idempotency)
    {
        command.Parameters.AddWithValue("idempotency_scope", idempotency.Scope);
        command.Parameters.AddWithValue("idempotency_key", idempotency.Key);
    }

    private static void AddIdempotencyCompletionParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalIssuanceIdempotency idempotency)
    {
        AddIdempotencyKeyParameters(command, idempotency);
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("replay_result_ref", draft.FiscalDocumentId.ToString("D"));
    }

    private static void AddVoidIdempotencyCompletionParameters(
        NpgsqlCommand command,
        FiscalDocumentVoidCommand voidCommand,
        FiscalDocumentVoidIdempotency idempotency)
    {
        AddVoidIdempotencyKeyParameters(command, idempotency);
        command.Parameters.AddWithValue("fiscal_document_id", voidCommand.FiscalDocumentId);
        command.Parameters.AddWithValue("replay_result_ref", voidCommand.FiscalDocumentId.ToString("D"));
    }

    private static void AddVoidUpdateParameters(
        NpgsqlCommand command,
        FiscalDocumentVoidCommand voidCommand,
        FiscalDocumentVoidIdempotency idempotency,
        Guid voidedStatusCodeId,
        DateTimeOffset voidedAt)
    {
        command.Parameters.AddWithValue("fiscal_document_id", voidCommand.FiscalDocumentId);
        command.Parameters.AddWithValue("voided_fiscal_document_status_code_id", voidedStatusCodeId);
        command.Parameters.AddWithValue("void_reason_code", voidCommand.ReasonCode);
        command.Parameters.AddWithValue("void_reason_text", (object?)voidCommand.ReasonText ?? DBNull.Value);
        command.Parameters.AddWithValue("void_requested_by_ref", voidCommand.RequestedByRef);
        command.Parameters.AddWithValue("void_requested_at", voidCommand.RequestedAt);
        command.Parameters.AddWithValue("voided_at", voidedAt);
        command.Parameters.AddWithValue("void_idempotency_key", idempotency.Key);
        command.Parameters.AddWithValue("void_semantic_request_hash", idempotency.SemanticRequestHash);
        command.Parameters.AddWithValue("void_correlation_id", voidCommand.CorrelationId);
        command.Parameters.AddWithValue("void_source_system_ref", (object?)voidCommand.SourceSystemRef ?? DBNull.Value);
        command.Parameters.AddWithValue("void_business_day_date", (object?)voidCommand.BusinessDayDate ?? DBNull.Value);
    }

    private static void AddVoidStatusHistoryParameters(
        NpgsqlCommand command,
        FiscalDocumentVoidCommand voidCommand,
        LockedFiscalDocumentVoidState lockedDocument,
        Guid voidedStatusCodeId,
        DateTimeOffset voidedAt)
    {
        command.Parameters.AddWithValue("fiscal_document_status_history_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", voidCommand.FiscalDocumentId);
        command.Parameters.AddWithValue("prior_fiscal_document_status_code_id", lockedDocument.FiscalDocumentStatusCodeId);
        command.Parameters.AddWithValue("voided_fiscal_document_status_code_id", voidedStatusCodeId);
        command.Parameters.AddWithValue("void_reason_text", (object?)voidCommand.ReasonText ?? DBNull.Value);
        command.Parameters.AddWithValue("voided_at", voidedAt);
        command.Parameters.AddWithValue("void_requested_by_ref", voidCommand.RequestedByRef);
    }

    private static void AddFiscalDocumentParameters(NpgsqlCommand command, FiscalDocumentDraft draft)
    {
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("site_pos_server_id", draft.SitePosServerId);
        command.Parameters.AddWithValue("channel_terminal_id", (object?)draft.ChannelTerminalId ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_identity_id", (object?)draft.ResolvedFiscalIdentityId ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_document_type_code_id", draft.FiscalDocumentTypeCodeId);
        command.Parameters.AddWithValue("fiscal_document_status_code_id", draft.FiscalDocumentStatusCodeId);
        command.Parameters.AddWithValue("fiscal_sequence_policy_id", (object?)draft.ResolvedFiscalSequencePolicyId ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_sequence_value", (object?)draft.FiscalSequenceValue ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_document_number", (object?)draft.FiscalDocumentNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_series", (object?)draft.FiscalSeries ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_number_prefix_text", (object?)draft.FiscalNumberPrefixText ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_number_suffix_text", (object?)draft.FiscalNumberSuffixText ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_number_assigned_at", (object?)draft.FiscalNumberAssignedAt ?? DBNull.Value);
        command.Parameters.AddWithValue("fiscal_number_assigned_by_ref", (object?)draft.FiscalNumberAssignedByRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_parking_session_ref", (object?)draft.CentralPmsParkingSessionRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_attempt_ref", (object?)draft.CentralPmsPaymentAttemptRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_confirmation_ref", (object?)draft.CentralPmsPaymentConfirmationRef ?? DBNull.Value);
        command.Parameters.AddWithValue("payment_finality_ref", (object?)draft.PaymentFinalityRef ?? DBNull.Value);
        command.Parameters.AddWithValue("vendor_ack_ref", (object?)draft.VendorAckRef ?? DBNull.Value);
        command.Parameters.AddWithValue("business_day_date", (object?)draft.BusinessDayDate ?? DBNull.Value);

        var contextParameter = command.Parameters.Add("document_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = PostgresFiscalDocumentSql.CreateDocumentContextJson(draft);
    }

    private static void AddHeaderSnapshotParameters(NpgsqlCommand command, FiscalDocumentDraft draft)
    {
        var snapshot = draft.SalesInvoiceHeaderSnapshot ??
            throw new InvalidOperationException("Sales Invoice header snapshot is required.");

        command.Parameters.AddWithValue("fiscal_document_header_snapshot_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_identity_id", snapshot.FiscalIdentityId);
        command.Parameters.AddWithValue("sales_invoice_header_profile_id", snapshot.SalesInvoiceHeaderProfileId);
        command.Parameters.AddWithValue("profile_version", snapshot.ProfileVersion);
        command.Parameters.AddWithValue("registered_business_name", snapshot.RegisteredBusinessName);
        command.Parameters.AddWithValue("registered_business_address", snapshot.RegisteredBusinessAddress);
        command.Parameters.AddWithValue("tin", snapshot.Tin);
        command.Parameters.AddWithValue("pos_serial_number", snapshot.PosSerialNumber);
        command.Parameters.AddWithValue("machine_identification_number", snapshot.MachineIdentificationNumber);
        command.Parameters.AddWithValue("parking_location_display", snapshot.ParkingLocationDisplay);
        command.Parameters.AddWithValue("terminal_id", (object?)snapshot.TerminalId ?? DBNull.Value);
        command.Parameters.AddWithValue("bir_accreditation_number", snapshot.BirAccreditationNumber);
        command.Parameters.AddWithValue("bir_accreditation_issued_date", snapshot.BirAccreditationIssuedDate);
        command.Parameters.AddWithValue("bir_accreditation_valid_until", snapshot.BirAccreditationValidUntil);
        command.Parameters.AddWithValue("ptu_number", snapshot.PtuNumber);
        command.Parameters.AddWithValue("ptu_issued_date", snapshot.PtuIssuedDate);
        command.Parameters.AddWithValue("sales_invoice_legal_statement", snapshot.SalesInvoiceLegalStatement);
        command.Parameters.AddWithValue("customer_service_footer", snapshot.CustomerServiceFooter);
        command.Parameters.AddWithValue("template_version", snapshot.TemplateVersion);
        command.Parameters.AddWithValue("presentation_version", snapshot.PresentationVersion);
        command.Parameters.AddWithValue("effective_at", snapshot.EffectiveAt);
        command.Parameters.AddWithValue("snapshot_created_at", snapshot.SnapshotCreatedAt);

        var snapshotJsonParameter = command.Parameters.Add("snapshot_json", NpgsqlDbType.Jsonb);
        snapshotJsonParameter.Value = PostgresFiscalDocumentSql.CreateSalesInvoiceHeaderSnapshotJson(snapshot);
    }

    private static void AddSequenceStateUpdateParameters(
        NpgsqlCommand command,
        FiscalDocumentNumberAssignment assignment)
    {
        command.Parameters.AddWithValue("fiscal_sequence_policy_id", assignment.FiscalSequencePolicyId);
        command.Parameters.AddWithValue("fiscal_sequence_value", assignment.FiscalSequenceValue);
        command.Parameters.AddWithValue("fiscal_number_assigned_at", assignment.FiscalNumberAssignedAt);
    }

    private static DateTimeOffset ReadDateTimeOffset(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => throw new InvalidOperationException("PostgreSQL timestamp value could not be read.")
        };
    }

    private static DateOnly ReadDateOnly(NpgsqlDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateOnly dateOnly => dateOnly,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => throw new InvalidOperationException("PostgreSQL date value could not be read.")
        };
    }

    private static DateOnly? ReadNullableDateOnly(NpgsqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ReadDateOnly(reader, ordinal);

    private static string? NormalizeOptionalPolicyText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record FiscalSequencePolicyCandidate(
        Guid FiscalSequencePolicyId,
        string PolicyCode,
        string? PrefixText,
        string? SuffixText,
        int? PaddingLength);

    private sealed record LockedFiscalDocumentVoidState(
        Guid FiscalDocumentId,
        string? FiscalDocumentNumber,
        long? FiscalSequenceValue,
        Guid FiscalDocumentTypeCodeId,
        Guid FiscalDocumentStatusCodeId,
        string CurrentStatusCodeKey,
        Guid CurrentStatusCodeSetId,
        string? VoidStatus,
        string? VoidReasonCode,
        string? VoidReasonText,
        string? VoidRequestedByRef,
        DateTimeOffset? VoidedAt,
        string? VoidIdempotencyKey,
        string? VoidSemanticRequestHash,
        string? VoidCorrelationId);

    private static void AddStatusHistoryParameters(NpgsqlCommand command, FiscalDocumentDraft draft)
    {
        command.Parameters.AddWithValue("fiscal_document_status_history_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_status_code_id", draft.FiscalDocumentStatusCodeId);
    }

    private static void AddDocumentLinkParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDocumentLinkInput link)
    {
        command.Parameters.AddWithValue("fiscal_document_link_id", Guid.NewGuid());
        command.Parameters.AddWithValue("source_fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("target_fiscal_document_id", link.TargetFiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_link_type_code_id", link.LinkTypeCodeId);
        command.Parameters.AddWithValue("link_reason_code_id", (object?)link.LinkReasonCodeId ?? DBNull.Value);
        command.Parameters.AddWithValue("link_reason_text", (object?)link.LinkReasonText ?? DBNull.Value);
        command.Parameters.AddWithValue("created_by_ref", (object?)link.CreatedByRef ?? DBNull.Value);
    }

    private static void AddDocumentLineParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDocumentLineInput line,
        Guid fiscalDocumentLineId)
    {
        command.Parameters.AddWithValue("fiscal_document_line_id", fiscalDocumentLineId);
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("line_sequence", line.LineSequence);
        command.Parameters.AddWithValue("line_type_code_id", line.LineTypeCodeId);
        command.Parameters.AddWithValue("line_status_code_id", (object?)line.LineStatusCodeId ?? DBNull.Value);
        command.Parameters.AddWithValue("description", line.Description);
        command.Parameters.AddWithValue("quantity", line.Quantity);
        command.Parameters.AddWithValue("unit_amount_minor_units", line.UnitAmountMinorUnits);
        command.Parameters.AddWithValue("gross_amount_minor_units", line.GrossAmountMinorUnits);
        command.Parameters.AddWithValue("discount_amount_minor_units", line.DiscountAmountMinorUnits);
        command.Parameters.AddWithValue("tax_amount_minor_units", line.TaxAmountMinorUnits);
        command.Parameters.AddWithValue("net_amount_minor_units", line.NetAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", line.CurrencyCode);
        command.Parameters.AddWithValue("source_ref", (object?)line.SourceRef ?? DBNull.Value);

        var lineContextParameter = command.Parameters.Add("line_context", NpgsqlDbType.Jsonb);
        lineContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateLineContextJson(line) ?? DBNull.Value;
    }

    private static void AddTenderParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTenderInput tender)
    {
        command.Parameters.AddWithValue("fiscal_tender_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("tender_type_code_id", tender.TenderTypeCodeId);
        command.Parameters.AddWithValue("amount_minor_units", tender.AmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", tender.CurrencyCode);
        command.Parameters.AddWithValue("central_pms_payment_attempt_ref", (object?)tender.CentralPmsPaymentAttemptRef ?? DBNull.Value);
        command.Parameters.AddWithValue("central_pms_payment_confirmation_ref", (object?)tender.CentralPmsPaymentConfirmationRef ?? DBNull.Value);
        command.Parameters.AddWithValue("payment_finality_ref", (object?)tender.PaymentFinalityRef ?? DBNull.Value);
        command.Parameters.AddWithValue("provider_ref", (object?)tender.ProviderRef ?? DBNull.Value);

        var tenderContextParameter = command.Parameters.Add("tender_context", NpgsqlDbType.Jsonb);
        tenderContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTenderContextJson(tender) ?? DBNull.Value;
    }

    private static void AddTaxDetailParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTaxDetailInput taxDetail,
        IReadOnlyDictionary<int, Guid> lineIdsBySequence)
    {
        Guid? fiscalDocumentLineId = null;
        if (taxDetail.LineSequence is not null)
        {
            fiscalDocumentLineId = lineIdsBySequence[taxDetail.LineSequence.Value];
        }

        command.Parameters.AddWithValue("fiscal_tax_detail_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_line_id", (object?)fiscalDocumentLineId ?? DBNull.Value);
        command.Parameters.AddWithValue("tax_type_code_id", taxDetail.TaxTypeCodeId);
        command.Parameters.AddWithValue("tax_classification_code_id", taxDetail.TaxClassificationCodeId);
        command.Parameters.AddWithValue("tax_rate", (object?)taxDetail.TaxRate ?? DBNull.Value);
        command.Parameters.AddWithValue("taxable_amount_minor_units", taxDetail.TaxableAmountMinorUnits);
        command.Parameters.AddWithValue("tax_amount_minor_units", taxDetail.TaxAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", taxDetail.CurrencyCode);

        var taxContextParameter = command.Parameters.Add("tax_context", NpgsqlDbType.Jsonb);
        taxContextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTaxContextJson(taxDetail) ?? DBNull.Value;
    }

    private static void AddDiscountPrivilegeDetailParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalDiscountPrivilegeDetailInput discountPrivilegeDetail,
        IReadOnlyDictionary<int, Guid> lineIdsBySequence)
    {
        Guid? fiscalDocumentLineId = null;
        if (discountPrivilegeDetail.LineSequence is not null)
        {
            fiscalDocumentLineId = lineIdsBySequence[discountPrivilegeDetail.LineSequence.Value];
        }

        command.Parameters.AddWithValue("fiscal_discount_privilege_detail_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("fiscal_document_line_id", (object?)fiscalDocumentLineId ?? DBNull.Value);
        command.Parameters.AddWithValue("discount_privilege_type_code_id", discountPrivilegeDetail.DiscountPrivilegeTypeCodeId);
        command.Parameters.AddWithValue("basis_amount_minor_units", discountPrivilegeDetail.BasisAmountMinorUnits);
        command.Parameters.AddWithValue("discount_amount_minor_units", discountPrivilegeDetail.DiscountAmountMinorUnits);
        command.Parameters.AddWithValue("vat_privilege_amount_minor_units", discountPrivilegeDetail.VatPrivilegeAmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", discountPrivilegeDetail.CurrencyCode);
        command.Parameters.AddWithValue("beneficiary_ref", (object?)discountPrivilegeDetail.BeneficiaryRef ?? DBNull.Value);
        command.Parameters.AddWithValue("evidence_ref", (object?)discountPrivilegeDetail.EvidenceRef ?? DBNull.Value);
        command.Parameters.AddWithValue("approval_ref", (object?)discountPrivilegeDetail.ApprovalRef ?? DBNull.Value);

        var contextParameter = command.Parameters.Add("discount_privilege_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateDiscountPrivilegeContextJson(discountPrivilegeDetail) ?? DBNull.Value;
    }

    private static void AddTotalParameters(
        NpgsqlCommand command,
        FiscalDocumentDraft draft,
        FiscalTotalInput total)
    {
        command.Parameters.AddWithValue("fiscal_total_id", Guid.NewGuid());
        command.Parameters.AddWithValue("fiscal_document_id", draft.FiscalDocumentId);
        command.Parameters.AddWithValue("total_type_code_id", total.TotalTypeCodeId);
        command.Parameters.AddWithValue("amount_minor_units", total.AmountMinorUnits);
        command.Parameters.AddWithValue("currency_code", total.CurrencyCode);

        var contextParameter = command.Parameters.Add("total_context", NpgsqlDbType.Jsonb);
        contextParameter.Value = (object?)PostgresFiscalDocumentSql.CreateTotalContextJson(total) ?? DBNull.Value;
    }
}
