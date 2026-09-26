using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class CloseableFiscalBusinessDateRuntimeTests
{
    private static readonly Guid SiteId = Guid.Parse("78000000-0000-4000-8000-000000000101");
    private static readonly Guid IdentityId = Guid.Parse("78000000-0000-4000-8000-000000000102");

    [Fact]
    public async Task ServiceNormalizesCurrencyAndPreservesAuthoritativeRows()
    {
        var row = new CloseableFiscalBusinessDate(
            Guid.Parse("78000000-0000-4000-8000-000000000103"),
            new DateOnly(2026, 9, 20),
            DateTimeOffset.Parse("2026-09-19T23:00:00Z"),
            DateTimeOffset.Parse("2026-09-20T23:00:00Z"),
            "OPEN",
            3,
            7);
        var repository = new StubRepository(new(
            SiteId,
            IdentityId,
            "PHP",
            [row],
            "closeable_fiscal_business_dates_read"));

        var result = await new CloseableFiscalBusinessDateService(repository)
            .ReadAsync(SiteId, IdentityId, " php ");

        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal("PHP", repository.CurrencyCode);
        Assert.Same(row, Assert.Single(result.FiscalBusinessDates));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PH")]
    [InlineData("PESO")]
    public async Task ServiceRejectsInvalidScopeBeforePersistence(string? currencyCode)
    {
        var repository = new StubRepository(new(
            SiteId,
            IdentityId,
            "PHP",
            [],
            "closeable_fiscal_business_dates_read"));
        var service = new CloseableFiscalBusinessDateService(repository);

        var invalidCurrency = await service.ReadAsync(SiteId, IdentityId, currencyCode!);
        var invalidSite = await service.ReadAsync(Guid.Empty, IdentityId, "PHP");
        var invalidIdentity = await service.ReadAsync(SiteId, Guid.Empty, "PHP");

        Assert.Equal("closeable_fiscal_business_dates_invalid", invalidCurrency.Code);
        Assert.Equal("closeable_fiscal_business_dates_invalid", invalidSite.Code);
        Assert.Equal("closeable_fiscal_business_dates_invalid", invalidIdentity.Code);
        Assert.Equal(0, repository.ReadCalls);
    }

    private sealed class StubRepository(CloseableFiscalBusinessDatesSnapshot result)
        : ICloseableFiscalBusinessDateRepository
    {
        public int ReadCalls { get; private set; }
        public string? CurrencyCode { get; private set; }

        public Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
            Guid sitePosServerId,
            Guid fiscalIdentityId,
            string currencyCode,
            CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            CurrencyCode = currencyCode;
            return Task.FromResult(result);
        }
    }
}
