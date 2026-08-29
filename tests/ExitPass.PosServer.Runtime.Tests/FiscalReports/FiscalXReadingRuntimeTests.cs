using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalXReadingRuntimeTests
{
    private static readonly Guid SiteId = Guid.Parse("71000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("71000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("71000000-0000-4000-8000-000000000003");
    private static readonly Guid ContractId = Guid.Parse("f6766f48-62f0-513f-b9eb-e61c2f3e8c66");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-08-04T10:30:00+08:00");

    [Fact]
    public void SemanticHashIsDeterministicAndExcludesCorrelationAndPrincipal()
    {
        var first = Command();
        var second = first with { CorrelationId = "different-correlation", RequestedByRef = "different-actor", ServiceIdentityRef = "different-service" };

        Assert.Equal(FiscalXReadingSemanticRequestHasher.Compute(first, Period()), FiscalXReadingSemanticRequestHasher.Compute(second, Period()));
        var canonical = FiscalXReadingSemanticRequestHasher.CanonicalSource(first, Period());
        Assert.DoesNotContain("correlation", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pos-server-fiscal-report-request:sha256:v1", canonical, StringComparison.Ordinal);
        Assert.Contains(PeriodId.ToString("D"), canonical, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("operation-2")]
    [InlineData("operation-3")]
    public void MaterialOperationIdentityChangesHash(string operationKey)
    {
        Assert.NotEqual(
            FiscalXReadingSemanticRequestHasher.Compute(Command(), Period()),
            FiscalXReadingSemanticRequestHasher.Compute(Command() with { OperationKey = operationKey }, Period()));
    }

    [Fact]
    public void OrdinarySalesInvoiceAggregatesWithoutCounterOrStateMutationFacts()
    {
        var aggregate = new FiscalXReadingAggregationService().Aggregate(
            [Document("recorded", sequence: 1)], [], "PHP");

        Assert.Equal(1, aggregate.QualifyingDocumentCount);
        Assert.Equal(10_000, aggregate.Amounts.GrossSalesAmountMinorUnits);
        Assert.Equal(8_000, aggregate.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(2_000, aggregate.Amounts.DiscountAmountMinorUnits);
        Assert.Equal(8_000, aggregate.Tenders.Single().AmountMinorUnits);
        Assert.Equal("cash", aggregate.Tenders.Single().Classification);
        Assert.Equal(0, aggregate.Amounts.RefundAmountMinorUnits);
        Assert.Equal(0, aggregate.Amounts.ServiceChargeAmountMinorUnits);
    }

    [Fact]
    public void IssuedSalesInvoiceAggregatesAsAuthoritativeSale()
    {
        var aggregate = new FiscalXReadingAggregationService().Aggregate(
            [Document("issued", sequence: 1)], [], "PHP");

        Assert.Equal(1, aggregate.QualifyingDocumentCount);
        Assert.Equal(10_000, aggregate.Amounts.GrossSalesAmountMinorUnits);
        Assert.Equal(8_000, aggregate.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(8_000, aggregate.Tenders.Single().AmountMinorUnits);
    }

    [Fact]
    public void SeniorCitizenAndPwdRemainSeparateFromVatExemption()
    {
        var senior = Document("recorded", 1) with
        {
            Lines = [new("parking_fee", 9_500, 1_500, 0, 8_000, "PHP")],
            Taxes = [new("vat_exempt", false, 9_500, 0, "PHP")],
            Statutory = new("SENIOR_CITIZEN", 1_500, 500, 8_000, "PHP"),
            Discounts = [new("statutory_peer", 1_500, 500, "PHP")]
        };
        var pwd = Document("recorded", 2) with
        {
            Lines = [new("parking_fee", 9_500, 1_500, 0, 8_000, "PHP")],
            Taxes = [new("vat_exempt", false, 9_500, 0, "PHP")],
            Statutory = new("PWD", 1_500, 500, 8_000, "PHP"),
            Discounts = [new("statutory_peer", 1_500, 500, "PHP")]
        };

        var aggregate = new FiscalXReadingAggregationService().Aggregate([senior, pwd], [], "PHP");

        Assert.Equal(1_500, aggregate.Amounts.SeniorCitizenDiscountAmountMinorUnits);
        Assert.Equal(1_500, aggregate.Amounts.PwdDiscountAmountMinorUnits);
        Assert.Equal(1_000, aggregate.Amounts.VatExemptionAmountMinorUnits);
        Assert.Contains(aggregate.Discounts, item => item.Classification == "senior_citizen_statutory");
        Assert.Contains(aggregate.Discounts, item => item.Classification == "pwd_statutory");
        Assert.Contains(aggregate.Discounts, item => item.Classification == "vat_exemption_adjustment");
    }

    [Fact]
    public void StatutoryInvoiceDoesNotCountVatExemptionAsLineDiscount()
    {
        var document = Document("issued", 1) with
        {
            Lines = [new("parking_fee", 2_232, 446, 0, 1_786, "PHP")],
            Tenders = [new("digital_wallet", 1_786, "PHP")],
            Taxes = [new("vat_exempt", false, 2_232, 0, "PHP")],
            Discounts = [new("statutory_peer", 446, 268, "PHP")],
            Statutory = new("SENIOR_CITIZEN", 446, 268, 1_786, "PHP")
        };

        var aggregate = new FiscalXReadingAggregationService().Aggregate([document], [], "PHP");

        Assert.Equal(2_232, aggregate.Amounts.GrossSalesAmountMinorUnits);
        Assert.Equal(446, aggregate.Amounts.DiscountAmountMinorUnits);
        Assert.Equal(268, aggregate.Amounts.VatExemptionAmountMinorUnits);
        Assert.Equal(1_786, aggregate.Amounts.NetSalesAmountMinorUnits);
    }

    [Fact]
    public void VoidedInvoiceIsExcludedFromSalesAndIncludedAsVoidTotal()
    {
        var aggregate = new FiscalXReadingAggregationService().Aggregate([Document("voided", 1)], [], "PHP");
        Assert.Equal(1, aggregate.QualifyingDocumentCount);
        Assert.Equal(0, aggregate.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(8_000, aggregate.Amounts.VoidAmountMinorUnits);
    }

    [Theory]
    [InlineData("refund")]
    [InlineData("return")]
    [InlineData("adjustment")]
    public void UngovernedDocumentTypesFailClosed(string documentType)
    {
        var exception = Assert.Throws<FiscalXReadingSafeException>(() =>
            new FiscalXReadingAggregationService().Aggregate([Document("recorded", 1) with { DocumentType = documentType }], [], "PHP"));
        Assert.Equal(FiscalXReadingOutcome.UnsupportedSourceClassification, exception.Outcome);
    }

    [Fact]
    public void MixedCurrencyAndUnsupportedTenderFailClosed()
    {
        var mixed = Document("recorded", 1) with { Tenders = [new("cash", 8_000, "USD")] };
        Assert.Equal(FiscalXReadingOutcome.MixedCurrency,
            Assert.Throws<FiscalXReadingSafeException>(() => new FiscalXReadingAggregationService().Aggregate([mixed], [], "PHP")).Outcome);

        var unsupported = Document("recorded", 1) with { Tenders = [new("crypto_asset", 8_000, "PHP")] };
        Assert.Equal(FiscalXReadingOutcome.UnsupportedSourceClassification,
            Assert.Throws<FiscalXReadingSafeException>(() => new FiscalXReadingAggregationService().Aggregate([unsupported], [], "PHP")).Outcome);
    }

    [Fact]
    public void UnexplainedSequenceGapFailsClosed()
    {
        var documents = new[] { Document("recorded", 1), Document("recorded", 3) };
        var gaps = new[] { new FiscalXReadingSourceGap(Guid.NewGuid(), documents[0].SequencePolicyId!.Value, 2, "unexplained") };
        Assert.Equal(FiscalXReadingOutcome.ReconciliationFailure,
            Assert.Throws<FiscalXReadingSafeException>(() => new FiscalXReadingAggregationService().Aggregate(documents, gaps, "PHP")).Outcome);
    }

    [Fact]
    public async Task ServiceValidatesBeforeRepositoryAndReturnsRepositoryReplay()
    {
        var repository = new StubRepository(new(FiscalXReadingOutcome.Replayed, Record()));
        var service = new FiscalXReadingService(repository);
        Assert.Equal(FiscalXReadingOutcome.InvalidRequest, (await service.GenerateAsync(Command() with { OperationKey = " " })).Outcome);
        Assert.Equal(0, repository.GenerateCalls);
        Assert.Equal(FiscalXReadingOutcome.Replayed, (await service.GenerateAsync(Command())).Outcome);
        Assert.Equal(1, repository.GenerateCalls);
    }

    private static FiscalXReadingSourceDocument Document(string status, long sequence) => new(
        Guid.NewGuid(), "sales_invoice", status, ObservedAt.AddMinutes(-10), Guid.Parse("71000000-0000-4000-8000-000000000099"), sequence,
        $"SI-{sequence:0000}", "SI", [new("parking_fee", 10_000, 2_000, 0, 8_000, "PHP")],
        [new("cash", 8_000, "PHP")], [new("vat_exempt", false, 8_000, 0, "PHP")],
        [new("coupon", 2_000, 0, "PHP")], null);

    private static FiscalXReadingCommand Command() => new("operation-1", SiteId, IdentityId, ObservedAt, "actor", "service", "correlation");
    private static FiscalXReadingPeriod Period() => new(PeriodId, ContractId, SiteId, IdentityId, new(2026, 8, 4), ObservedAt.AddHours(-2), ObservedAt.AddHours(10), "Asia/Manila", new(0, 0), "PHP", 1);
    private static FiscalXReadingRecord Record() => new(Guid.NewGuid(), "X-TEST", Guid.NewGuid(), "operation-1", "X_READING", FiscalXReadingContract.ContractVersion, FiscalXReadingContract.SemanticHashVersion, SiteId, IdentityId, PeriodId, new(2026, 8, 4), ObservedAt.AddHours(-2), ObservedAt.AddHours(10), "Asia/Manila", new(0, 0), "PHP", ObservedAt, ObservedAt, 0, new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), [], [], [], "correlation", "X-TEST", true);

    private sealed class StubRepository(FiscalXReadingResult result) : IFiscalXReadingRepository
    {
        public int GenerateCalls { get; private set; }
        public Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default) { GenerateCalls++; return Task.FromResult(result); }
        public Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
