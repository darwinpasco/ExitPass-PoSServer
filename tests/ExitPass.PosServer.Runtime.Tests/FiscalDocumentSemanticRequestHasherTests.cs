using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class FiscalDocumentSemanticRequestHasherTests
{
    private const string ExpectedRepresentativeHash = "6a490379e4275a57f0a0695ff9dbd1271c4480adaeeefb9b6bfbd11e4d1ed201";

    [Fact]
    public void RepresentativeFixtureMatchesPosServerCanonicalSourceAndHash()
    {
        using var fixture = ReadFixture();
        var root = fixture.RootElement;
        var command = RepresentativeCommand();

        var canonicalSource = FiscalDocumentSemanticRequestHasher.Canonicalize(command);
        var hash = FiscalDocumentSemanticRequestHasher.Hash(command);
        using var canonicalDocument = JsonDocument.Parse(canonicalSource);

        Assert.Equal("sha256:v1", root.GetProperty("canonical_source_version").GetString());
        Assert.Equal(FiscalDocumentSemanticRequestHasher.Algorithm, root.GetProperty("hash_algorithm").GetString());
        Assert.Equal(FiscalDocumentSemanticRequestHasher.Version, root.GetProperty("hash_algorithm_version").GetString());
        Assert.Equal(20, canonicalDocument.RootElement.EnumerateObject().Count());
        Assert.Equal(root.GetProperty("canonical_source_fact_count").GetInt32(), canonicalDocument.RootElement.EnumerateObject().Count());
        Assert.Equal(root.GetProperty("canonical_source_text").GetString(), canonicalSource);
        Assert.Equal(root.GetProperty("expected_sha256_hash").GetString(), hash);
        Assert.Equal(ExpectedRepresentativeHash, hash);
        Assert.Equal(hash, ComputeSha256LowerHex(canonicalSource));
    }

    [Fact]
    public void IdenticalPosServerRequestFactsProduceStableSha256V1Hash()
    {
        var first = RepresentativeCommand();
        var second = RepresentativeCommand();

        Assert.Equal(FiscalDocumentSemanticRequestHasher.Canonicalize(first), FiscalDocumentSemanticRequestHasher.Canonicalize(second));
        Assert.Equal(ExpectedRepresentativeHash, FiscalDocumentSemanticRequestHasher.Hash(first));
        Assert.Equal(ExpectedRepresentativeHash, FiscalDocumentSemanticRequestHasher.Hash(second));
    }

    [Fact]
    public void ChangedStableFiscalFactsChangeSha256V1Hash()
    {
        var original = RepresentativeCommand();
        var changedLine = original.DocumentLines![0] with
        {
            GrossAmountMinorUnits = 13000,
            NetAmountMinorUnits = 12000
        };
        var changed = original with { DocumentLines = [changedLine] };

        Assert.NotEqual(FiscalDocumentSemanticRequestHasher.Canonicalize(original), FiscalDocumentSemanticRequestHasher.Canonicalize(changed));
        Assert.NotEqual(FiscalDocumentSemanticRequestHasher.Hash(original), FiscalDocumentSemanticRequestHasher.Hash(changed));
    }

    [Fact]
    public void DictionaryOrderAndWhitespaceDoNotChangeCanonicalSource()
    {
        var original = RepresentativeCommand();
        var equivalent = original with
        {
            SitePosServerRef = " site-pos-server-parity-001 ",
            FiscalDocumentTypeCodeKey = " sales_invoice ",
            PayableBasis = original.PayableBasis! with
            {
                CurrencyCode = " php ",
                ReferenceContext = new Dictionary<string, string>
                {
                    ["source_system"] = " central_pms ",
                    ["basis_source"] = " payment_confirmation "
                },
                DiscountReferences =
                [
                    original.PayableBasis!.DiscountReferences![0] with
                    {
                        ReferenceContext = new Dictionary<string, string>
                        {
                            ["validation_scope"] = " statutory_discount_reference ",
                            ["approved_by_system"] = " central_pms "
                        }
                    }
                ]
            },
            ReferenceContext = new Dictionary<string, string>
            {
                ["source_system"] = " central_pms ",
                ["parity_fixture"] = " pos_server_sha256_v1 "
            },
            DocumentLines =
            [
                original.DocumentLines![0] with
                {
                    CurrencyCode = " php ",
                    Description = " Parking fee ",
                    SourceRef = " line-source-parity-001 ",
                    LineContext = new Dictionary<string, string>
                    {
                        ["source_system"] = " central_pms ",
                        ["line_source"] = " parking_charge "
                    }
                }
            ],
            Tenders =
            [
                original.Tenders![0] with
                {
                    CurrencyCode = " php ",
                    PaymentFinalityRef = " central-finality-parity-001 ",
                    TenderContext = new Dictionary<string, string>
                    {
                        ["tender_source"] = " payment_confirmation ",
                        ["source_system"] = " central_pms "
                    }
                }
            ],
            TaxDetails =
            [
                original.TaxDetails![0] with
                {
                    CurrencyCode = " php ",
                    TaxContext = new Dictionary<string, string>
                    {
                        ["tax_source"] = " parking_tax_classification ",
                        ["source_system"] = " central_pms "
                    }
                }
            ],
            DiscountPrivilegeDetails =
            [
                original.DiscountPrivilegeDetails![0] with
                {
                    CurrencyCode = " php ",
                    ApprovalRef = " discount-validation-parity-001 ",
                    DiscountPrivilegeContext = new Dictionary<string, string>
                    {
                        ["source_system"] = " central_pms ",
                        ["privilege_source"] = " central_pms_discount_reference "
                    }
                }
            ],
            Totals =
            [
                original.Totals![0] with
                {
                    CurrencyCode = " php ",
                    TotalContext = new Dictionary<string, string>
                    {
                        ["total_source"] = " payable_basis ",
                        ["source_system"] = " central_pms "
                    }
                }
            ]
        };

        Assert.Equal(FiscalDocumentSemanticRequestHasher.Canonicalize(original), FiscalDocumentSemanticRequestHasher.Canonicalize(equivalent));
        Assert.Equal(ExpectedRepresentativeHash, FiscalDocumentSemanticRequestHasher.Hash(equivalent));
    }

    [Fact]
    public void CanonicalSourceExcludesResponseRetryAndFiscalNumberOutcomeFields()
    {
        var canonicalSource = FiscalDocumentSemanticRequestHasher.Canonicalize(RepresentativeCommand());

        Assert.DoesNotContain("fiscal_document_id", canonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_document_number", canonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fiscal_sequence_value", canonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("result_classification", canonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("retry", canonicalSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("replay", canonicalSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IdempotencyResolverUsesRepresentativeScopeKeyAndSha256V1Hash()
    {
        var idempotency = FiscalIssuanceIdempotencyResolver.Resolve(RepresentativeCommand());

        Assert.Equal(
            "fiscal_document_creation:10000000000000000000000000000001:10000000000000000000000000000101",
            idempotency.Scope);
        Assert.Equal("central-finality-parity-001", idempotency.Key);
        Assert.Equal(ExpectedRepresentativeHash, idempotency.SemanticRequestHash);
    }

    private static FiscalDocumentCreationCommand RepresentativeCommand() =>
        new(
            "site-pos-server-parity-001",
            "sales_invoice",
            new FiscalizationPayableBasisInput(
                "payable-basis-parity-001",
                "central-finality-parity-001",
                "PHP",
                12500,
                [
                    new FiscalDiscountReferenceInput(
                        "discount-validation-parity-001",
                        FiscalDiscountReferenceStatus.Approved,
                        true,
                        Dictionary(
                            ("validation_scope", "statutory_discount_reference"),
                            ("approved_by_system", "central_pms")))
                ],
                Dictionary(
                    ("source_system", "central_pms"),
                    ("basis_source", "payment_confirmation"))),
            SitePosServerId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            ChannelTerminalId: Guid.Parse("10000000-0000-0000-0000-000000000011"),
            FiscalDocumentTypeCodeId: Guid.Parse("10000000-0000-0000-0000-000000000101"),
            FiscalDocumentStatusCodeId: Guid.Parse("10000000-0000-0000-0000-000000000102"),
            BusinessDayDate: new DateOnly(2026, 7, 6),
            CentralPmsParkingSessionRef: "parking-session-parity-001",
            CentralPmsPaymentAttemptRef: "payment-attempt-parity-001",
            CentralPmsPaymentConfirmationRef: "payment-confirmation-parity-001",
            PaymentFinalityRef: "central-finality-parity-001",
            VendorAckRef: "vendor-ack-parity-001",
            DocumentLinks: [],
            DocumentLines:
            [
                new FiscalDocumentLineInput(
                    1,
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    "Parking fee",
                    1,
                    12500,
                    12500,
                    1000,
                    0,
                    11500,
                    "PHP",
                    SourceRef: "line-source-parity-001",
                    LineContext: Dictionary(
                        ("source_system", "central_pms"),
                        ("line_source", "parking_charge")))
            ],
            Tenders:
            [
                new FiscalTenderInput(
                    Guid.Parse("10000000-0000-0000-0000-000000000301"),
                    12500,
                    "PHP",
                    CentralPmsPaymentAttemptRef: "payment-attempt-parity-001",
                    CentralPmsPaymentConfirmationRef: "payment-confirmation-parity-001",
                    PaymentFinalityRef: "central-finality-parity-001",
                    ProviderRef: "provider-ref-parity-001",
                    TenderContext: Dictionary(
                        ("source_system", "central_pms"),
                        ("tender_source", "payment_confirmation")))
            ],
            TaxDetails:
            [
                new FiscalTaxDetailInput(
                    Guid.Parse("10000000-0000-0000-0000-000000000401"),
                    Guid.Parse("10000000-0000-0000-0000-000000000402"),
                    12500,
                    0,
                    "PHP",
                    LineSequence: 1,
                    TaxRate: 0,
                    TaxContext: Dictionary(
                        ("source_system", "central_pms"),
                        ("tax_source", "parking_tax_classification")))
            ],
            DiscountPrivilegeDetails:
            [
                new FiscalDiscountPrivilegeDetailInput(
                    Guid.Parse("10000000-0000-0000-0000-000000000501"),
                    12500,
                    1000,
                    0,
                    "PHP",
                    LineSequence: 1,
                    BeneficiaryRef: "beneficiary-ref-parity-001",
                    EvidenceRef: "evidence-ref-parity-001",
                    ApprovalRef: "discount-validation-parity-001",
                    DiscountPrivilegeContext: Dictionary(
                        ("source_system", "central_pms"),
                        ("privilege_source", "central_pms_discount_reference")))
            ],
            Totals:
            [
                new FiscalTotalInput(
                    Guid.Parse("10000000-0000-0000-0000-000000000601"),
                    12500,
                    "PHP",
                    Dictionary(
                        ("source_system", "central_pms"),
                        ("total_source", "payable_basis")))
            ],
            ReferenceContext: Dictionary(
                ("source_system", "central_pms"),
                ("parity_fixture", "pos_server_sha256_v1")));

    private static Dictionary<string, string> Dictionary(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    private static string ComputeSha256LowerHex(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static JsonDocument ReadFixture()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "docs",
            "v1.3",
            "pos-server",
            "fiscal-numbering",
            "fixtures",
            "pos_server_semantic_hash_sha256_v1_representative_fixture.json");
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
                "fiscal-numbering",
                "ExitPass_POS_Server_Fiscal_Numbering_Idempotency_Runtime_Note_v1.0.md");

            if (File.Exists(candidate))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate POS Server repository root.");
    }
}
