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
using Xunit.Abstractions;

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class FiscalDocumentApiPostgresSmokeTests
{
    private const string ConnectionStringEnvironmentVariable = "POSSERVER_API_SMOKE_DB_URL";
    private static readonly Guid SitePosServerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid FiscalDocumentTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000101");
    private static readonly Guid FiscalDocumentStatusCodeId = Guid.Parse("10000000-0000-0000-0000-000000000102");
    private static readonly Guid FiscalLineTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000201");
    private static readonly Guid FiscalTenderTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000301");
    private static readonly Guid FiscalTaxTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000401");
    private static readonly Guid FiscalTaxClassificationCodeId = Guid.Parse("10000000-0000-0000-0000-000000000402");
    private static readonly Guid FiscalDiscountPrivilegeTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000501");
    private static readonly Guid FiscalTotalTypeCodeId = Guid.Parse("10000000-0000-0000-0000-000000000601");
    private static readonly Guid FiscalIdentityId = Guid.Parse("10000000-0000-0000-0000-000000000701");
    private static readonly Guid FiscalSequenceFamilyCodeId = Guid.Parse("10000000-0000-0000-0000-000000000801");
    private static readonly Guid FiscalSequencePolicyStatusCodeId = Guid.Parse("10000000-0000-0000-0000-000000000802");
    private static readonly Guid FiscalSequencePolicyId = Guid.Parse("10000000-0000-0000-0000-000000000803");

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
        var request = CreateValidRequest("success");

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal("accepted", body.Code);
        Assert.NotNull(body.FiscalDocumentId);

        var fiscalDocumentId = body.FiscalDocumentId.Value;
        using var getResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}");
        var getBody = await getResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getBody);
        Assert.True(getBody.Succeeded);
        Assert.Equal("found", getBody.Code);
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
        Assert.Null(getBody.Document.FiscalSequencePolicyId);
        Assert.Null(getBody.Document.FiscalSequenceValue);
        Assert.Null(getBody.Document.FiscalDocumentNumber);
        Assert.Null(getBody.Document.FiscalSeries);
        Assert.Null(getBody.Document.FiscalNumberPrefixText);
        Assert.Null(getBody.Document.FiscalNumberSuffixText);
        Assert.Null(getBody.Document.FiscalNumberAssignedAt);
        Assert.Null(getBody.Document.FiscalNumberAssignedByRef);

        using var missingGetResponse = await client.GetAsync($"/v1/fiscal-documents/{Guid.Parse("99999999-9999-9999-9999-999999999999")}");
        var missingGetBody = await missingGetResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.NotFound, missingGetResponse.StatusCode);
        Assert.NotNull(missingGetBody);
        Assert.False(missingGetBody.Succeeded);
        Assert.Equal("fiscal_document_not_found", missingGetBody.Code);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_documents", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_status_history", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(0, await CountAsync(connection, "pos.fiscal_document_links", "source_fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_document_lines", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_tenders", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_tax_details", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_discount_privilege_details", "fiscal_document_id = @id", "id", fiscalDocumentId));
        Assert.Equal(1, await CountAsync(connection, "pos.fiscal_totals", "fiscal_document_id = @id", "id", fiscalDocumentId));

        Assert.Equal("central-finality-success", await ScalarStringAsync(
            connection,
            "select payment_finality_ref from pos.fiscal_documents where fiscal_document_id = @id",
            fiscalDocumentId));
        Assert.Equal("evidence-ref-success", await ScalarStringAsync(
            connection,
            "select evidence_ref from pos.fiscal_discount_privilege_details where fiscal_document_id = @id",
            fiscalDocumentId));

        var assignedAt = DateTimeOffset.Parse("2026-07-01T08:15:00Z");
        await UpdateDisposableFiscalNumberingAsync(connection, fiscalDocumentId, assignedAt);

        using var numberedGetResponse = await client.GetAsync($"/v1/fiscal-documents/{fiscalDocumentId}");
        var numberedGetBody = await numberedGetResponse.Content.ReadFromJsonAsync<GetFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.OK, numberedGetResponse.StatusCode);
        Assert.NotNull(numberedGetBody);
        Assert.NotNull(numberedGetBody.Document);
        Assert.Equal(FiscalIdentityId, numberedGetBody.Document.FiscalIdentityId);
        Assert.Equal(FiscalSequencePolicyId, numberedGetBody.Document.FiscalSequencePolicyId);
        Assert.Equal(42, numberedGetBody.Document.FiscalSequenceValue);
        Assert.Equal("SI-00000042", numberedGetBody.Document.FiscalDocumentNumber);
        Assert.Equal("SI", numberedGetBody.Document.FiscalSeries);
        Assert.Equal("SI-", numberedGetBody.Document.FiscalNumberPrefixText);
        Assert.Equal("-A", numberedGetBody.Document.FiscalNumberSuffixText);
        Assert.Equal(assignedAt, numberedGetBody.Document.FiscalNumberAssignedAt);
        Assert.Equal("pos-server-smoke-fixture", numberedGetBody.Document.FiscalNumberAssignedByRef);

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
                is_active
            ) values (
                @site_pos_server_id,
                'site-pos-server-smoke',
                'Site POS Server Smoke Fixture',
                'central-pms-site-smoke',
                'central-pms-site-resolution-smoke',
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
    }

    private static async Task<WebApplication> StartApiAsync(string connectionString)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PosServer"] = connectionString
        });
        builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);

        var app = builder.Build();
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

        return new HttpClient { BaseAddress = new Uri(address) };
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
            BusinessDayDate: new DateOnly(2026, 7, 1),
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
            "created_smoke",
            "Created Smoke",
            "Disposable smoke-test created status posture."),
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
            "tax_classification_smoke",
            "Tax Classification Smoke",
            "Disposable smoke-test tax classification posture."),
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

    private static async Task UpdateDisposableFiscalNumberingAsync(
        NpgsqlConnection connection,
        Guid fiscalDocumentId,
        DateTimeOffset assignedAt)
    {
        await ExecuteSqlAsync(
            connection,
            """
            update pos.fiscal_documents
            set fiscal_identity_id = @fiscal_identity_id,
                fiscal_sequence_policy_id = @fiscal_sequence_policy_id,
                fiscal_sequence_value = 42,
                fiscal_document_number = 'SI-00000042',
                fiscal_series = 'SI',
                fiscal_number_prefix_text = 'SI-',
                fiscal_number_suffix_text = '-A',
                fiscal_number_assigned_at = @assigned_at,
                fiscal_number_assigned_by_ref = 'pos-server-smoke-fixture',
                updated_at = current_timestamp
            where fiscal_document_id = @fiscal_document_id;
            """,
            command =>
            {
                command.Parameters.AddWithValue("fiscal_document_id", fiscalDocumentId);
                command.Parameters.AddWithValue("fiscal_identity_id", FiscalIdentityId);
                command.Parameters.AddWithValue("fiscal_sequence_policy_id", FiscalSequencePolicyId);
                command.Parameters.AddWithValue("assigned_at", assignedAt);
            });
    }

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

    private static async Task<string?> ScalarStringAsync(NpgsqlConnection connection, string sql, Guid id)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        return (string?)await command.ExecuteScalarAsync();
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
