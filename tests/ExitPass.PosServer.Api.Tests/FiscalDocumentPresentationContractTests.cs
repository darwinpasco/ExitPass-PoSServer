using System.Text.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentPresentationContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("recorded-assigned-cash")]
    [InlineData("recorded-not-assigned-cash")]
    [InlineData("voided-assigned-cash")]
    public async Task FiscalDocumentPresentationEndpointMatchesGovernedFixture(string caseId)
    {
        using var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, caseId);
        var document = BuildDocument(fixtureCase.GetProperty("storedFiscalDocument"));
        var response = await InvokeAsync(document);

        Assert.True(response.Succeeded);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        AssertTopLevelFields(response, fixtureCase.GetProperty("expectedResponse"));
        AssertPresentationRows(response, fixtureCase.GetProperty("expectedPresentationRows"));
    }

    [Fact]
    public async Task FiscalDocumentPresentationEndpointRepeatedReadsAreSideEffectFree()
    {
        using var fixture = ReadFixture();
        var fixtureCase = FindCase(fixture, "recorded-not-assigned-cash");
        var document = BuildDocument(fixtureCase.GetProperty("storedFiscalDocument"));
        var reader = new CountingFiscalDocumentReader(document);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(reader));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var first = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            adapter);
        var second = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            adapter);

        Assert.Equal(2, reader.ReadCount);
        Assert.Equal(
            JsonSerializer.Serialize(first, JsonOptions),
            JsonSerializer.Serialize(second, JsonOptions));
        Assert.Null(document.FiscalDocumentNumber);
        Assert.Null(document.FiscalSequenceValue);
        Assert.Null(document.FiscalNumberAssignedAt);
        Assert.Equal("not_assigned", first.FiscalNumberAssignmentState);
        Assert.DoesNotContain(
            typeof(GetDigitalSalesInvoicePresentationResponse).GetProperties().Select(property => property.Name),
            name => name.Contains("Print", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FiscalDocumentPresentationContractArtifactDocumentsSelectedEndpoint()
    {
        using var contract = ReadContractArtifact();
        var root = contract.RootElement;

        Assert.Equal("fiscal-document-presentation.v1", root.GetProperty("contractVersion").GetString());
        Assert.Equal("GET", root.GetProperty("method").GetString());
        Assert.Equal(
            "/v1/fiscal-documents/{fiscalDocumentId}/digital-sales-invoice/presentation",
            root.GetProperty("route").GetString());
        Assert.Equal("application/json", root.GetProperty("contentType").GetString());
        Assert.Equal(
            "digital-sales-invoice-presentation-json-v1",
            root.GetProperty("presentationVersion").GetString());
        Assert.Contains(
            root.GetProperty("responseFields").EnumerateArray(),
            field => field.GetProperty("name").GetString() == "presentation");
        Assert.Contains(
            root.GetProperty("statusCodes").EnumerateArray(),
            status => status.GetProperty("httpStatus").GetInt32() == StatusCodes.Status404NotFound);
    }

    private static async Task<GetDigitalSalesInvoicePresentationResponse> InvokeAsync(FiscalDocumentReadModel document)
    {
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new CountingFiscalDocumentReader(document)));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        return await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            adapter);
    }

    private static void AssertTopLevelFields(
        GetDigitalSalesInvoicePresentationResponse response,
        JsonElement expected)
    {
        Assert.Equal(expected.GetProperty("code").GetString(), response.Code);
        Assert.Equal(Guid.Parse(expected.GetProperty("fiscalDocumentId").GetString()!), response.FiscalDocumentId);
        Assert.Equal(expected.GetProperty("fiscalDocumentNumber").GetString(), response.FiscalDocumentNumber);
        Assert.Equal(expected.GetProperty("fiscalDocumentType").GetString(), response.FiscalDocumentType);
        Assert.Equal(expected.GetProperty("fiscalDocumentStatus").GetString(), response.FiscalDocumentStatus);
        Assert.Equal(expected.GetProperty("fiscalNumberAssignmentState").GetString(), response.FiscalNumberAssignmentState);
        Assert.Equal(expected.GetProperty("fiscalSeries").GetString(), response.FiscalSeries);
        Assert.Equal(expected.GetProperty("fiscalNumberPrefixText").GetString(), response.FiscalNumberPrefixText);
        Assert.Equal(expected.GetProperty("fiscalNumberSuffixText").GetString(), response.FiscalNumberSuffixText);
        Assert.Equal(ReadNullableDateTimeOffset(expected, "fiscalNumberAssignedAt"), response.FiscalNumberAssignedAt);
        Assert.Equal(DateTimeOffset.Parse(expected.GetProperty("recordedAt").GetString()!), response.RecordedAt);
        Assert.Equal(expected.GetProperty("voidStatus").GetString(), response.VoidStatus);
        Assert.Equal(expected.GetProperty("voidReasonCode").GetString(), response.VoidReasonCode);
        Assert.Equal(ReadNullableDateTimeOffset(expected, "voidedAt"), response.VoidedAt);
        Assert.Equal(expected.GetProperty("presentationVersion").GetString(), response.PresentationVersion);
        Assert.Equal(expected.GetProperty("templateVersion").GetString(), response.TemplateVersion);
        Assert.Equal(expected.GetProperty("contentType").GetString(), response.ContentType);
        Assert.NotNull(response.Presentation);
        Assert.Equal(response.PresentationVersion, response.Presentation.PresentationVersion);
        Assert.Equal(response.TemplateVersion, response.Presentation.SourceTemplateContractVersion);
        Assert.Equal(response.ContentType, response.Presentation.RenderFormat);
    }

    private static void AssertPresentationRows(
        GetDigitalSalesInvoicePresentationResponse response,
        JsonElement expectedRows)
    {
        Assert.NotNull(response.Presentation);
        var rows = response.Presentation.Sections
            .SelectMany(section => section.Rows)
            .ToDictionary(row => row.Key, StringComparer.Ordinal);

        foreach (var expectedRow in expectedRows.EnumerateArray())
        {
            var key = expectedRow.GetProperty("key").GetString()!;
            Assert.True(rows.TryGetValue(key, out var row), $"Missing presentation row '{key}'.");
            Assert.Equal(expectedRow.GetProperty("displayValue").GetString(), row.DisplayValue);
        }
    }

    private static FiscalDocumentReadModel BuildDocument(JsonElement source)
    {
        var assignedAt = ReadNullableDateTimeOffset(source, "fiscalNumberAssignedAt");
        var voidedAt = ReadNullableDateTimeOffset(source, "voidedAt");
        var documentId = Guid.Parse(source.GetProperty("fiscalDocumentId").GetString()!);

        return new FiscalDocumentReadModel(
            documentId,
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            source.GetProperty("fiscalIdentityId").ValueKind == JsonValueKind.Null
                ? null
                : Guid.Parse(source.GetProperty("fiscalIdentityId").GetString()!),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            source.GetProperty("fiscalDocumentType").GetString(),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            source.GetProperty("fiscalDocumentStatus").GetString(),
            source.GetProperty("fiscalSequencePolicyId").ValueKind == JsonValueKind.Null
                ? null
                : Guid.Parse(source.GetProperty("fiscalSequencePolicyId").GetString()!),
            source.GetProperty("fiscalSequenceValue").ValueKind == JsonValueKind.Null
                ? null
                : source.GetProperty("fiscalSequenceValue").GetInt64(),
            source.GetProperty("fiscalDocumentNumber").GetString(),
            source.GetProperty("fiscalSeries").GetString(),
            source.GetProperty("fiscalNumberPrefixText").GetString(),
            source.GetProperty("fiscalNumberSuffixText").GetString(),
            assignedAt,
            assignedAt is null ? null : "pos-server:system",
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            "central-finality-001",
            "upstream_finality_ref",
            new string('a', 64),
            "sha256:v1",
            "matched",
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 7, 1),
            source.GetProperty("voidStatus").GetString(),
            source.GetProperty("voidReasonCode").GetString(),
            voidedAt,
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.Parse(source.GetProperty("recordedAt").GetString()!),
            DateTimeOffset.Parse(source.GetProperty("recordedAt").GetString()!).AddMinutes(1),
            [],
            [],
            [Line(documentId)],
            [Tender(documentId, source.GetProperty("tenderType").GetString())],
            [Tax(documentId)],
            [Discount(documentId)],
            [Total(documentId)]);
    }

    private static FiscalDocumentLineReadModel Line(Guid fiscalDocumentId) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000201"),
            fiscalDocumentId,
            1,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            "Parking fee",
            1,
            12500,
            12500,
            1000,
            0,
            11500,
            "PHP",
            "line-source-001",
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"));

    private static FiscalTenderReadModel Tender(Guid fiscalDocumentId, string? tenderTypeCodeKey) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000301"),
            fiscalDocumentId,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            tenderTypeCodeKey,
            12500,
            "PHP",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "provider-ref-001",
            "{\"source_system\":\"central_pms\"}",
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"));

    private static FiscalTaxDetailReadModel Tax(Guid fiscalDocumentId) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000401"),
            fiscalDocumentId,
            Guid.Parse("10000000-0000-0000-0000-000000000201"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            0,
            12500,
            0,
            "PHP",
            "{\"source_system\":\"central_pms\"}",
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"));

    private static FiscalDiscountPrivilegeDetailReadModel Discount(Guid fiscalDocumentId) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000501"),
            fiscalDocumentId,
            Guid.Parse("10000000-0000-0000-0000-000000000201"),
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            12500,
            1000,
            0,
            "PHP",
            "beneficiary-ref-001",
            "evidence-ref-001",
            "discount-validation-001",
            "{\"source_system\":\"central_pms\"}",
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"));

    private static FiscalTotalReadModel Total(Guid fiscalDocumentId) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000601"),
            fiscalDocumentId,
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            12500,
            "PHP",
            "{\"source_system\":\"central_pms\"}",
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"));

    private static JsonElement FindCase(JsonDocument fixture, string caseId) =>
        fixture.RootElement
            .GetProperty("cases")
            .EnumerateArray()
            .Single(item => item.GetProperty("caseId").GetString() == caseId);

    private static DateTimeOffset? ReadNullableDateTimeOffset(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).ValueKind == JsonValueKind.Null
            ? null
            : DateTimeOffset.Parse(element.GetProperty(propertyName).GetString()!);

    private static JsonDocument ReadFixture() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "docs",
            "v1.3",
            "pos-server",
            "digital-sales-invoice",
            "fixtures",
            "fiscal-document-presentation-contract-v1.json")));

    private static JsonDocument ReadContractArtifact() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "contracts",
            "pos-server",
            "fiscal-document-presentation.v1.json")));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "ExitPass.PoSServer.sln");

            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate POS Server repository root.");
    }

    private sealed class CountingFiscalDocumentReader : IFiscalDocumentReader
    {
        private readonly FiscalDocumentReadModel document;

        public CountingFiscalDocumentReader(FiscalDocumentReadModel document)
        {
            this.document = document;
        }

        public int ReadCount { get; private set; }

        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(document.FiscalDocumentId == fiscalDocumentId ? document : null);
        }
    }
}
