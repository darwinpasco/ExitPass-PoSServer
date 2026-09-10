using System.Net;
using System.Net.Http.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalDocumentApiPostgresSmokeTests
{
    private const string ConnectionStringEnvironmentVariable = "POSSERVER_API_SMOKE_DB_URL";
    private const string SmokeApiKey = "synthetic-fiscal-document-smoke-key";
    private static readonly Guid SitePosServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FiscalDocumentTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000101");
    private static readonly Guid FiscalDocumentStatusCodeId = Guid.Parse("10000000-0000-0000-0000-000000000102");
    private static readonly Guid FiscalDocumentVoidedStatusCodeId = Guid.Parse("10000000-0000-0000-0000-000000000103");
    private static readonly Guid FiscalLineTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000201");
    private static readonly Guid FiscalTenderTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000301");
    private static readonly Guid FiscalTaxTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000401");
    private static readonly Guid FiscalTaxClassificationCodeId = Guid.Parse("10000000-0000-0000-0000-000000000402");
    private static readonly Guid FiscalVatExemptTaxClassificationCodeId = Guid.Parse("10000000-0000-0000-0000-000000000403");
    private static readonly Guid FiscalDiscountPrivilegeTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000501");
    private static readonly Guid FiscalTotalTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000601");
    private static readonly Guid FiscalIdentityId = Guid.Parse("10000000-0000-0000-0000-000000000701");
    private static readonly Guid FiscalSequenceFamilyCodeId = Guid.Parse("10000000-0000-0000-0000-000000000801");
    private static readonly Guid FiscalSequencePolicyStatusCodeId = Guid.Parse("10000000-0000-0000-0000-000000000802");
    private static readonly Guid FiscalSequencePolicyId = Guid.Parse("10000000-0000-0000-0000-000000000803");
    private static readonly Guid FiscalSequenceStateId = Guid.Parse("10000000-0000-0000-0000-000000000804");
    private static readonly Guid FiscalReportingPeriodId = Guid.Parse("10000000-0000-0000-0000-000000000805");
    private static readonly Guid HistoricalFiscalReportingPeriodId = Guid.Parse("10000000-0000-0000-0000-000000000806");

    private readonly ITestOutputHelper output;

    public FiscalDocumentApiPostgresSmokeTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public async Task PostFiscalDocumentWritesCompletePersistenceShell()
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = CreateValidRequest("success") with
        {
            InvoiceCustomerInformation = new InvoiceCustomerInformationRequest(
                "  Juan Dela Cruz  ",
                "  100 Sample Street  ",
                "  123-456-789  ",
                "  Sample Trading  ",
                "UNAPPROVED-ID-MUST-BE-DISCARDED")
        };

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("ordinary create", response.StatusCode, body);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal("accepted", body.Code);
        Assert.Equal("newly_created", body.ResultClassification);
        Assert.Equal("fiscal_document_number_assigned", body.FiscalIssuanceEvidenceStatus);
        Assert.Equal("assigned", body.FiscalNumberAssignmentState);
        Assert.NotNull(body.FiscalDocumentId);
        Assert.Equal(FiscalIdentityId, body.FiscalIdentityId);
        Assert.Equal(FiscalDocumentStatusCodeId, body.FiscalDocumentStatusCodeId);
        Assert.Equal(FiscalSequencePolicyId, body.FiscalSequencePolicyId);
        Assert.Equal(1, body.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", body.FiscalDocumentNumber);
        Assert.Equal("api-smoke-sequence-policy", body.FiscalSeries);
        Assert.Equal("SI-", body.FiscalNumberPrefixText);
        Assert.Equal("-A", body.FiscalNumberSuffixText);
        Assert.NotNull(body.FiscalNumberAssignedAt);
        Assert.Equal("pos-server:system", body.FiscalNumberAssignedByRef);

        var fiscalDocumentId = body.FiscalDocumentId.Value;
        using var getResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getBody);
        Assert.True(getBody.Succeeded);
        Assert.Equal("found", getBody.Code);
        Assert.Equal("fiscal_document_number_assigned", getBody.FiscalIssuanceEvidenceStatus);
        Assert.Equal("assigned", getBody.FiscalNumberAssignmentState);
        Assert.Equal(FiscalDocumentStatusCodeId, getBody.FiscalDocumentStatusCodeId);
        Assert.NotNull(getBody.Document);
        Assert.Equal(fiscalDocumentId, getBody.Document.FiscalDocumentId);
        Assert.Single(getBody.Document.StatusHistory);
        Assert.Empty(getBody.Document.DocumentLinks);
        Assert.Single(getBody.Document.Lines);
        Assert.Single(getBody.Document.Tenders);
        Assert.Single(getBody.Document.TaxDetails);
        Assert.Single(getBody.Document.DiscountPrivilegeDetails);
        Assert.Single(getBody.Document.Totals);
        Assert.Equal("central-finality-success", getBody.Document.PaymentFinalityRef);
        Assert.Equal("evidence-ref-success", getBody.Document.DiscountPrivilegeDetails[0].EvidenceRef);
        Assert.Equal(FiscalIdentityId, getBody.Document.FiscalIdentityId);
        Assert.Equal(FiscalSequencePolicyId, getBody.Document.FiscalSequencePolicyId);
        Assert.Equal(1, getBody.Document.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", getBody.Document.FiscalDocumentNumber);
        Assert.Equal("api-smoke-sequence-policy", getBody.Document.FiscalSeries);
        Assert.Equal("SI-", getBody.Document.FiscalNumberPrefixText);
        Assert.Equal("-A", getBody.Document.FiscalNumberSuffixText);
        Assert.NotNull(getBody.Document.FiscalNumberAssignedAt);
        Assert.Equal("pos-server:system", getBody.Document.FiscalNumberAssignedByRef);
        Assert.Equal(
            "fiscal_document_creation:10000000000000000000000000000001:10000000000000000000000000000101",
            getBody.Document.IdempotencyScope);
        Assert.Equal("central-finality-success", getBody.Document.IdempotencyKey);
        Assert.Equal("upstream_finality_ref", getBody.Document.IdempotencyKeySource);
        Assert.NotNull(getBody.Document.SemanticRequestHash);
        Assert.Equal(64, getBody.Document.SemanticRequestHash.Length);
        Assert.Equal("pos-server-fiscal-document-create:sha256:v4", getBody.Document.SemanticRequestHashVersion);
        Assert.Equal("matched", getBody.Document.SemanticRequestHashStatus);
        Assert.NotNull(getBody.Document.InvoiceCustomerInformation);
        Assert.Equal("Juan Dela Cruz", getBody.Document.InvoiceCustomerInformation.CustomerName);
        Assert.Equal("100 Sample Street", getBody.Document.InvoiceCustomerInformation.Address);
        Assert.Equal("123-456-789", getBody.Document.InvoiceCustomerInformation.Tin);
        Assert.Equal("Sample Trading", getBody.Document.InvoiceCustomerInformation.BusinessStyle);
        Assert.Null(getBody.Document.InvoiceCustomerInformation.StatutoryIdNumber);
        Assert.Equal("vatable", getBody.Document.TaxDetails.Single().TaxClassificationCodeKey);

        using var presentationResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation");
        var presentationBody = await presentationResponse.Content.ReadFromJsonAsync<GetDigitalSalesInvoicePresentationResponse>();
        Assert.Equal(HttpStatusCode.OK, presentationResponse.StatusCode);
        Assert.NotNull(presentationBody?.Presentation);
        var customerSection = presentationBody.Presentation.Sections.Single(section => section.Name == "customerInformation");
        Assert.Contains(customerSection.Rows, row => row.Key == "customerInformation.customerName" && row.DisplayValue == "Juan Dela Cruz");
        var vatSection = presentationBody.Presentation.Sections.Single(section => section.Name == "vatBreakdown");
        Assert.Equal(4, vatSection.Rows.Count);
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatableSales" && row.DisplayValue == "PHP 125.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatAmount" && row.DisplayValue == "PHP 0.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatExemptSales" && row.DisplayValue == "PHP 0.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.zeroRatedSales" && row.DisplayValue == "PHP 0.00");

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replayBody = await replayResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        Assert.NotNull(replayBody);
        Assert.True(replayBody.Succeeded);
        Assert.Equal("idempotent_replay", replayBody.ResultClassification);
        Assert.Equal("assigned", replayBody.FiscalNumberAssignmentState);
        Assert.Equal("fiscal_document_number_assigned", replayBody.FiscalIssuanceEvidenceStatus);
        Assert.Equal(fiscalDocumentId, replayBody.FiscalDocumentId);
        Assert.Equal(FiscalDocumentStatusCodeId, replayBody.FiscalDocumentStatusCodeId);
        Assert.Equal(1, replayBody.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", replayBody.FiscalDocumentNumber);

        var conflictRequest = request with
        {
            InvoiceCustomerInformation = request.InvoiceCustomerInformation! with { CustomerName = "Changed Customer" }
        };
        using var conflictResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", conflictRequest);
        var conflictBody = await conflictResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.NotNull(conflictBody);
        Assert.False(conflictBody.Succeeded);
        Assert.Equal("fiscal_document_idempotency_conflict", conflictBody.Code);
        Assert.Equal("do_not_retry_without_request_change", conflictBody.ErrorPosture);
        Assert.Equal("not_assigned", conflictBody.FiscalNumberAssignmentState);

        using var missingGetResponse = await client.GetAsync($"/v1/fiscal-documents/{Guid.Parse("99999999-9999-9999-9999-999999999999")}");
        var missingGetBody = await missingGetResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.NotFound, missingGetResponse.StatusCode);
        Assert.NotNull(missingGetBody);
        Assert.False(missingGetBody.Succeeded);
        Assert.Equal("fiscal_document_not_found", missingGetBody.Code);
        Assert.Equal("not_assigned", missingGetBody.FiscalNumberAssignmentState);

        var voidRequest = new VoidFiscalDocumentRequest(
            "void-key-success",
            "operator_error",
            "Smoke void correction",
            "api-smoke-operator",
            DateTimeOffset.Parse("2026-07-09T04:30:00Z"),
            "corr-void-success",
            "api-smoke",
            new DateOnly(2026, 7, 1));
        using var voidResponse = await client.PostAsJsonAsync($"/v1/fiscal-documents/{fiscalDocumentId}/void", voidRequest);
        var voidBody = await voidResponse.Content.ReadFromJsonAsync<VoidFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, voidResponse.StatusCode);
        Assert.NotNull(voidBody);
        Assert.True(voidBody.Succeeded);
        Assert.Equal("accepted", voidBody.Code);
        Assert.Equal("newly_voided", voidBody.ResultClassification);
        Assert.Equal(fiscalDocumentId, voidBody.FiscalDocumentId);
        Assert.Equal("SI-00000001-A", voidBody.FiscalDocumentNumber);
        Assert.Equal(1, voidBody.FiscalSequenceValue);
        Assert.Equal("voided", voidBody.FiscalDocumentStatus);
        Assert.Equal("recorded", voidBody.VoidStatus);

        using var voidReplayResponse = await client.PostAsJsonAsync($"/v1/fiscal-documents/{fiscalDocumentId}/void", voidRequest);
        var voidReplayBody = await voidReplayResponse.Content.ReadFromJsonAsync<VoidFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, voidReplayResponse.StatusCode);
        Assert.NotNull(voidReplayBody);
        Assert.True(voidReplayBody.Succeeded);
        Assert.Equal("idempotent_replay", voidReplayBody.ResultClassification);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_documents", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(2, await CountAsync(connection, "pos.fiscal_document_status_history", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(0, await CountAsync(connection, "pos.fiscal_document_links", "source_fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_lines", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_tenders", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_tax_details", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_discount_privilege_details", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_totals", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(2, await CountAsync(connection, "pos.idempotency_records", "linked_fiscal_document_id = @id", "id", fiscalDocumentId));

        Assert.Equal("central-finality-success", await ScalarStringAsync(
            connection,
            "select payment_finality_ref from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        var semanticRequestHash = await ScalarStringAsync(
            connection,
            "select semantic_request_hash from pos.idempotency_records where linked_fiscal_document_id = @id",
            fiscalDocumentId);
        Assert.NotNull(semanticRequestHash);
        Assert.Equal(64, semanticRequestHash.Length);
        Assert.Equal("pos-server-fiscal-document-create:sha256:v4", await ScalarStringAsync(
            connection,
            "select idempotency_context ->> 'semantic_request_hash_version' from pos.idempotency_records where linked_fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal("Juan Dela Cruz", await ScalarStringAsync(
            connection,
            "select document_context -> 'invoice_customer_information' ->> 'customer_name' from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal("evidence-ref-success", await ScalarStringAsync(
            connection,
            "select evidence_ref from pos.fiscal_discount_privilege_details where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal(1, await ScalarLongAsync(
            connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
            FiscalSequencePolicyId));
        Assert.Equal(1, await ScalarLongAsync(
            connection,
            "select last_issued_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
            FiscalSequencePolicyId));
        Assert.Equal("recorded", await ScalarStringAsync(
            connection,
            "select void_status from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal("operator_error", await ScalarStringAsync(
            connection,
            "select void_reason_code from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal("void-key-success", await ScalarStringAsync(
            connection,
            "select void_idempotency_key from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal(FiscalDocumentVoidedStatusCodeId.ToString("D"), await ScalarStringAsync(
            connection,
            "select fiscal_document_status_code_id::text from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));

        Assert.Equal(0, await CountTextMarkerAsync(connection, "raw_id"));
        Assert.Equal(0, await CountTextMarkerAsync(connection, "payment_payload"));

        foreach (var prohibitedTable in ProhibitedRuntimeTables())
        {
            Assert.Equal(0, await CountIfTableExistsAsync(connection, prohibitedTable));
        }

        Assert.Equal(0, await CountTablesLikeAsync(connection, "%exit%"));
        Assert.Equal(0, await CountTablesLikeAsync(connection, "%gate%"));
    }

    [Fact]
    public async Task PostZeroPayableStatutoryFiscalDocumentPersistsSalesInvoiceAndEjWithoutTenderOrPaymentAncestry()
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = CreateValidZeroPayableStatutoryRequest("zero-payable");

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var responseText = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Accepted,
            $"Unexpected POS response {response.StatusCode}: {responseText}");
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal("newly_created", body.ResultClassification);
        Assert.Equal("ZERO_PAYABLE_STATUTORY_FINALITY", body.CompletionBasis);
        Assert.Equal(request.CompletionAuthorityRef, body.CompletionAuthorityRef);
        Assert.False(string.IsNullOrWhiteSpace(body.ElectronicJournalEventReference));
        var fiscalDocumentId = body.FiscalDocumentId!.Value;

        using var getResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getBody?.Document);
        Assert.Equal("ZERO_PAYABLE_STATUTORY_FINALITY", getBody.Document.CompletionBasis);
        Assert.Equal(request.CompletionAuthorityRef, getBody.Document.CompletionAuthorityRef);
        Assert.Null(getBody.Document.CentralPmsPaymentAttemptRef);
        Assert.Null(getBody.Document.CentralPmsPaymentConfirmationRef);
        Assert.Null(getBody.Document.PaymentFinalityRef);
        Assert.Empty(getBody.Document.Tenders);
        Assert.Equal(0, Assert.Single(getBody.Document.Totals).AmountMinorUnits);
        Assert.Equal(body.ElectronicJournalEventReference, getBody.Document.ElectronicJournalEventReference);
        Assert.Equal("OSCA-12345", getBody.Document.InvoiceCustomerInformation!.StatutoryIdNumber);
        Assert.Equal("vatable", getBody.Document.TaxDetails.Single().TaxClassificationCodeKey);
        Assert.Equal("pos-server-fiscal-document-create:sha256:v4", getBody.Document.SemanticRequestHashVersion);

        using var presentationResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation");
        var presentationBody = await presentationResponse.Content.ReadFromJsonAsync<GetDigitalSalesInvoicePresentationResponse>();
        Assert.Equal(HttpStatusCode.OK, presentationResponse.StatusCode);
        Assert.NotNull(presentationBody?.Presentation);
        var vatSection = presentationBody.Presentation.Sections.Single(section => section.Name == "vatBreakdown");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatableSales" && row.DisplayValue == "PHP 26.79");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatAmount" && row.DisplayValue == "PHP 3.21");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatExemptSales" && row.DisplayValue == "PHP 0.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.zeroRatedSales" && row.DisplayValue == "PHP 0.00");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(1, await CountAsync(
            connection,
            "pos.fiscal_documents",
            "fiscal_document_id = @id",
            "id",
            fiscalDocumentId));
        Assert.Equal(1, await CountAsync(
            connection,
            "pos.electronic_journal_records",
            "fiscal_document_id = @id and is_canonical",
            "id",
            fiscalDocumentId));
        Assert.Equal("ZERO_PAYABLE_STATUTORY_FINALITY", await ScalarStringAsync(
            connection,
            "select event_facts ->> 'completion_basis' from pos.electronic_journal_records where fiscal_document_id = @id and is_canonical;",
            fiscalDocumentId));
        Assert.Equal("false", await ScalarStringAsync(
            connection,
            "select event_facts ->> 'monetary_payment_received' from pos.electronic_journal_records where fiscal_document_id = @id and is_canonical;",
            fiscalDocumentId));
        Assert.Equal("0", await ScalarStringAsync(
            connection,
            "select event_facts ->> 'payable_amount_minor_units' from pos.electronic_journal_records where fiscal_document_id = @id and is_canonical;",
            fiscalDocumentId));

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replayBody = await replayResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        Assert.Equal("idempotent_replay", replayBody!.ResultClassification);
        Assert.Equal(fiscalDocumentId, replayBody.FiscalDocumentId);
        Assert.Equal(body.ElectronicJournalEventReference, replayBody.ElectronicJournalEventReference);
        Assert.Equal(1, await CountAsync(
            connection,
            "pos.electronic_journal_records",
            "fiscal_document_id = @id and is_canonical",
            "id",
            fiscalDocumentId));

        var conflict = request with
        {
            InvoiceCustomerInformation = request.InvoiceCustomerInformation! with { CustomerName = "Conflicting Customer" }
        };
        using var conflictResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", conflict);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task DelayedIssuanceResolvesUniqueOpenHistoricalPeriodByImmutableBusinessDate()
    {
        if (!TryGetSmokeConnectionString(out var connectionString)) return;

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await ConfigureHistoricalAndCurrentPeriodsAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var businessDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var request = CreateValidRequest("delayed-business-date") with { BusinessDayDate = businessDate };

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.True(body?.Succeeded);
        Assert.Equal("newly_created", body?.ResultClassification);
        Assert.NotNull(body?.FiscalDocumentId);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var proof = new NpgsqlCommand(
            """
            select d.fiscal_reporting_period_id,d.business_day_date,d.created_at,p.period_end_at,
                   (select count(*) from pos.fiscal_documents x where x.central_pms_payment_attempt_ref=@attempt),
                   (select count(*) from pos.idempotency_records i where i.linked_fiscal_document_id=d.fiscal_document_id),
                   (select count(*) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical),
                   (select min(e.business_day_date) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical),
                   (select min(e.recorded_at) from pos.electronic_journal_records e where e.fiscal_document_id=d.fiscal_document_id and e.is_canonical),
                   d.fiscal_document_number,
                   (select count(*) from pos.fiscal_documents x where x.fiscal_reporting_period_id=@current_period),
                   (select count(*) from pos.fiscal_document_lines x where x.fiscal_document_id=d.fiscal_document_id),
                   (select count(*) from pos.fiscal_tenders x where x.fiscal_document_id=d.fiscal_document_id),
                   (select count(*) from pos.fiscal_totals x where x.fiscal_document_id=d.fiscal_document_id)
            from pos.fiscal_documents d
            join pos.fiscal_reporting_periods p on p.fiscal_reporting_period_id=d.fiscal_reporting_period_id
            where d.fiscal_document_id=@document;
            """, connection))
        {
            proof.Parameters.AddWithValue("document", body!.FiscalDocumentId!.Value);
            proof.Parameters.AddWithValue("attempt", "payment-attempt-delayed-business-date");
            proof.Parameters.AddWithValue("current_period", FiscalReportingPeriodId);
            await using var reader = await proof.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(HistoricalFiscalReportingPeriodId, reader.GetGuid(0));
            Assert.Equal(businessDate, reader.GetFieldValue<DateOnly>(1));
            Assert.True(reader.GetFieldValue<DateTimeOffset>(2) >= reader.GetFieldValue<DateTimeOffset>(3));
            Assert.Equal(1, reader.GetInt64(4));
            Assert.Equal(1, reader.GetInt64(5));
            Assert.Equal(1, reader.GetInt64(6));
            Assert.Equal(businessDate, reader.GetFieldValue<DateOnly>(7));
            Assert.True(reader.GetFieldValue<DateTimeOffset>(8) >= reader.GetFieldValue<DateTimeOffset>(3));
            Assert.StartsWith("SI-", reader.GetString(9), StringComparison.Ordinal);
            Assert.Equal(0, reader.GetInt64(10));
            Assert.Equal(1, reader.GetInt64(11));
            Assert.Equal(1, reader.GetInt64(12));
            Assert.Equal(1, reader.GetInt64(13));
        }

        using var replay = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replayBody = await replay.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Accepted, replay.StatusCode);
        Assert.Equal("idempotent_replay", replayBody?.ResultClassification);
        Assert.Equal(body!.FiscalDocumentId, replayBody?.FiscalDocumentId);

        using var changedBusinessDate = await client.PostAsJsonAsync(
            "/v1/fiscal-documents/",
            request with { BusinessDayDate = businessDate.AddDays(1) });
        var conflict = await changedBusinessDate.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, changedBusinessDate.StatusCode);
        Assert.Equal("fiscal_document_idempotency_conflict", conflict?.Code);

        var mutation = new NpgsqlCommand(
            "update pos.fiscal_documents set business_day_date=business_day_date+1 where fiscal_document_id=@document",
            connection);
        mutation.Parameters.AddWithValue("document", body.FiscalDocumentId.Value);
        var mutationFailure = await Assert.ThrowsAsync<PostgresException>(() => mutation.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.CheckViolation, mutationFailure.SqlState);
    }

    [Fact]
    public async Task BusinessDatePeriodResolutionFailsClosedWhenMissingAmbiguousOrClosed()
    {
        if (!TryGetSmokeConnectionString(out var connectionString)) return;

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        using var missing = await client.PostAsJsonAsync(
            "/v1/fiscal-documents/",
            CreateValidRequest("missing-period") with { BusinessDayDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)) });
        var missingBody = await missing.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
        Assert.Equal("fiscal_reporting_period_unavailable", missingBody?.Code);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await ExecuteSqlAsync(connection,
                """
                update pos.fiscal_reporting_periods set period_sequence=2 where fiscal_reporting_period_id=@current;
                insert into pos.fiscal_reporting_periods(
                    fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,
                    period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,
                    business_day_cutoff_local_time,currency_code,period_sequence,opened_at,created_by_ref,updated_by_ref)
                values(@historical,'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',@site,@identity,
                    '1a6f7021-bc84-5c01-afaa-c5d6685633c8',current_date,
                    transaction_timestamp()-interval '3 days',transaction_timestamp()-interval '2 days',
                    'Etc/UTC','00:00:00','PHP',1,transaction_timestamp()-interval '3 days','LATE-ISSUANCE-TEST','LATE-ISSUANCE-TEST');
                """,
                command =>
                {
                    command.Parameters.AddWithValue("current", FiscalReportingPeriodId);
                    command.Parameters.AddWithValue("historical", HistoricalFiscalReportingPeriodId);
                    command.Parameters.AddWithValue("site", SitePosServerId);
                    command.Parameters.AddWithValue("identity", FiscalIdentityId);
                });
        }

        using var ambiguous = await client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("ambiguous-period"));
        var ambiguousBody = await ambiguous.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, ambiguous.StatusCode);
        Assert.Equal("fiscal_reporting_period_ambiguous", ambiguousBody?.Code);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await ExecuteSqlAsync(connection,
                "delete from pos.fiscal_reporting_periods where fiscal_reporting_period_id=@historical; update pos.fiscal_reporting_periods set period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94',closing_started_at=clock_timestamp(),closed_at=clock_timestamp() where fiscal_reporting_period_id=@current",
                command =>
                {
                    command.Parameters.AddWithValue("historical", HistoricalFiscalReportingPeriodId);
                    command.Parameters.AddWithValue("current", FiscalReportingPeriodId);
                });
        }

        using var closed = await client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("closed-period"));
        var closedBody = await closed.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
        Assert.Equal("fiscal_reporting_period_closed", closedBody?.Code);

        await using var verify = new NpgsqlConnection(connectionString);
        await verify.OpenAsync();
        Assert.Equal(0, await CountAsync(verify, "pos.fiscal_documents"));
        Assert.Equal(0, await CountAsync(verify, "pos.idempotency_records"));
        Assert.Equal(0, await CountAsync(verify, "pos.electronic_journal_records"));
        Assert.Equal(0, await ScalarLongAsync(verify,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@id",
            FiscalSequencePolicyId));
    }

    [Fact]
    public async Task ClosedReportingPeriodRejectsIssuanceAndVoidWithoutAllocatingAnotherFiscalNumber()
    {
        if (!TryGetSmokeConnectionString(out var connectionString)) return;

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        using var createdResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("boundary-open"));
        var created = await createdResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Accepted, createdResponse.StatusCode);
        Assert.NotNull(created?.FiscalDocumentId);

        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await ExecuteSqlAsync(connection,
                "UPDATE pos.fiscal_reporting_periods SET period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94', closing_started_at=clock_timestamp(), closed_at=clock_timestamp(), updated_at=clock_timestamp(), updated_by_ref='Z007B-SMOKE' WHERE fiscal_reporting_period_id=@period",
                command => command.Parameters.AddWithValue("period", FiscalReportingPeriodId));
        }

        using var rejectedCreateResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("boundary-closed"));
        var rejectedCreate = await rejectedCreateResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, rejectedCreateResponse.StatusCode);
        Assert.Equal("fiscal_reporting_period_closed", rejectedCreate?.Code);

        using var rejectedVoidResponse = await client.PostAsJsonAsync(
            $"/v1/fiscal-documents/{created!.FiscalDocumentId}/void",
            new VoidFiscalDocumentRequest("boundary-void", "customer_request", null, "synthetic-operator", DateTimeOffset.UtcNow, "boundary-correlation", null, DateOnly.FromDateTime(DateTime.UtcNow)));
        var rejectedVoid = await rejectedVoidResponse.Content.ReadFromJsonAsync<VoidFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, rejectedVoidResponse.StatusCode);
        Assert.Equal("unsupported_cross_period_fiscal_mutation", rejectedVoid?.Code);

        await using var verify = new NpgsqlConnection(connectionString);
        await verify.OpenAsync();
        Assert.Equal(1, await ScalarLongAsync(verify,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@id",
            FiscalSequencePolicyId));
        Assert.Equal(1, await CountAsync(verify, "pos.fiscal_documents"));
        Assert.Equal(FiscalReportingPeriodId, await ScalarGuidAsync(verify,
            "select fiscal_reporting_period_id from pos.fiscal_documents where fiscal_document_id=@id",
            created.FiscalDocumentId!.Value));
        Assert.Equal(0, await CountAsync(verify, "pos.fiscal_document_status_history", "new_fiscal_document_status_code_id=@id", "id", FiscalDocumentVoidedStatusCodeId));
    }

    [Fact]
    public async Task CloseBoundaryAcquiredFirstMakesIssuanceWaitThenRejectsWithoutFiscalRows()
    {
        if (!TryGetSmokeConnectionString(out var connectionString)) return;

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        await using var boundaryConnection = new NpgsqlConnection(connectionString);
        await boundaryConnection.OpenAsync();
        await using var boundaryTransaction = await boundaryConnection.BeginTransactionAsync();
        await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
            boundaryConnection, boundaryTransaction, SitePosServerId, FiscalIdentityId, "PHP", default);
        await PostgresFiscalCloseBoundaryCoordinator.ResolveAndLockOpenPeriodAsync(
            boundaryConnection, boundaryTransaction, SitePosServerId, FiscalIdentityId, "PHP", default);

        var pendingIssuance = client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("close-first"));
        Assert.NotSame(pendingIssuance, await Task.WhenAny(pendingIssuance, Task.Delay(200)));

        await ExecuteSqlAsync(boundaryConnection,
            "UPDATE pos.fiscal_reporting_periods SET period_status_code_id='af7ee931-a023-507e-81a4-17adf047eb94', closing_started_at=clock_timestamp(), closed_at=clock_timestamp(), updated_at=clock_timestamp(), updated_by_ref='Z007B-SMOKE' WHERE fiscal_reporting_period_id=@period",
            command =>
            {
                command.Transaction = boundaryTransaction;
                command.Parameters.AddWithValue("period", FiscalReportingPeriodId);
            });
        await boundaryTransaction.CommitAsync();

        using var rejected = await pendingIssuance;
        var body = await rejected.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal("fiscal_reporting_period_closed", body?.Code);

        await using var verify = new NpgsqlConnection(connectionString);
        await verify.OpenAsync();
        Assert.Equal(0, await CountAsync(verify, "pos.fiscal_documents"));
        Assert.Equal(0, await ScalarLongAsync(verify,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id=@id",
            FiscalSequencePolicyId));
    }

    [Fact]
    public async Task IssuanceAcquiredFirstMakesBoundaryWaitAndObserveCompleteCommittedDocument()
    {
        if (!TryGetSmokeConnectionString(out var connectionString)) return;

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);
        await using (var setup = new NpgsqlConnection(connectionString))
        {
            await setup.OpenAsync();
            await ExecuteSqlAsync(setup, """
                CREATE OR REPLACE FUNCTION pos.delay_z007b_fiscal_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN PERFORM pg_sleep(0.5); RETURN NEW; END; $$;
                CREATE TRIGGER trg_z007b_delay_fiscal_insert BEFORE INSERT ON pos.fiscal_documents
                FOR EACH ROW EXECUTE FUNCTION pos.delay_z007b_fiscal_insert();
                """);
        }

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var issuance = client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("issuance-first"));
        await WaitForAdvisoryLockAsync(connectionString);

        var boundary = ObserveCommittedDocumentAfterBoundaryAsync(connectionString);

        using var response = await issuance;
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, await boundary);
    }

    private static async Task<long> ObserveCommittedDocumentAfterBoundaryAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await PostgresFiscalCloseBoundaryCoordinator.AcquireAsync(
            connection, transaction, SitePosServerId, FiscalIdentityId, "PHP", default);
        await PostgresFiscalCloseBoundaryCoordinator.ResolveAndLockOpenPeriodAsync(
            connection, transaction, SitePosServerId, FiscalIdentityId, "PHP", default);
        var documentCount = await CountAsync(connection, "pos.fiscal_documents");
        await transaction.RollbackAsync();
        return documentCount;
    }

    private static async Task WaitForAdvisoryLockAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        for (var attempt = 0; attempt < 100; attempt++)
        {
            await using var command = new NpgsqlCommand(
                "select count(*) from pg_locks where locktype='advisory' and granted", connection);
            if (Convert.ToInt64(await command.ExecuteScalarAsync()) > 0) return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Fiscal issuance did not acquire the governed advisory lock.");
    }

    [Fact]
    public async Task PostAppliedStatutoryFiscalDocumentPersistsReadbackAndPresentationSnapshot()
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = CreateValidStatutoryRequest("statutory-senior");

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("statutory senior create", response.StatusCode, body);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal("newly_created", body.ResultClassification);
        Assert.NotNull(body.FiscalDocumentId);

        var fiscalDocumentId = body.FiscalDocumentId.Value;
        using var getResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getBody?.Document);
        Assert.NotNull(getBody.Document.AppliedStatutoryFiscalFacts);
        Assert.Null(getBody.Document.SemanticRequestHash);
        Assert.Equal("pos-server-fiscal-document-create:sha256:v4", getBody.Document.SemanticRequestHashVersion);
        Assert.Equal("SENIOR_CITIZEN", getBody.Document.AppliedStatutoryFiscalFacts.EntitlementType);
        Assert.Equal("VAT_EXEMPTION_AND_STATUTORY_DISCOUNT", getBody.Document.AppliedStatutoryFiscalFacts.BenefitClassification);
        Assert.Equal("VAT_EXEMPT", getBody.Document.AppliedStatutoryFiscalFacts.VatTreatment);
        Assert.Equal(7143, getBody.Document.AppliedStatutoryFiscalFacts.FinalPayableAmountMinorUnits);
        Assert.Equal("OSCA-12345", getBody.Document.InvoiceCustomerInformation!.StatutoryIdNumber);
        Assert.Equal("vat_exempt", getBody.Document.TaxDetails.Single().TaxClassificationCodeKey);

        using var presentationResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation");
        var presentationBody = await presentationResponse.Content.ReadFromJsonAsync<GetDigitalSalesInvoicePresentationResponse>();

        Assert.Equal(HttpStatusCode.OK, presentationResponse.StatusCode);
        Assert.NotNull(presentationBody?.Presentation);
        var statutorySection = Assert.Single(
            presentationBody.Presentation.Sections,
            section => section.Name == "appliedStatutoryFiscalFacts");
        Assert.Contains(
            statutorySection.Rows,
            row => row.Key == "appliedStatutoryFiscalFacts.benefitClassification" &&
                string.Equals(row.DisplayValue, "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT", StringComparison.Ordinal));
        Assert.DoesNotContain(
            statutorySection.Rows,
            row => row.Key.Contains("decision", StringComparison.OrdinalIgnoreCase) ||
                row.Key.Contains("evidence", StringComparison.OrdinalIgnoreCase));
        var customerSection = presentationBody.Presentation.Sections.Single(section => section.Name == "customerInformation");
        Assert.Contains(customerSection.Rows, row =>
            row.Key == "customerInformation.statutoryIdNumber" && row.DisplayValue == "OSCA-12345");
        var vatSection = presentationBody.Presentation.Sections.Single(section => section.Name == "vatBreakdown");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatableSales" && row.DisplayValue == "PHP 0.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatAmount" && row.DisplayValue == "PHP 0.00");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.vatExemptSales" && row.DisplayValue == "PHP 89.29");
        Assert.Contains(vatSection.Rows, row => row.Key == "totals.zeroRatedSales" && row.DisplayValue == "PHP 0.00");

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replayBody = await replayResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("statutory senior replay", replayResponse.StatusCode, replayBody);

        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        Assert.NotNull(replayBody);
        Assert.True(replayBody.Succeeded);
        Assert.Equal("idempotent_replay", replayBody.ResultClassification);
        Assert.Equal(fiscalDocumentId, replayBody.FiscalDocumentId);

        var conflictRequest = request with
        {
            AppliedStatutoryFiscalFacts = request.AppliedStatutoryFiscalFacts! with
            {
                StatutoryRequestReference = Guid.Parse("21000000-0000-4000-8000-000000009999")
            }
        };
        using var conflictResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", conflictRequest);
        var conflictBody = await conflictResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("statutory material conflict", conflictResponse.StatusCode, conflictBody);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.NotNull(conflictBody);
        Assert.False(conflictBody.Succeeded);
        Assert.Equal("fiscal_document_idempotency_conflict", conflictBody.Code);

        var invalidRequest = CreateValidStatutoryRequest("invalid-finality") with
        {
            AppliedStatutoryFiscalFacts = CreateValidStatutoryRequest("invalid-finality").AppliedStatutoryFiscalFacts! with
            {
                AppliedTariffSnapshotId = Guid.Parse("21000000-0000-4000-8000-000000000009")
            }
        };
        using var invalidResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", invalidRequest);
        var invalidBody = await invalidResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("statutory invalid finality", invalidResponse.StatusCode, invalidBody);

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.NotNull(invalidBody);
        Assert.False(invalidBody.Succeeded);
        Assert.Equal("applied_statutory_facts_not_final", invalidBody.Code);

        var pwdRequest = CreateValidPwdStatutoryRequest("statutory-pwd");
        using var pwdResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", pwdRequest);
        var pwdBody = await pwdResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        WriteCreateResult("statutory pwd create", pwdResponse.StatusCode, pwdBody);

        Assert.Equal(HttpStatusCode.Accepted, pwdResponse.StatusCode);
        Assert.NotNull(pwdBody);
        Assert.True(pwdBody.Succeeded);
        Assert.Equal(2, pwdBody.FiscalSequenceValue);
        Assert.NotNull(pwdBody.FiscalDocumentId);

        using var pwdGetResponse = await client.GetAsync($"/v1/fiscal-documents/{pwdBody.FiscalDocumentId}");
        var pwdGetBody = await pwdGetResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, pwdGetResponse.StatusCode);
        Assert.NotNull(pwdGetBody?.Document?.AppliedStatutoryFiscalFacts);
        Assert.Equal("PWD", pwdGetBody.Document.AppliedStatutoryFiscalFacts.EntitlementType);
        Assert.Equal("ASSISTED_PAYMENT_TERMINAL", pwdGetBody.Document.AppliedStatutoryFiscalFacts.SourcePaymentChannel);
        Assert.Equal(Guid.Parse("22000000-0000-4000-8000-000000000011"), pwdGetBody.Document.AppliedStatutoryFiscalFacts.TerminalCashTenderId);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_documents", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_applied_statutory_facts", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_applied_statutory_facts", "fiscal_document_id = @id", "id", pwdBody.FiscalDocumentId!.Value));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_status_history", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(2, await ScalarLongAsync(
            connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
            FiscalSequencePolicyId));
        Assert.Equal("pos-server-fiscal-document-create:sha256:v4", await ScalarStringAsync(
            connection,
            "select idempotency_context ->> 'semantic_request_hash_version' from pos.idempotency_records where linked_fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal(0, await CountTextMarkerAsync(connection, "beneficiary_name"));
        Assert.Equal(0, await CountTextMarkerAsync(connection, "evidence_image"));
    }

    [Fact]
    public async Task LatePersistenceFailureRollsBackFiscalDocumentShell()
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = CreateValidRequest("rollback") with
        {
            Totals =
            [
                new CreateFiscalTotalRequest(
                    Guid.Parse("10000000-0000-0000-0000-000000009999"),
                    12500,
                    "PHP",
                    new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ]
        };

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Succeeded);
        Assert.Equal("persistence_write_failed", body.Code);
        Assert.Equal("not_assigned", body.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_service_recovery", body.ErrorPosture);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Assert.Equal(0, await CountAsync(
            connection,
            "pos.fiscal_documents",
            "payment_finality_ref = @ref",
            "ref",
            "central-finality-rollback"));
        Assert.Equal(0, await CountAsync(
            connection,
            "pos.fiscal_document_lines",
            "source_ref = @ref",
            "ref",
            "line-source-rollback"));
        Assert.Equal(0, await CountAsync(
            connection,
            "pos.fiscal_tenders",
            "payment_finality_ref = @ref",
            "ref",
            "central-finality-rollback"));
        Assert.Equal(0, await CountAsync(
            connection,
            "pos.fiscal_discount_privilege_details",
            "approval_ref = @ref",
            "ref",
            "discount-validation-rollback"));
        Assert.Equal(0, await ScalarLongAsync(
            connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
            FiscalSequencePolicyId));
    }

    [Fact]
    public async Task MissingFiscalIdentityBlocksNumberingAllocation()
    {
        await AssertNumberingPrerequisiteFailureAsync(
            async connection =>
            {
                await ExecuteSqlAsync(
                    connection,
                    "delete from pos.site_pos_server_fiscal_identity_history where site_pos_server_id = @site_pos_server_id;",
                    command => command.Parameters.AddWithValue("site_pos_server_id", SitePosServerId));
            },
            "fiscal_identity_not_found");
    }

    [Fact]
    public async Task MissingSequencePolicyBlocksNumberingAllocation()
    {
        await AssertNumberingPrerequisiteFailureAsync(
            async connection =>
            {
                await ExecuteSqlAsync(
                    connection,
                    "delete from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @fiscal_sequence_policy_id;",
                    command => command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId));
                await ExecuteSqlAsync(
                    connection,
                    "delete from pos.fiscal_sequence_policies where fiscal_sequence_policy_id = @fiscal_sequence_policy_id;",
                    command => command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId));
            },
            "fiscal_sequence_policy_not_found");
    }

    [Fact]
    public async Task MissingSequenceStateBlocksNumberingAllocation()
    {
        await AssertNumberingPrerequisiteFailureAsync(
            async connection =>
            {
                await ExecuteSqlAsync(
                    connection,
                    "delete from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @fiscal_sequence_policy_id;",
                    command => command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId));
            },
            "fiscal_sequence_state_not_found");
    }

    [Fact]
    public async Task ConcurrentFiscalDocumentPostsReceiveDistinctFiscalNumbers()
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        var firstTask = client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("concurrent-a"));
        var secondTask = client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest("concurrent-b"));
        await Task.WhenAll(firstTask, secondTask);

        using var firstResponse = await firstTask;
        using var secondResponse = await secondTask;
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, secondResponse.StatusCode);
        Assert.NotNull(firstBody);
        Assert.NotNull(secondBody);
        Assert.True(firstBody.Succeeded);
        Assert.True(secondBody.Succeeded);
        Assert.Equal("newly_created", firstBody.ResultClassification);
        Assert.Equal("newly_created", secondBody.ResultClassification);
        Assert.Equal("assigned", firstBody.FiscalNumberAssignmentState);
        Assert.Equal("assigned", secondBody.FiscalNumberAssignmentState);
        Assert.NotEqual(firstBody.FiscalDocumentId, secondBody.FiscalDocumentId);
        Assert.NotEqual(firstBody.FiscalDocumentNumber, secondBody.FiscalDocumentNumber);
        Assert.Equal([1L, 2L], new[] { firstBody.FiscalSequenceValue!.Value, secondBody.FiscalSequenceValue!.Value }.Order());

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(2, await ScalarLongAsync(
            connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
            FiscalSequencePolicyId));
    }

    private async Task AssertNumberingPrerequisiteFailureAsync(
        Func<NpgsqlConnection, Task> breakFixtureAsync,
        string expectedCode)
    {
        if (!TryGetSmokeConnectionString(out var connectionString))
        {
            return;
        }

        await RebuildDisposableDatabaseAsync(connectionString);
        await InsertDisposableSmokeFixtureAsync(connectionString);

        await using (var setupConnection = new NpgsqlConnection(connectionString))
        {
            await setupConnection.OpenAsync();
            await breakFixtureAsync(setupConnection);
        }

        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", CreateValidRequest(expectedCode));
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body.Succeeded);
        Assert.Equal(expectedCode, body.Code);
        Assert.Equal("not_assigned", body.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_configuration_correction", body.ErrorPosture);
        Assert.Null(body.FiscalDocumentId);

        await using var verifyConnection = new NpgsqlConnection(connectionString);
        await verifyConnection.OpenAsync();
        Assert.Equal(0, await CountAsync(
            verifyConnection,
            "pos.fiscal_documents",
            "payment_finality_ref = @ref",
            "ref",
            $"central-finality-{expectedCode}"));

        if (expectedCode != "fiscal_sequence_policy_not_found")
        {
            Assert.Equal(0, await ScalarLongAsync(
                verifyConnection,
                "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id = @id",
                FiscalSequencePolicyId));
        }
    }

    private bool TryGetSmokeConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            output.WriteLine(
                $"Skipping PostgreSQL API smoke test because {ConnectionStringEnvironmentVariable} is not set.");
            return false;
        }

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must be an Npgsql key-value connection string, not a URL-style PostgreSQL URI.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database ?? string.Empty;
        if (!databaseName.Contains("smoke", StringComparison.OrdinalIgnoreCase) ||
            !databaseName.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
            !databaseName.Contains("local", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("prod", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("shared", StringComparison.OrdinalIgnoreCase) ||
            databaseName.Contains("live", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target a disposable local smoke validation database.");
        }

        return true;
    }

    private void WriteCreateResult(string scenario, HttpStatusCode statusCode, CreateFiscalDocumentResponse? body)
    {
        output.WriteLine(
            $"{scenario}: status={statusCode}; code={body?.Code ?? "<null>"}; classification={body?.ResultClassification ?? "<null>"}; posture={body?.ErrorPosture ?? "<null>"}; message={body?.Message ?? "<null>"}");
    }

    private static async Task RebuildDisposableDatabaseAsync(string connectionString)
    {
        var repoRoot = FindRepositoryRoot();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await ExecuteSqlAsync(connection, "drop schema if exists pos cascade;");

        var manifestPath = Path.Combine(repoRoot, "db", "rebuild", "pos_sql_apply_order.txt");
        foreach (var manifestEntry in File.ReadAllLines(manifestPath)
                     .Select(line => line.Trim())
                     .Where(line => line.Length > 0 && !line.StartsWith('#')))
        {
            await ExecuteFileAsync(connection, Path.Combine(repoRoot, manifestEntry.Replace('/', Path.DirectorySeparatorChar)));
        }

        var generatedSqlDirectory = Path.Combine(repoRoot, "db", "reference-data", "controlled-codes", "generated", "sql");
        foreach (var generatedSqlFile in Directory.GetFiles(generatedSqlDirectory, "*.sql").OrderBy(path => path, StringComparer.Ordinal))
        {
            await ExecuteFileAsync(connection, generatedSqlFile);
        }
    }

    private static async Task InsertDisposableSmokeFixtureAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        foreach (var code in SmokeCodes())
        {
            await ExecuteSqlAsync(
                connection,
                """
                insert into pos.controlled_code_sets (
                    controlled_code_set_id,
                    code_set_key,
                    display_name,
                    description,
                    governance_owner,
                    source_ref,
                    is_active
                ) values (
                    @set_id,
                    @set_key,
                    @set_display_name,
                    @set_description,
                    'Engineering',
                    'runtime/fiscal-document-api-postgres-smoke disposable fixture',
                    true
                ) on conflict (code_set_key) do update set
                    display_name = excluded.display_name,
                    description = excluded.description,
                    updated_at = current_timestamp;

                insert into pos.controlled_codes (
                    controlled_code_id,
                    controlled_code_set_id,
                    code_key,
                    display_name,
                    description,
                    source_ref,
                    sort_order,
                    is_active
                ) values (
                    @code_id,
                    @set_id,
                    @code_key,
                    @code_display_name,
                    @code_description,
                    'runtime/fiscal-document-api-postgres-smoke disposable fixture',
                    1,
                    true
                ) on conflict (controlled_code_set_id, code_key) do update set
                    display_name = excluded.display_name,
                    description = excluded.description,
                    updated_at = current_timestamp;
                """,
                command =>
                {
                    command.Parameters.AddWithValue("set_id", code.SetId);
                    command.Parameters.AddWithValue("set_key", code.SetKey);
                    command.Parameters.AddWithValue("set_display_name", code.SetDisplayName);
                    command.Parameters.AddWithValue("set_description", code.SetDescription);
                    command.Parameters.AddWithValue("code_id", code.CodeId);
                    command.Parameters.AddWithValue("code_key", code.CodeKey);
                    command.Parameters.AddWithValue("code_display_name", code.CodeDisplayName);
                    command.Parameters.AddWithValue("code_description", code.CodeDescription);
                });
        }

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.site_pos_servers (
                site_pos_server_id,
                site_pos_server_code,
                display_name,
                central_pms_site_ref,
                central_pms_site_resolution_ref,
                reporting_timezone_name,
                business_day_cutoff_local_time,
                is_active
            ) values (
                @site_pos_server_id,
                'site-pos-server-smoke',
                'Site POS Server Smoke Fixture',
                'central-pms-site-smoke',
                'central-pms-site-resolution-smoke',
                'Etc/UTC',
                '00:00:00',
                true
            ) on conflict (site_pos_server_id) do update set
                display_name = excluded.display_name,
                updated_at = current_timestamp;
            """,
            command => command.Parameters.AddWithValue("site_pos_server_id", SitePosServerId));

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.fiscal_identities (
                fiscal_identity_id,
                fiscal_identity_code,
                taxpayer_display_name,
                registered_business_display_name,
                metadata_json,
                is_active
            ) values (
                @fiscal_identity_id,
                'api-smoke-fiscal-identity',
                'API Smoke Taxpayer',
                'API Smoke Registered Business',
                '{}'::jsonb,
                true
            ) on conflict (fiscal_identity_id) do update set
                taxpayer_display_name = excluded.taxpayer_display_name,
                updated_at = current_timestamp;
            """,
            command => command.Parameters.AddWithValue("fiscal_identity_id", FiscalIdentityId));

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.site_pos_server_fiscal_identity_history (
                site_pos_server_fiscal_identity_history_id,
                site_pos_server_id,
                fiscal_identity_id,
                effective_start_at
            ) values (
                @history_id,
                @site_pos_server_id,
                @fiscal_identity_id,
                '2026-01-01T00:00:00Z'
            ) on conflict (site_pos_server_id, fiscal_identity_id, effective_start_at) do update set
                updated_at = current_timestamp;
            """,
            command =>
            {
                command.Parameters.AddWithValue("history_id", Guid.Parse("10000000-0000-0000-0000-000000000702"));
                command.Parameters.AddWithValue("site_pos_server_id", SitePosServerId);
                command.Parameters.AddWithValue("fiscal_identity_id", FiscalIdentityId);
            });

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.fiscal_reporting_periods (
                fiscal_reporting_period_id,
                fiscal_reporting_contract_version_id,
                site_pos_server_id,
                fiscal_identity_id,
                period_status_code_id,
                business_day_date,
                period_start_at,
                period_end_at,
                reporting_timezone_name,
                business_day_cutoff_local_time,
                currency_code,
                period_sequence,
                opened_at,
                created_by_ref,
                updated_by_ref
            ) values (
                @period_id,
                'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',
                @site_pos_server_id,
                @fiscal_identity_id,
                '1a6f7021-bc84-5c01-afaa-c5d6685633c8',
                current_date,
                transaction_timestamp() - interval '1 hour',
                transaction_timestamp() + interval '1 hour',
                'Etc/UTC',
                '00:00:00',
                'PHP',
                1,
                transaction_timestamp() - interval '1 hour',
                'Z007B-SMOKE',
                'Z007B-SMOKE'
            );
            """,
            command =>
            {
                command.Parameters.AddWithValue("period_id", FiscalReportingPeriodId);
                command.Parameters.AddWithValue("site_pos_server_id", SitePosServerId);
                command.Parameters.AddWithValue("fiscal_identity_id", FiscalIdentityId);
            });

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.fiscal_sequence_policies (
                fiscal_sequence_policy_id,
                site_pos_server_id,
                sequence_family_code_id,
                document_type_code_id,
                policy_code,
                display_name,
                prefix_text,
                suffix_text,
                padding_length,
                current_policy_status_code_id,
                effective_start_at,
                policy_context
            ) values (
                @fiscal_sequence_policy_id,
                @site_pos_server_id,
                @sequence_family_code_id,
                @document_type_code_id,
                'api-smoke-sequence-policy',
                'API Smoke Sequence Policy',
                'SI-',
                '-A',
                8,
                @policy_status_code_id,
                '2026-01-01T00:00:00Z',
                '{}'::jsonb
            ) on conflict (fiscal_sequence_policy_id) do update set
                display_name = excluded.display_name,
                updated_at = current_timestamp;
            """,
            command =>
            {
                command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId);
                command.Parameters.AddWithValue("site_pos_server_id", SitePosServerId);
                command.Parameters.AddWithValue("sequence_family_code_id", FiscalSequenceFamilyCodeId);
                command.Parameters.AddWithValue("document_type_code_id", FiscalDocumentTypeCodeId);
                command.Parameters.AddWithValue("policy_status_code_id", FiscalSequencePolicyStatusCodeId);
            });

        await ExecuteSqlAsync(
            connection,
            """
            insert into pos.fiscal_sequence_states (
                fiscal_sequence_state_id,
                fiscal_sequence_policy_id,
                current_sequence_value,
                sequence_state_code_id,
                state_context
            ) values (
                @fiscal_sequence_state_id,
                @fiscal_sequence_policy_id,
                0,
                @sequence_state_code_id,
                '{}'::jsonb
            ) on conflict (fiscal_sequence_policy_id) do update set
                current_sequence_value = 0,
                last_reserved_sequence_value = null,
                last_issued_sequence_value = null,
                last_transition_at = null,
                updated_at = current_timestamp;
            """,
            command =>
            {
                command.Parameters.AddWithValue("fiscal_sequence_state_id", FiscalSequenceStateId);
                command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId);
                command.Parameters.AddWithValue("sequence_state_code_id", FiscalSequencePolicyStatusCodeId);
            });
    }

    private static async Task ConfigureHistoricalAndCurrentPeriodsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteSqlAsync(connection,
            """
            update pos.fiscal_reporting_periods set period_sequence=2 where fiscal_reporting_period_id=@current;
            insert into pos.fiscal_reporting_periods(
                fiscal_reporting_period_id,fiscal_reporting_contract_version_id,site_pos_server_id,fiscal_identity_id,
                period_status_code_id,business_day_date,period_start_at,period_end_at,reporting_timezone_name,
                business_day_cutoff_local_time,currency_code,period_sequence,opened_at,created_by_ref,updated_by_ref)
            values(@historical,'f6766f48-62f0-513f-b9eb-e61c2f3e8c66',@site,@identity,
                '1a6f7021-bc84-5c01-afaa-c5d6685633c8',current_date-2,
                transaction_timestamp()-interval '3 days',transaction_timestamp()-interval '2 days',
                'Etc/UTC','00:00:00','PHP',1,transaction_timestamp()-interval '3 days','LATE-ISSUANCE-TEST','LATE-ISSUANCE-TEST');
            update pos.fiscal_reporting_periods set expected_prior_period_id=@historical where fiscal_reporting_period_id=@current;
            """,
            command =>
            {
                command.Parameters.AddWithValue("current", FiscalReportingPeriodId);
                command.Parameters.AddWithValue("historical", HistoricalFiscalReportingPeriodId);
                command.Parameters.AddWithValue("site", SitePosServerId);
                command.Parameters.AddWithValue("identity", FiscalIdentityId);
            });
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PosServer"] = connectionString,
            ["PosServer:Admin:ApiKeys:smoke:Principal"] = "fiscal-document-smoke",
            ["PosServer:Admin:ApiKeys:smoke:Key"] = SmokeApiKey,
            ["PosServer:Admin:ApiKeys:smoke:Permissions:0"] = FiscalDocumentAuthorization.CreatePermission,
            ["PosServer:Admin:ApiKeys:smoke:Permissions:1"] = FiscalDocumentAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:smoke:Permissions:2"] = FiscalDocumentAuthorization.VoidPermission
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
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;
        var address = addresses?.SingleOrDefault() ??
            throw new InvalidOperationException("Could not resolve smoke API server address.");

        var client = new HttpClient { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, SmokeApiKey);
        return client;
    }

    private static CreateFiscalDocumentRequest CreateValidRequest(string suffix) =>
        new(
            "site-pos-server-smoke",
            "sales_invoice_smoke",
            new FiscalizationPayableBasisRequest(
                $"payable-basis-{suffix}",
                $"central-finality-{suffix}",
                "PHP",
                12500,
                [new FiscalDiscountReferenceRequest($"discount-validation-{suffix}", "approved", true)]),
            SitePosServerId: SitePosServerId,
            FiscalDocumentTypeCodeId: FiscalDocumentTypeCodeId,
            FiscalDocumentStatusCodeId: FiscalDocumentStatusCodeId,
            BusinessDayDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CentralPmsParkingSessionRef: $"parking-session-{suffix}",
            CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
            CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
            PaymentFinalityRef: $"central-finality-{suffix}",
            VendorAckRef: $"vendor-ack-{suffix}",
            DocumentLines:
            [
                new CreateFiscalDocumentLineRequest(
                    1,
                    FiscalLineTypeCodeId,
                    "Parking fee",
                    1,
                    12500,
                    12500,
                    1000,
                    0,
                    11500,
                    "PHP",
                    SourceRef: $"line-source-{suffix}",
                    LineContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Tenders:
            [
                new CreateFiscalTenderRequest(
                    FiscalTenderTypeCodeId,
                    12500,
                    "PHP",
                    CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
                    CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
                    PaymentFinalityRef: $"central-finality-{suffix}",
                    ProviderRef: $"provider-ref-{suffix}",
                    TenderContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            TaxDetails:
            [
                new CreateFiscalTaxDetailRequest(
                    FiscalTaxTypeCodeId,
                    FiscalTaxClassificationCodeId,
                    12500,
                    0,
                    "PHP",
                    LineSequence: 1,
                    TaxRate: 0,
                    TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            DiscountPrivilegeDetails:
            [
                new CreateFiscalDiscountPrivilegeDetailRequest(
                    FiscalDiscountPrivilegeTypeCodeId,
                    12500,
                    1000,
                    0,
                    "PHP",
                    LineSequence: 1,
                    BeneficiaryRef: $"beneficiary-ref-{suffix}",
                    EvidenceRef: $"evidence-ref-{suffix}",
                    ApprovalRef: $"discount-validation-{suffix}",
                    DiscountPrivilegeContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Totals:
            [
                new CreateFiscalTotalRequest(
                    FiscalTotalTypeCodeId,
                    12500,
                    "PHP",
                    new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ]);

    private static CreateFiscalDocumentRequest CreateValidStatutoryRequest(string suffix)
    {
        var parkingSessionId = Guid.Parse("21000000-0000-4000-8000-000000000005");
        var siteId = Guid.Parse("21000000-0000-4000-8000-000000000006");
        var facts = new AppliedStatutoryFiscalFactsRequest(
            Guid.Parse("21000000-0000-4000-8000-000000000001"),
            Guid.Parse("21000000-0000-4000-8000-000000000002"),
            Guid.Parse("21000000-0000-4000-8000-000000000003"),
            Guid.Parse("21000000-0000-4000-8000-000000000004"),
            parkingSessionId,
            siteId,
            Guid.Parse("21000000-0000-4000-8000-000000000007"),
            "SENIOR_CITIZEN",
            "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT",
            new AppliedStatutoryPolicyReferenceRequest(
                "NATIONAL_LAW",
                AppliedPolicyReferenceId: Guid.Parse("21000000-0000-4000-8000-000000000008"),
                PolicyCode: "TEST-SENIOR-CITIZEN-POLICY",
                NationalLawReference: "TEST-NATIONAL-LAW-REFERENCE"),
            Guid.Parse("21000000-0000-4000-8000-000000000009"),
            Guid.Parse("21000000-0000-4000-8000-000000000010"),
            10000,
            8929,
            0,
            "VAT_EXEMPT",
            1786,
            7143,
            "PHP",
            DateTimeOffset.Parse("2026-07-29T08:15:00+08:00"),
            "WEBPAY");

        return new CreateFiscalDocumentRequest(
            "site-pos-server-smoke",
            "sales_invoice_smoke",
            new FiscalizationPayableBasisRequest(
                $"payable-basis-{suffix}",
                $"central-finality-{suffix}",
                "PHP",
                7143,
                [new FiscalDiscountReferenceRequest(facts.StatutoryValidationId.ToString(), "approved", true)]),
            SitePosServerId: SitePosServerId,
            SiteId: siteId,
            FiscalDocumentTypeCodeId: FiscalDocumentTypeCodeId,
            FiscalDocumentStatusCodeId: FiscalDocumentStatusCodeId,
            BusinessDayDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CentralPmsParkingSessionRef: parkingSessionId.ToString("D"),
            CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
            CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
            PaymentFinalityRef: $"central-finality-{suffix}",
            VendorAckRef: $"vendor-ack-{suffix}",
            DocumentLines:
            [
                new CreateFiscalDocumentLineRequest(
                    1,
                    FiscalLineTypeCodeId,
                    "Parking fee",
                    1,
                    10000,
                    10000,
                    2857,
                    0,
                    7143,
                    "PHP",
                    SourceRef: $"line-source-{suffix}",
                    LineContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Tenders:
            [
                new CreateFiscalTenderRequest(
                    FiscalTenderTypeCodeId,
                    7143,
                    "PHP",
                    CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
                    CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
                    PaymentFinalityRef: $"central-finality-{suffix}",
                    ProviderRef: $"provider-ref-{suffix}",
                    TenderContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            TaxDetails:
            [
                new CreateFiscalTaxDetailRequest(
                    FiscalTaxTypeCodeId,
                    FiscalVatExemptTaxClassificationCodeId,
                    8929,
                    0,
                    "PHP",
                    LineSequence: 1,
                    TaxRate: 0,
                    TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            DiscountPrivilegeDetails:
            [
                new CreateFiscalDiscountPrivilegeDetailRequest(
                    FiscalDiscountPrivilegeTypeCodeId,
                    10000,
                    1786,
                    1071,
                    "PHP",
                    LineSequence: 1,
                    ApprovalRef: facts.StatutoryValidationId.ToString(),
                    DiscountPrivilegeContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Totals:
            [
                new CreateFiscalTotalRequest(
                    FiscalTotalTypeCodeId,
                    7143,
                    "PHP",
                    new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            AppliedStatutoryFiscalFacts: facts,
            InvoiceCustomerInformation: new InvoiceCustomerInformationRequest(
                "Maria Santos",
                "100 Sample Street",
                "123-456-789",
                "Sample Trading",
                "OSCA-12345"));
    }

    private static CreateFiscalDocumentRequest CreateValidZeroPayableStatutoryRequest(string suffix)
    {
        var paid = CreateValidStatutoryRequest(suffix);
        var facts = paid.AppliedStatutoryFiscalFacts! with
        {
            BenefitClassification = "FREE_PARKING",
            OriginalAmountMinorUnits = 3000,
            VatExclusiveBasisAmountMinorUnits = 2679,
            VatAmountMinorUnits = 321,
            VatTreatment = "VAT_EXCLUSIVE",
            StatutoryDiscountAmountMinorUnits = 2679,
            FinalPayableAmountMinorUnits = 0
        };
        var authorityRef = facts.StatutoryPayableBasisApplicationCommandId!.Value.ToString("D");
        var upstreamRef = $"ZERO_PAYABLE_STATUTORY_FINALITY:{authorityRef}";

        return paid with
        {
            CentralPmsPaymentAttemptRef = null,
            CentralPmsPaymentConfirmationRef = null,
            PaymentFinalityRef = null,
            CompletionBasis = "ZERO_PAYABLE_STATUTORY_FINALITY",
            CompletionAuthorityRef = authorityRef,
            PayableBasis = paid.PayableBasis! with
            {
                PayableBasisRef = facts.AppliedTariffSnapshotId!.Value.ToString("D"),
                UpstreamFinalityRef = upstreamRef,
                PayableAmountMinorUnits = 0
            },
            DocumentLines =
            [
                paid.DocumentLines![0] with
                {
                    UnitAmountMinorUnits = 2679,
                    GrossAmountMinorUnits = 2679,
                    DiscountAmountMinorUnits = 2679,
                    TaxAmountMinorUnits = 0,
                    NetAmountMinorUnits = 0
                }
            ],
            Tenders = [],
            TaxDetails =
            [
                paid.TaxDetails![0] with
                {
                    TaxClassificationCodeId = FiscalTaxClassificationCodeId,
                    TaxableAmountMinorUnits = 2679,
                    TaxAmountMinorUnits = 321,
                    TaxRate = 12
                }
            ],
            DiscountPrivilegeDetails =
            [
                paid.DiscountPrivilegeDetails![0] with
                {
                    BasisAmountMinorUnits = 2679,
                    DiscountAmountMinorUnits = 2679,
                    VatPrivilegeAmountMinorUnits = 321
                }
            ],
            Totals = [paid.Totals![0] with { AmountMinorUnits = 0 }],
            AppliedStatutoryFiscalFacts = facts
        };
    }

    private static CreateFiscalDocumentRequest CreateValidPwdStatutoryRequest(string suffix)
    {
        var parkingSessionId = Guid.Parse("22000000-0000-4000-8000-000000000005");
        var siteId = Guid.Parse("22000000-0000-4000-8000-000000000006");
        var terminalCashTenderId = Guid.Parse("22000000-0000-4000-8000-000000000011");
        var facts = new AppliedStatutoryFiscalFactsRequest(
            Guid.Parse("22000000-0000-4000-8000-000000000001"),
            Guid.Parse("22000000-0000-4000-8000-000000000002"),
            Guid.Parse("22000000-0000-4000-8000-000000000003"),
            Guid.Parse("22000000-0000-4000-8000-000000000004"),
            parkingSessionId,
            siteId,
            Guid.Parse("22000000-0000-4000-8000-000000000007"),
            "PWD",
            "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT",
            new AppliedStatutoryPolicyReferenceRequest(
                "NATIONAL_LAW",
                AppliedPolicyReferenceId: Guid.Parse("22000000-0000-4000-8000-000000000008"),
                PolicyCode: "TEST-PWD-POLICY",
                NationalLawReference: "TEST-NATIONAL-LAW-REFERENCE"),
            Guid.Parse("22000000-0000-4000-8000-000000000009"),
            Guid.Parse("22000000-0000-4000-8000-000000000010"),
            12000,
            10714,
            0,
            "VAT_EXEMPT",
            2143,
            8571,
            "PHP",
            DateTimeOffset.Parse("2026-07-29T09:25:00+08:00"),
            "ASSISTED_PAYMENT_TERMINAL",
            terminalCashTenderId);

        return new CreateFiscalDocumentRequest(
            "site-pos-server-smoke",
            "sales_invoice_smoke",
            new FiscalizationPayableBasisRequest(
                $"payable-basis-{suffix}",
                $"central-finality-{suffix}",
                "PHP",
                8571,
                [new FiscalDiscountReferenceRequest(facts.StatutoryValidationId.ToString(), "approved", true)]),
            SitePosServerId: SitePosServerId,
            SiteId: siteId,
            FiscalDocumentTypeCodeId: FiscalDocumentTypeCodeId,
            FiscalDocumentStatusCodeId: FiscalDocumentStatusCodeId,
            BusinessDayDate: DateOnly.FromDateTime(DateTime.UtcNow),
            CentralPmsParkingSessionRef: parkingSessionId.ToString("D"),
            CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
            CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
            PaymentFinalityRef: $"central-finality-{suffix}",
            VendorAckRef: $"vendor-ack-{suffix}",
            DocumentLines:
            [
                new CreateFiscalDocumentLineRequest(
                    1,
                    FiscalLineTypeCodeId,
                    "Parking fee",
                    1,
                    12000,
                    12000,
                    3429,
                    0,
                    8571,
                    "PHP",
                    SourceRef: $"line-source-{suffix}",
                    LineContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Tenders:
            [
                new CreateFiscalTenderRequest(
                    FiscalTenderTypeCodeId,
                    8571,
                    "PHP",
                    CentralPmsPaymentAttemptRef: $"payment-attempt-{suffix}",
                    CentralPmsPaymentConfirmationRef: $"payment-confirmation-{suffix}",
                    PaymentFinalityRef: $"central-finality-{suffix}",
                    ProviderRef: $"provider-ref-{suffix}",
                    TenderContext: new Dictionary<string, string>
                    {
                        ["source_system"] = "central_pms",
                        ["terminal_cash_tender_ref"] = terminalCashTenderId.ToString("D")
                    })
            ],
            TaxDetails:
            [
                new CreateFiscalTaxDetailRequest(
                    FiscalTaxTypeCodeId,
                    FiscalVatExemptTaxClassificationCodeId,
                    10714,
                    0,
                    "PHP",
                    LineSequence: 1,
                    TaxRate: 0,
                    TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            DiscountPrivilegeDetails:
            [
                new CreateFiscalDiscountPrivilegeDetailRequest(
                    FiscalDiscountPrivilegeTypeCodeId,
                    12000,
                    2143,
                    1286,
                    "PHP",
                    LineSequence: 1,
                    ApprovalRef: facts.StatutoryValidationId.ToString(),
                    DiscountPrivilegeContext: new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            Totals:
            [
                new CreateFiscalTotalRequest(
                    FiscalTotalTypeCodeId,
                    8571,
                    "PHP",
                    new Dictionary<string, string> { ["source_system"] = "central_pms" })
            ],
            AppliedStatutoryFiscalFacts: facts);
    }

    private static IReadOnlyList<SmokeCode> SmokeCodes() =>
    [
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000101"),
            "api_smoke_fiscal_document_type",
            "API Smoke Fiscal Document Type",
            "Disposable smoke-test fiscal document type code set.",
            FiscalDocumentTypeCodeId,
            "sales_invoice_smoke",
            "Sales Invoice Smoke",
            "Disposable smoke-test sales invoice posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000102"),
            "api_smoke_fiscal_document_status",
            "API Smoke Fiscal Document Status",
            "Disposable smoke-test fiscal document status code set.",
            FiscalDocumentStatusCodeId,
            "issued",
            "Issued Smoke",
            "Disposable smoke-test issued status posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000102"),
            "api_smoke_fiscal_document_status",
            "API Smoke Fiscal Document Status",
            "Disposable smoke-test fiscal document status code set.",
            FiscalDocumentVoidedStatusCodeId,
            "voided",
            "Voided Smoke",
            "Disposable smoke-test voided status posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000201"),
            "api_smoke_fiscal_line_type",
            "API Smoke Fiscal Line Type",
            "Disposable smoke-test fiscal line type code set.",
            FiscalLineTypeCodeId,
            "parking_fee_smoke",
            "Parking Fee Smoke",
            "Disposable smoke-test parking fee line posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000301"),
            "api_smoke_fiscal_tender_type",
            "API Smoke Fiscal Tender Type",
            "Disposable smoke-test fiscal tender type code set.",
            FiscalTenderTypeCodeId,
            "cash_smoke",
            "Cash Smoke",
            "Disposable smoke-test tender posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000401"),
            "api_smoke_fiscal_tax_type",
            "API Smoke Fiscal Tax Type",
            "Disposable smoke-test fiscal tax type code set.",
            FiscalTaxTypeCodeId,
            "tax_smoke",
            "Tax Smoke",
            "Disposable smoke-test tax type posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000402"),
            "api_smoke_fiscal_tax_classification",
            "API Smoke Fiscal Tax Classification",
            "Disposable smoke-test fiscal tax classification code set.",
            FiscalTaxClassificationCodeId,
            "vatable",
            "VATable",
            "Authoritative VATable tax classification for the disposable smoke fixture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000402"),
            "api_smoke_fiscal_tax_classification",
            "API Smoke Fiscal Tax Classification",
            "Disposable smoke-test fiscal tax classification code set.",
            FiscalVatExemptTaxClassificationCodeId,
            "vat_exempt",
            "VAT Exempt",
            "Authoritative VAT-exempt tax classification for the disposable smoke fixture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000501"),
            "api_smoke_discount_privilege_type",
            "API Smoke Discount Privilege Type",
            "Disposable smoke-test discount/privilege type code set.",
            FiscalDiscountPrivilegeTypeCodeId,
            "discount_privilege_smoke",
            "Discount Privilege Smoke",
            "Disposable smoke-test discount/privilege posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000601"),
            "api_smoke_fiscal_total_type",
            "API Smoke Fiscal Total Type",
            "Disposable smoke-test fiscal total type code set.",
            FiscalTotalTypeCodeId,
            "payable_total_smoke",
            "Payable Total Smoke",
            "Disposable smoke-test payable total posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000801"),
            "api_smoke_sequence_family",
            "API Smoke Sequence Family",
            "Disposable smoke-test sequence family code set.",
            FiscalSequenceFamilyCodeId,
            "sales_invoice_sequence_smoke",
            "Sales Invoice Sequence Smoke",
            "Disposable smoke-test sequence family posture."),
        new(
            Guid.Parse("20000000-0000-0000-0000-000000000802"),
            "api_smoke_sequence_policy_status",
            "API Smoke Sequence Policy Status",
            "Disposable smoke-test sequence policy status code set.",
            FiscalSequencePolicyStatusCodeId,
            "active_smoke",
            "Active Smoke",
            "Disposable smoke-test sequence policy active posture.")
    ];

    private static IEnumerable<string> ProhibitedRuntimeTables() =>
    [
        "pos.fiscal_report_requests",
        "pos.fiscal_report_scopes",
        "pos.x_z_reports",
        "pos.bir_sales_summary_reports",
        "pos.annex_e_reports",
        "pos.fiscal_report_output_refs",
        "pos.digital_si_urls",
        "pos.digital_si_url_access_events"
    ];

    private static async Task ExecuteFileAsync(NpgsqlConnection connection, string path)
    {
        await ExecuteSqlAsync(connection, await File.ReadAllTextAsync(path));
    }

    private static async Task ExecuteSqlAsync(
        NpgsqlConnection connection,
        string sql,
        Action<NpgsqlCommand>? configure = null)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configure?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> CountAsync(
        NpgsqlConnection connection,
        string tableName,
        string predicate,
        string parameterName,
        object value)
    {
        await using var command = new NpgsqlCommand($"select count(*) from {tableName} where {predicate};", connection);
        command.Parameters.AddWithValue(parameterName, value);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = new NpgsqlCommand($"select count(*) from {tableName};", connection);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<Guid> ScalarGuidAsync(NpgsqlConnection connection, string sql, Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (Guid)(await command.ExecuteScalarAsync() ?? Guid.Empty);
    }

    private static async Task<string?> ScalarStringAsync(NpgsqlConnection connection, string sql, Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task<long> ScalarLongAsync(NpgsqlConnection connection, string sql, Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<long> CountTextMarkerAsync(NpgsqlConnection connection, string marker)
    {
        await using var command = new NpgsqlCommand(
            """
            select
                (select count(*) from pos.fiscal_documents where document_context::text ilike @marker) +
                (select count(*) from pos.fiscal_document_lines where coalesce(line_context::text, '') ilike @marker) +
                (select count(*) from pos.fiscal_tenders where coalesce(tender_context::text, '') ilike @marker) +
                (select count(*) from pos.fiscal_tax_details where coalesce(tax_context::text, '') ilike @marker) +
                (select count(*) from pos.fiscal_discount_privilege_details
                    where coalesce(discount_privilege_context::text, '') ilike @marker
                       or coalesce(evidence_ref, '') ilike @marker) +
                (select count(*) from pos.fiscal_totals where coalesce(total_context::text, '') ilike @marker);
            """,
            connection);
        command.Parameters.AddWithValue("marker", $"%{marker}%");
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<long> CountIfTableExistsAsync(NpgsqlConnection connection, string qualifiedTableName)
    {
        await using var command = new NpgsqlCommand(
            """
            select case
                when to_regclass(@table_name) is null then 0
                else (select count(*) from pg_catalog.pg_class c)
            end;
            """,
            connection);
        command.Parameters.AddWithValue("table_name", qualifiedTableName);

        if ((long)(await command.ExecuteScalarAsync() ?? 0L) == 0)
        {
            return 0;
        }

        await using var countCommand = new NpgsqlCommand($"select count(*) from {qualifiedTableName};", connection);
        return (long)(await countCommand.ExecuteScalarAsync() ?? 0L);
    }

    private static async Task<long> CountTablesLikeAsync(NpgsqlConnection connection, string pattern)
    {
        await using var command = new NpgsqlCommand(
            """
            select count(*)
            from information_schema.tables
            where table_schema = 'pos'
              and table_name ilike @pattern;
            """,
            connection);
        command.Parameters.AddWithValue("pattern", pattern);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var manifestPath = Path.Combine(current.FullName, "db", "rebuild", "pos_sql_apply_order.txt");
            if (File.Exists(manifestPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }

    private sealed record SmokeCode(
        Guid SetId,
        string SetKey,
        string SetDisplayName,
        string SetDescription,
        Guid CodeId,
        string CodeKey,
        string CodeDisplayName,
        string CodeDescription);
}
