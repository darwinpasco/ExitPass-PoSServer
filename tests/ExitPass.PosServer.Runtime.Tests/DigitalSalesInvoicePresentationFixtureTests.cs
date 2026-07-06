using System.Text.Json;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class DigitalSalesInvoicePresentationFixtureTests
{
    private static readonly string[] ExpectedSectionOrder =
    [
        "header",
        "sellerSitePosIdentity",
        "documentIdentity",
        "fiscalNumbering",
        "parkingPaymentReferences",
        "lineItems",
        "discounts",
        "taxes",
        "tenders",
        "totals",
        "auditHashStatus",
        "footerDisclaimers",
        "deferredPlaceholders"
    ];

    [Theory]
    [InlineData("digital-sales-invoice-presentation-assigned-sample-v1.json")]
    [InlineData("digital-sales-invoice-presentation-not-assigned-sample-v1.json")]
    public void PresentationSampleJsonParsesAndUsesExpectedVersionsAndSections(string fileName)
    {
        using var fixture = ReadFixture(fileName);
        var presentation = fixture.RootElement.GetProperty("presentation");

        Assert.Equal(
            "digital-sales-invoice-presentation-json-v1",
            presentation.GetProperty("presentationVersion").GetString());
        Assert.Equal(
            "digital-sales-invoice-json-v1",
            presentation.GetProperty("sourceTemplateContractVersion").GetString());
        Assert.Equal(
            "PH_DIGITAL_SALES_INVOICE",
            presentation.GetProperty("fiscalTemplateFamily").GetString());
        Assert.Equal(
            "application/json",
            presentation.GetProperty("renderFormat").GetString());

        var sectionNames = presentation
            .GetProperty("sections")
            .EnumerateArray()
            .Select(section => section.GetProperty("name").GetString() ?? string.Empty)
            .ToArray();
        Assert.Equal(ExpectedSectionOrder, sectionNames);
    }

    [Theory]
    [InlineData("digital-sales-invoice-presentation-assigned-sample-v1.json")]
    [InlineData("digital-sales-invoice-presentation-not-assigned-sample-v1.json")]
    public void PresentationSampleJsonMarksDeferredPlaceholdersAsDeferred(string fileName)
    {
        using var fixture = ReadFixture(fileName);
        var deferredSection = fixture.RootElement
            .GetProperty("presentation")
            .GetProperty("sections")
            .EnumerateArray()
            .Single(section => section.GetProperty("name").GetString() == "deferredPlaceholders");

        Assert.Equal("deferred", deferredSection.GetProperty("posture").GetString());
        Assert.All(
            deferredSection.GetProperty("rows").EnumerateArray(),
            row =>
            {
                Assert.Equal("deferred", row.GetProperty("posture").GetString());
                Assert.Equal("deferred", row.GetProperty("valueKind").GetString());
            });
    }

    [Fact]
    public void NotAssignedPresentationSampleIncludesFiscalNumberNotice()
    {
        using var fixture = ReadFixture("digital-sales-invoice-presentation-not-assigned-sample-v1.json");
        var presentation = fixture.RootElement.GetProperty("presentation");

        Assert.Equal("not_assigned", presentation.GetProperty("numberingState").GetString());
        Assert.Contains(
            presentation.GetProperty("notices").EnumerateArray(),
            notice =>
                notice.GetProperty("code").GetString() == "fiscal_number_not_assigned" &&
                notice.GetProperty("severity").GetString() == "warning");
    }

    private static JsonDocument ReadFixture(string fileName)
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "docs",
            "v1.3",
            "pos-server",
            "digital-sales-invoice",
            "fixtures",
            fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "docs",
                "v1.3",
                "pos-server",
                "digital-sales-invoice",
                "ExitPass_POS_Server_Digital_Sales_Invoice_UI_Print_Preview_Consumer_Fixture_v1.0.md");

            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate POS Server repository root.");
    }
}
