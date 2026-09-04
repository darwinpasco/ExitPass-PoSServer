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

namespace ExitPass.PosServer.Api.IntegrationTests;

public sealed class PersistentIstPitxFiscalIssuanceProofTests
{
    private const string ConnectionStringEnvironmentVariable = "POSSERVER_PERSISTENT_IST_PROOF_DB_URL";
    private static readonly Guid SiteId = Guid.Parse("2d1dcdf8-f563-537c-8542-0bde7cc9da97");
    private static readonly Guid SitePosServerId = Guid.Parse("3a138565-1b88-55f8-c83d-5380db6edccc");
    private static readonly Guid FiscalIdentityId = Guid.Parse("ad02beb5-b8cd-4545-9ff2-5586782c686a");
    private static readonly Guid ChannelTerminalId = Guid.Parse("b71f74f7-8e56-5fda-b257-5d9823ab8520");
    private static readonly Guid DocumentTypeId = Guid.Parse("b9fc7458-57a7-5bbe-809d-8c8b3d780b1d");
    private static readonly Guid DocumentStatusId = Guid.Parse("9f868073-a648-5b1d-99c0-f96d598c4eb0");
    private static readonly Guid LineTypeId = Guid.Parse("09997efc-e7b4-5cfb-8904-3020e01de4a9");
    private static readonly Guid TenderTypeId = Guid.Parse("a4bd3153-076e-564f-af56-3532be501e95");
    private static readonly Guid TaxTypeId = Guid.Parse("328dcb64-584a-5f59-a304-2e5189a2aa83");
    private static readonly Guid TaxClassificationId = Guid.Parse("ab180f41-e181-5579-b9f1-5ae7a840a946");
    private static readonly Guid TotalTypeId = Guid.Parse("6abfc6aa-b94e-5a24-b533-8d18a33c468b");
    private static readonly Guid SequencePolicyId = Guid.Parse("2e4252dd-38f0-5424-bee2-0c864b41f6b2");
    private const string PaymentFinalityRef = "r41-readiness-proof-finality";
    private const string ProofApiKey = "disposable-r41-pos-proof-key";

    [Fact]
    public async Task PitxConfigurationIssuesOnceAndReplayDoesNotAdvanceSequence()
    {
        if (!TryGetDisposableProofConnectionString(out var connectionString))
        {
            return;
        }

        var existingDocumentId = await ReadExistingFiscalDocumentIdAsync(connectionString);
        if (existingDocumentId is null)
        {
            await AssertFreshSequenceAsync(connectionString);
        }
        await using var app = await StartApiAsync(connectionString);
        using var client = CreateClient(app);
        var request = CreateRequest();

        using var response = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var body = await response.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.Succeeded);
        Assert.Equal(existingDocumentId is null ? "newly_created" : "idempotent_replay", body.ResultClassification);
        Assert.Equal(FiscalIdentityId, body.FiscalIdentityId);
        Assert.Equal(SequencePolicyId, body.FiscalSequencePolicyId);
        Assert.Equal(1, body.FiscalSequenceValue);
        Assert.Equal("SI-00000001", body.FiscalDocumentNumber);
        Assert.NotNull(body.FiscalDocumentId);
        if (existingDocumentId is not null)
        {
            Assert.Equal(existingDocumentId, body.FiscalDocumentId);
        }
        await AssertIssuedStateAsync(connectionString, body.FiscalDocumentId.Value);

        using var replayResponse = await client.PostAsJsonAsync("/v1/fiscal-documents/", request);
        var replay = await replayResponse.Content.ReadFromJsonAsync<CreateFiscalDocumentResponse>();

        Assert.Equal(HttpStatusCode.Accepted, replayResponse.StatusCode);
        Assert.NotNull(replay);
        Assert.True(replay.Succeeded);
        Assert.Equal("idempotent_replay", replay.ResultClassification);
        Assert.Equal(body.FiscalDocumentId, replay.FiscalDocumentId);
        Assert.Equal(1, replay.FiscalSequenceValue);
        Assert.Equal("SI-00000001", replay.FiscalDocumentNumber);
        await AssertIssuedStateAsync(connectionString, body.FiscalDocumentId.Value);
    }

    private static bool TryGetDisposableProofConnectionString(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var host = builder.Host ?? string.Empty;
        if (!string.Equals(builder.Database, "exitpass_pos_r41_validation", StringComparison.Ordinal) ||
            host.Contains("persistent", StringComparison.OrdinalIgnoreCase) ||
            host.Contains("prod", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} must target only exitpass_pos_r41_validation on a disposable host.");
        }
        return true;
    }

    private static async Task<Guid?> ReadExistingFiscalDocumentIdAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select fiscal_document_id from pos.fiscal_documents where payment_finality_ref=@ref",
            connection);
        command.Parameters.AddWithValue("ref", PaymentFinalityRef);
        return await command.ExecuteScalarAsync() is Guid id ? id : null;
    }

    private static async Task AssertFreshSequenceAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(0, await ScalarAsync(connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id='2e4252dd-38f0-5424-bee2-0c864b41f6b2'"));
    }

    private static async Task AssertIssuedStateAsync(string connectionString, Guid fiscalDocumentId)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection,
            "select count(*) from pos.fiscal_documents where fiscal_document_id=@document and payment_finality_ref=@ref", fiscalDocumentId));
        Assert.Equal(1, await ScalarAsync(connection,
            "select count(*) from pos.electronic_journal_records where fiscal_document_id=@document and is_canonical and idempotency_ref=@ref", fiscalDocumentId));
        Assert.Equal(1, await ScalarAsync(connection,
            "select current_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id='2e4252dd-38f0-5424-bee2-0c864b41f6b2'"));
        Assert.Equal(1, await ScalarAsync(connection,
            "select last_reserved_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id='2e4252dd-38f0-5424-bee2-0c864b41f6b2'"));
        Assert.Equal(1, await ScalarAsync(connection,
            "select last_issued_sequence_value from pos.fiscal_sequence_states where fiscal_sequence_policy_id='2e4252dd-38f0-5424-bee2-0c864b41f6b2'"));
        Assert.Equal(1, await ScalarAsync(connection, """
            select count(*) from pos.fiscal_document_header_snapshots
            where fiscal_document_id=@document
              and sales_invoice_header_profile_id='cb90e882-89f3-4bf3-975c-a6fb10dd765c'
              and fiscal_identity_id='ad02beb5-b8cd-4545-9ff2-5586782c686a'
              and registered_business_name='Professional Parking Management Corporation'
              and machine_identification_number='MIN-TEST-PITX-L3-001'
              and pos_serial_number='SN-TEST-PITX-L3-001'
              and supplier_developer_registered_name='Professional Parking Management Corporation'
              and supplier_developer_tin='999-999-999-000'
              and bir_accreditation_number='ACC-TEST-2026-0001'
              and ptu_number='PTU-TEST-PITX-L3-0001'
            """, fiscalDocumentId));
    }

    private static async Task<long> ScalarAsync(NpgsqlConnection connection, string sql, Guid? fiscalDocumentId = null)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("ref", PaymentFinalityRef);
        if (fiscalDocumentId is not null)
        {
            command.Parameters.AddWithValue("document", fiscalDocumentId.Value);
        }
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
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
            ["PosServer:Admin:ApiKeys:proof:Principal"] = "persistent-ist-proof",
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
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.SingleOrDefault()
            ?? throw new InvalidOperationException("Could not resolve proof API address.");
        var client = new HttpClient { BaseAddress = new Uri(address) };
        client.DefaultRequestHeaders.Add(SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName, ProofApiKey);
        return client;
    }

    private static CreateFiscalDocumentRequest CreateRequest() => new(
        "PITX-L3-POS",
        "sales_invoice",
        new FiscalizationPayableBasisRequest("r41-readiness-proof-payable", PaymentFinalityRef, "PHP", 10000, []),
        SitePosServerId: SitePosServerId,
        SiteId: SiteId,
        ChannelTerminalId: ChannelTerminalId,
        RuntimeTerminalRef: "PITX-L3-WEBPAY-01",
        FiscalDocumentTypeCodeId: DocumentTypeId,
        FiscalDocumentStatusCodeId: DocumentStatusId,
        BusinessDayDate: new DateOnly(2026, 9, 4),
        CentralPmsParkingSessionRef: "r41-readiness-proof-session",
        CentralPmsPaymentAttemptRef: "r41-readiness-proof-attempt",
        CentralPmsPaymentConfirmationRef: "r41-readiness-proof-confirmation",
        PaymentFinalityRef: PaymentFinalityRef,
        DocumentLines:
        [
            new(1, LineTypeId, "Parking fee", 1, 8929, 8929, 0, 1071, 10000, "PHP")
        ],
        Tenders:
        [
            new(TenderTypeId, 10000, "PHP", "r41-readiness-proof-attempt",
                "r41-readiness-proof-confirmation", PaymentFinalityRef, "paymongo-proof")
        ],
        TaxDetails:
        [
            new(TaxTypeId, TaxClassificationId, 8929, 1071, "PHP", 1, 0.12m)
        ],
        DiscountPrivilegeDetails: [],
        Totals:
        [
            new(TotalTypeId, 10000, "PHP")
        ]);
}
