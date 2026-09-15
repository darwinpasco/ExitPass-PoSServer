using System.Net;
using System.Net.Http.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class PersistentIstSchemaReconciliationProofTests
{
    private const string ConnectionStringEnvironmentVariable = "POSSERVER_PERSISTENT_SCHEMA_RECONCILIATION_PROOF_DB_URL";
    private const string ExpectedDatabase = "exitpass_pos_ist_schema_reconcile_validation";
    private const string ProofApiKey = "disposable-schema-reconciliation-proof-key";
    private const string PaymentFinalityRef = "terminal-cash-payment-confirmation:e308ac46-10c9-4afa-8666-4f96eaa64dbd:sales_invoice";
    private static readonly Guid HistoricalPeriodId = Guid.Parse("1ee930c2-a471-5b34-8329-69974a64142c");
    private static readonly Guid SequencePolicyId = Guid.Parse("2e4252dd-38f0-5424-bee2-0c864b41f6b2");

    [Fact]
    public async Task HistoricalAndCurrentElectronicJournalRowsVerifyUnderPersistedSemanticProfiles()
    {
        if (!TryGetDisposableProofConnectionString(out var connectionString)) return;

        await using var app = await StartApiAsync(connectionString);
        using var scope = app.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IElectronicJournalRepository>();
        var query = new ElectronicJournalQuery(
                Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"),
                Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a"), "PHP");
        var page = await repository.ReadAsync(query);
        Assert.NotNull(page.Page);
        Assert.InRange(page.Page.Events.Count, 10, 11);
        foreach (var historical in page.Page.Events.Where(value => value.StreamSequence <= 10))
        {
            Assert.Null(historical.PrintableSalesInvoiceText);
            Assert.Equal(ElectronicJournalContract.LegacySemanticHashVersion, historical.SemanticHashVersion);
            var append = ToAppendRequest(historical);
            var legacySemantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(
                append, historical.SemanticHashVersion);
            Assert.Equal(historical.SemanticHash, legacySemantic);
            Assert.Equal(historical.IntegrityHash, ElectronicJournalCanonicalizer.ComputeIntegrityHash(
                append, historical.EventReference, historical.StreamSequence, historical.RecordedAt,
                legacySemantic, historical.PreviousIntegrityHash));
        }
        foreach (var current in page.Page.Events.Where(value => value.StreamSequence > 10))
        {
            Assert.Equal(11, current.StreamSequence);
            Assert.Equal(ElectronicJournalContract.CurrentSemanticHashVersion, current.SemanticHashVersion);
            var currentAppend = ToAppendRequest(current);
            var currentSemantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(
                currentAppend, current.SemanticHashVersion);
            Assert.Equal(current.SemanticHash, currentSemantic);
            Assert.Equal(current.IntegrityHash, ElectronicJournalCanonicalizer.ComputeIntegrityHash(
                currentAppend, current.EventReference, current.StreamSequence, current.RecordedAt,
                currentSemantic, current.PreviousIntegrityHash));
        }
        var integrity = await repository.VerifyIntegrityAsync(query);
        Assert.Equal(ElectronicJournalOutcome.Success, integrity.Outcome);
        Assert.NotNull(integrity.Result);
        Assert.True(integrity.Result.IsValid);
        Assert.Equal(page.Page.Events.Count, integrity.Result.VerifiedEventCount);
        Assert.Equal(page.Page.Events.Count, integrity.Result.LastSequence);
        Assert.Null(integrity.Result.FailureCode);
    }

    [Fact]
    public async Task ExistingLateForeignKeyFailureFixtureLeavesNoPartialState()
    {
        if (!TryGetDisposableProofConnectionString(out var connectionString)) return;

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var before = await ReadMutationManifestAsync(connection);
        var request = ExactHistoricalRequest() with
        {
            BusinessDayDate = new DateOnly(2026, 9, 10),
            PayableBasis = ExactHistoricalRequest().PayableBasis! with
            {
                PayableBasisRef = "schema-reconcile-independent-rollback-payable",
                UpstreamFinalityRef = "schema-reconcile-independent-rollback-finality"
            },
            CentralPmsParkingSessionRef = "schema-reconcile-independent-rollback-session",
            CentralPmsPaymentAttemptRef = "schema-reconcile-independent-rollback-attempt",
            CentralPmsPaymentConfirmationRef = "schema-reconcile-independent-rollback-confirmation",
            UpstreamFinalityRef = "schema-reconcile-independent-rollback-finality",
            PaymentFinalityRef = "schema-reconcile-independent-rollback-finality",
            DocumentLines = ExactHistoricalRequest().DocumentLines!.Select(line => line with { SourceRef = "schema-reconcile-independent-rollback-payable" }).ToArray(),
            Lines = ExactHistoricalRequest().Lines!.Select(line => line with { SourceRef = "schema-reconcile-independent-rollback-payable" }).ToArray(),
            Tenders = ExactHistoricalRequest().Tenders!.Select(tender => tender with
            {
                CentralPmsPaymentAttemptRef = "schema-reconcile-independent-rollback-attempt",
                CentralPmsPaymentConfirmationRef = "schema-reconcile-independent-rollback-confirmation",
                PaymentFinalityRef = "schema-reconcile-independent-rollback-finality"
            }).ToArray(),
            Totals = [ExactHistoricalRequest().Totals![0] with { TotalTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000009999") }]
        };

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("persistence_write_failed", body.Code);
        Assert.Equal(before, await ReadMutationManifestAsync(connection));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.fiscal_documents where payment_finality_ref='schema-reconcile-independent-rollback-finality'"));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.idempotency_records where idempotency_key='schema-reconcile-independent-rollback-finality'"));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.electronic_journal_records where idempotency_ref='schema-reconcile-independent-rollback-finality'"));
    }

    [Fact]
    public async Task ExactFailedTerminalCashRequestCreatesOncePrintsAndReplaysWithoutAdvancement()
    {
        if (!TryGetDisposableProofConnectionString(out var connectionString)) return;

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = ExactHistoricalRequest();
        var executionStartedAt = DateTimeOffset.UtcNow;
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(10, await ScalarLongAsync(connection, "select count(*) from pos.fiscal_documents"));
        Assert.Equal(10, await ScalarLongAsync(connection, "select count(*) from pos.idempotency_records"));
        Assert.Equal(10, await ScalarLongAsync(connection, "select count(*) from pos.fiscal_document_header_snapshots"));
        Assert.Equal(10, await ScalarLongAsync(connection, "select count(*) from pos.electronic_journal_records"));
        Assert.Equal(10, await ScalarLongAsync(connection,
            $"select count(*) from pos.electronic_journal_records where is_canonical and semantic_hash_version='{ElectronicJournalContract.LegacySemanticHashVersion}'"));
        Assert.Equal(0, await ScalarLongAsync(connection,
            $"select count(*) from pos.electronic_journal_records where is_canonical and semantic_hash_version='{ElectronicJournalContract.CurrentSemanticHashVersion}'"));
        Assert.Equal("10|10|10", await ScalarStringAsync(connection,
            "select concat_ws('|',current_sequence_value,last_reserved_sequence_value,last_issued_sequence_value) from pos.fiscal_sequence_states where fiscal_sequence_policy_id='2e4252dd-38f0-5424-bee2-0c864b41f6b2'"));
        var historicalEjManifest = await ReadHistoricalEjManifestAsync(connection);
        var priorStreamHead = await ScalarStringAsync(connection,
            "select last_event_hash from pos.electronic_journal_streams");
        var priorStreamSequence = await ScalarLongAsync(connection,
            "select last_sequence_value from pos.electronic_journal_streams");
        Assert.Equal(10, priorStreamSequence);
        using (var preIssuanceScope = app.Services.CreateScope())
        {
            var preIssuanceIntegrity = await preIssuanceScope.ServiceProvider.GetRequiredService<IElectronicJournalRepository>()
                .VerifyIntegrityAsync(new ElectronicJournalQuery(
                    Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"),
                    Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a"), "PHP", ThroughSequence: 10));
            Assert.Equal(ElectronicJournalOutcome.Success, preIssuanceIntegrity.Outcome);
            Assert.NotNull(preIssuanceIntegrity.Result);
            Assert.True(preIssuanceIntegrity.Result.IsValid);
            Assert.Equal(10, preIssuanceIntegrity.Result.VerifiedEventCount);
            Assert.Equal(10, preIssuanceIntegrity.Result.LastSequence);
        }

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.NotNull(body);
        Assert.True(response.StatusCode == HttpStatusCode.Accepted,
            $"Expected Accepted but received {(int)response.StatusCode} {body.Code}: {body.Message}");
        Assert.True(body.Succeeded);
        Assert.Equal("newly_created", body.ResultClassification);
        Assert.NotNull(body.FiscalDocumentId);
        Assert.Equal(11, body.FiscalSequenceValue);
        Assert.Equal("SI-00000011", body.FiscalDocumentNumber);
        Assert.Equal("PAYMENT_FINALITY", body.CompletionBasis);
        Assert.Equal("e308ac46-10c9-4afa-8666-4f96eaa64dbd", body.CompletionAuthorityRef);
        Assert.False(string.IsNullOrWhiteSpace(body.ElectronicJournalEventReference));

        await AssertCreatedStateAsync(connection, body.FiscalDocumentId.Value, body.ElectronicJournalEventReference!,
            priorStreamHead, priorStreamSequence, executionStartedAt);
        using (var scope = app.Services.CreateScope())
        {
            var render = await scope.ServiceProvider.GetRequiredService<DigitalSalesInvoiceRenderService>()
                .RenderAsync(body.FiscalDocumentId.Value);
            Assert.True(render.Succeeded);
            Assert.NotNull(render.Render);
            // The writer rendered the exact request draft, whose tender carried ProviderRef=CASH
            // and no resolved code key. Recreate that immutable writer input for byte comparison.
            var exactCreationRender = render.Render with
            {
                Tenders = render.Render.Tenders.Select(tender => tender with
                {
                    TenderTypeCodeKey = null,
                    ProviderRef = "CASH"
                }).ToArray()
            };
            var expectedPrintableText = scope.ServiceProvider.GetRequiredService<CanonicalSalesInvoiceTextRenderer>()
                .Render(scope.ServiceProvider.GetRequiredService<CanonicalSalesInvoiceTextFactory>().Create(exactCreationRender)).Text;
            await using var printableCommand = new NpgsqlCommand(
                "select printable_sales_invoice_text from pos.electronic_journal_records where event_reference=@event", connection);
            printableCommand.Parameters.AddWithValue("event", body.ElectronicJournalEventReference!);
            Assert.Equal(expectedPrintableText, (string)(await printableCommand.ExecuteScalarAsync())!);

            Assert.NotNull(body.FiscalIdentityId);
            var journal = await scope.ServiceProvider.GetRequiredService<IElectronicJournalRepository>()
                .ReadAsync(new ElectronicJournalQuery(
                    Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"), body.FiscalIdentityId.Value, "PHP",
                    FiscalDocumentReference: body.FiscalDocumentId.Value.ToString("D")));
            Assert.Equal(ElectronicJournalOutcome.Success, journal.Outcome);
            Assert.NotNull(journal.Page);
            var journalEvent = Assert.Single(journal.Page.Events);
            Assert.Equal(body.ElectronicJournalEventReference, journalEvent.EventReference);
            var append = ToAppendRequest(journalEvent);
            Assert.Equal(ElectronicJournalContract.CurrentSemanticHashVersion, journalEvent.SemanticHashVersion);
            var semanticHash = ElectronicJournalCanonicalizer.ComputeSemanticHash(
                append, journalEvent.SemanticHashVersion);
            Assert.Equal(journalEvent.SemanticHash, semanticHash);
            Assert.Equal(journalEvent.IntegrityHash, ElectronicJournalCanonicalizer.ComputeIntegrityHash(
                append, journalEvent.EventReference, journalEvent.StreamSequence, journalEvent.RecordedAt,
                semanticHash, journalEvent.PreviousIntegrityHash));
        }
        Assert.Equal(historicalEjManifest, await ReadHistoricalEjManifestAsync(connection));
        Assert.Equal(10, await ScalarLongAsync(connection,
            $"select count(*) from pos.electronic_journal_records where is_canonical and semantic_hash_version='{ElectronicJournalContract.LegacySemanticHashVersion}'"));
        Assert.Equal(1, await ScalarLongAsync(connection,
            $"select count(*) from pos.electronic_journal_records where is_canonical and semantic_hash_version='{ElectronicJournalContract.CurrentSemanticHashVersion}'"));
        using (var mixedScope = app.Services.CreateScope())
        {
            var mixedIntegrity = await mixedScope.ServiceProvider.GetRequiredService<IElectronicJournalRepository>()
                .VerifyIntegrityAsync(new ElectronicJournalQuery(
                    Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"),
                    Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a"), "PHP"));
            Assert.Equal(ElectronicJournalOutcome.Success, mixedIntegrity.Outcome);
            Assert.NotNull(mixedIntegrity.Result);
            Assert.True(mixedIntegrity.Result.IsValid);
            Assert.Equal(11, mixedIntegrity.Result.VerifiedEventCount);
            Assert.Equal(11, mixedIntegrity.Result.LastSequence);
        }
        Assert.Equal(await ScalarStringAsync(connection,
                "select integrity_hash from pos.electronic_journal_records where stream_sequence_value=11"),
            await ScalarStringAsync(connection, "select last_event_hash from pos.electronic_journal_streams"));
        var afterFirst = await ReadMutationManifestAsync(connection);

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replay = await replayResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        Assert.NotNull(replay);
        Assert.True(replay.Succeeded);
        Assert.Equal("idempotent_replay", replay.ResultClassification);
        Assert.Equal(body.FiscalDocumentId, replay.FiscalDocumentId);
        Assert.Equal(body.FiscalDocumentNumber, replay.FiscalDocumentNumber);
        Assert.Equal(body.ElectronicJournalEventReference, replay.ElectronicJournalEventReference);
        Assert.Equal(afterFirst, await ReadMutationManifestAsync(connection));

        var rollbackRequest = request with
        {
            PayableBasis = request.PayableBasis! with
            {
                PayableBasisRef = "schema-reconcile-rollback-payable",
                UpstreamFinalityRef = "schema-reconcile-rollback-finality"
            },
            CentralPmsParkingSessionRef = "schema-reconcile-rollback-session",
            CentralPmsPaymentAttemptRef = "schema-reconcile-rollback-attempt",
            CentralPmsPaymentConfirmationRef = "schema-reconcile-rollback-confirmation",
            UpstreamFinalityRef = "schema-reconcile-rollback-finality",
            PaymentFinalityRef = "schema-reconcile-rollback-finality",
            DocumentLines = request.DocumentLines!.Select(line => line with { SourceRef = "schema-reconcile-rollback-payable" }).ToArray(),
            Lines = request.Lines!.Select(line => line with { SourceRef = "schema-reconcile-rollback-payable" }).ToArray(),
            Tenders = request.Tenders!.Select(tender => tender with
            {
                CentralPmsPaymentAttemptRef = "schema-reconcile-rollback-attempt",
                CentralPmsPaymentConfirmationRef = "schema-reconcile-rollback-confirmation",
                PaymentFinalityRef = "schema-reconcile-rollback-finality"
            }).ToArray(),
            Totals = [request.Totals![0] with { TotalTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000009999") }]
        };

        using var rollbackResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", rollbackRequest);
        var rollback = await rollbackResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, rollbackResponse.StatusCode);
        Assert.NotNull(rollback);
        Assert.False(rollback.Succeeded);
        Assert.Equal("persistence_write_failed", rollback.Code);
        Assert.Equal(afterFirst, await ReadMutationManifestAsync(connection));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.fiscal_documents where payment_finality_ref='schema-reconcile-rollback-finality'"));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.idempotency_records where idempotency_key='schema-reconcile-rollback-finality'"));
        Assert.Equal(0, await ScalarLongAsync(connection,
            "select count(*) from pos.electronic_journal_records where idempotency_ref='schema-reconcile-rollback-finality'"));
    }

    private static async Task AssertCreatedStateAsync(
        NpgsqlConnection connection,
        Guid documentId,
        string eventReference,
        string priorStreamHead,
        long priorStreamSequence,
        DateTimeOffset executionStartedAt)
    {
        await using var command = new NpgsqlCommand("""
            select d.business_day_date,d.fiscal_reporting_period_id,d.fiscal_number_assigned_at,
                   d.created_at,d.completion_basis,d.completion_authority_ref,
                   (select count(*) from pos.idempotency_records i
                    join pos.controlled_codes status on status.controlled_code_id=i.operation_status_code_id
                    where i.linked_fiscal_document_id=d.fiscal_document_id and status.code_key='issued'),
                   (select count(*) from pos.fiscal_document_header_snapshots h where h.fiscal_document_id=d.fiscal_document_id),
                   (select count(*) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical),
                   (select count(*) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical and e.journal_context is null),
                   (select count(*) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical and char_length(e.printable_sales_invoice_text) > 0),
                   period.period_sequence,event.event_reference,event.previous_integrity_hash,event.stream_sequence_value,
                   event.semantic_hash ~ '^[0-9a-f]{64}$',event.integrity_hash ~ '^[0-9a-f]{64}$',
                   event.semantic_hash_version,event.journal_context is null
            from pos.fiscal_documents d
            join pos.fiscal_reporting_periods period on period.fiscal_reporting_period_id=d.fiscal_reporting_period_id
            join pos.electronic_journal_records event on event.fiscal_document_id=d.fiscal_document_id and event.is_canonical
            where d.fiscal_document_id=@document
            """, connection);
        command.Parameters.AddWithValue("document", documentId);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(new DateOnly(2026, 9, 8), reader.GetFieldValue<DateOnly>(0));
        Assert.Equal(HistoricalPeriodId, reader.GetGuid(1));
        Assert.True(reader.GetFieldValue<DateTimeOffset>(2) >= executionStartedAt.AddSeconds(-1));
        Assert.True(reader.GetFieldValue<DateTimeOffset>(3) >= executionStartedAt.AddSeconds(-1));
        Assert.Equal(reader.GetFieldValue<DateTimeOffset>(2), reader.GetFieldValue<DateTimeOffset>(3));
        Assert.Equal("PAYMENT_FINALITY", reader.GetString(4));
        Assert.Equal("e308ac46-10c9-4afa-8666-4f96eaa64dbd", reader.GetString(5));
        Assert.Equal(1, reader.GetInt64(6));
        Assert.Equal(1, reader.GetInt64(7));
        Assert.Equal(1, reader.GetInt64(8));
        Assert.Equal(1, reader.GetInt64(9));
        Assert.Equal(1, reader.GetInt64(10));
        Assert.Equal(4, reader.GetInt64(11));
        Assert.Equal(eventReference, reader.GetString(12));
        Assert.Equal(priorStreamHead, reader.GetString(13));
        Assert.Equal(priorStreamSequence + 1, reader.GetInt64(14));
        Assert.True(reader.GetBoolean(15));
        Assert.True(reader.GetBoolean(16));
        Assert.Equal(ElectronicJournalContract.CurrentSemanticHashVersion, reader.GetString(17));
        Assert.True(reader.GetBoolean(18));
        Assert.False(await reader.ReadAsync());
    }

    private static async Task<string> ReadHistoricalEjManifestAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("""
            select md5(coalesce(string_agg(concat_ws('|',
              electronic_journal_record_id,event_reference,stream_sequence_value,semantic_hash_version,
              semantic_hash,integrity_hash_version,integrity_hash,previous_integrity_hash,
              coalesce(journal_context::text,'<null>'),coalesce(printable_sales_invoice_text,'<null>')),
              ',' order by stream_sequence_value),''))
            from pos.electronic_journal_records
            where stream_sequence_value <= 10
            """, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<string> ReadMutationManifestAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("""
            select concat_ws('|',
              (select count(*) from pos.fiscal_documents),
              (select count(*) from pos.idempotency_records),
              (select count(*) from pos.fiscal_document_header_snapshots),
              (select count(*) from pos.electronic_journal_records),
              (select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@policy),
              (select last_reserved_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@policy),
              (select last_issued_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@policy),
              (select last_sequence_value from pos.electronic_journal_streams),
              (select last_event_hash from pos.electronic_journal_streams))
            """, connection);
        command.Parameters.AddWithValue("policy", SequencePolicyId);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<long> ScalarLongAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<string> ScalarStringAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    private static ElectronicJournalAppendRequest ToAppendRequest(ElectronicJournalEvent journalEvent) => new(
        journalEvent.SitePosServerId, journalEvent.FiscalIdentityId, journalEvent.CurrencyCode,
        journalEvent.FiscalReportingPeriodId, journalEvent.EventType, journalEvent.SourceTransitionReference,
        journalEvent.SourceTransitionVersion, journalEvent.EffectiveAt, journalEvent.ActorReference,
        journalEvent.ServiceIdentityReference, journalEvent.CorrelationReference, journalEvent.Facts,
        journalEvent.FiscalDocumentId, journalEvent.FiscalReportRequestId, journalEvent.XZReportId,
        journalEvent.BirSalesSummaryReportId, journalEvent.FiscalSequencePolicyId, journalEvent.BusinessDayDate,
        journalEvent.IdempotencyReference, journalEvent.ReprintRequestId, journalEvent.PrintableSalesInvoiceText);

    private static CreateFiscalDocumentRequest ExactHistoricalRequest()
    {
        var line = new CreateFiscalDocumentLineRequest(
            1, Guid.Parse("09997efc-e7b4-5cfb-8904-3020e01de4a9"), "Parking fee - cash", 1m,
            4500, 4500, 0, 0, 4500, "PHP", SourceRef: "e95ac359-0d8e-4f9c-b742-e41271abc11e",
            LineContext: new Dictionary<string, string>
            {
                ["source"] = "terminal-cash-payment",
                ["terminalCashTenderId"] = "a09ebfef-0610-4a25-a928-e423e0be02f3"
            });
        return new CreateFiscalDocumentRequest(
            "PITX-L3-WEBPAY-01", "sales_invoice",
            new FiscalizationPayableBasisRequest(
                "e95ac359-0d8e-4f9c-b742-e41271abc11e", PaymentFinalityRef, "PHP", 4500, [],
                new Dictionary<string, string>
                {
                    ["tariffSnapshotId"] = "e95ac359-0d8e-4f9c-b742-e41271abc11e",
                    ["paymentMethod"] = "CASH"
                }),
            SitePosServerId: Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc"),
            SiteId: Guid.Parse("2d1dcdf8-f563-537c-8542-0bde7cc9da97"),
            ChannelTerminalId: null,
            RuntimeTerminalRef: null,
            FiscalDocumentTypeCodeId: Guid.Parse("b9fc7458-57a7-5bbe-809d-8c8b3d780b1d"),
            FiscalDocumentStatusCodeId: Guid.Parse("9f868073-a648-5b1d-99c0-f96d598c4eb0"),
            BusinessDayDate: new DateOnly(2026, 9, 8),
            CentralPmsParkingSessionRef: "fa437973-e696-4304-beb7-9ab515748408",
            CentralPmsPaymentAttemptRef: "5855d1a9-2899-432c-ba9d-2be478f984bb",
            CentralPmsPaymentConfirmationRef: "e308ac46-10c9-4afa-8666-4f96eaa64dbd",
            UpstreamFinalityRef: PaymentFinalityRef,
            PaymentFinalityRef: PaymentFinalityRef,
            VendorAckRef: null,
            DocumentLines: [line],
            Lines: [line],
            Tenders:
            [
                new CreateFiscalTenderRequest(
                    Guid.Parse("a4bd3153-076e-564f-af56-3532be501e95"), 4500, "PHP",
                    "5855d1a9-2899-432c-ba9d-2be478f984bb", "e308ac46-10c9-4afa-8666-4f96eaa64dbd",
                    PaymentFinalityRef, "CASH", new Dictionary<string, string>
                    {
                        ["paymentMethod"] = "CASH",
                        ["terminalCashTenderId"] = "a09ebfef-0610-4a25-a928-e423e0be02f3"
                    })
            ],
            TaxDetails: [],
            DiscountPrivilegeDetails: [],
            Totals:
            [
                new CreateFiscalTotalRequest(Guid.Parse("6abfc6aa-b94e-5a24-b533-8d18a33c468b"), 4500, "PHP",
                    new Dictionary<string, string> { ["kind"] = "grand_total" })
            ],
            ReferenceContext: new Dictionary<string, string>
            {
                ["terminalCashTenderId"] = "a09ebfef-0610-4a25-a928-e423e0be02f3",
                ["cashCustodySessionId"] = "e18a8628-b1a1-4050-a2bc-cbac11fd7750",
                ["fiscalIssuanceReferenceId"] = "cd5b94a6-7e64-4f47-a7e7-e6254a2db5cf"
            });
    }

    private static bool TryGetDisposableProofConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var host = builder.Host ?? string.Empty;
        if (!string.Equals(builder.Database, ExpectedDatabase, StringComparison.Ordinal) ||
            host.Contains("persistent", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("prod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} must target only {ExpectedDatabase} on a disposable host.");
        }
        return true;
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PosServer"] = connectionString,
            ["POS_REQUIRE_COMPLETE_SALES_INVOICE_HEADER_PROFILE"] = "true",
            ["PosServer:Admin:ApiKeys:proof:Principal"] = "schema-reconciliation-proof",
            ["PosServer:Admin:ApiKeys:proof:Key"] = ProofApiKey,
            ["PosServer:Admin:ApiKeys:proof:Permission"] = FiscalDocumentAuthorization.CreatePermission
        });
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapFiscalDocumentEndpoints();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("Could not resolve proof API address.");
        var client = new HttpClient { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, ProofApiKey);
        return client;
    }
}
