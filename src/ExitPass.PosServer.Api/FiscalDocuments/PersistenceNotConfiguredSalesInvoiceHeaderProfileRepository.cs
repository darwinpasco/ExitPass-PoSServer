using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class PersistenceNotConfiguredSalesInvoiceHeaderProfileRepository : ISalesInvoiceHeaderProfileRepository
{
    public Task<FiscalIdentityProfile> CreateFiscalIdentityAsync(FiscalIdentityProfile identity, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<FiscalIdentityProfile?> GetFiscalIdentityAsync(Guid fiscalIdentityId, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<FiscalIdentityProfile> UpdateFiscalIdentityAsync(FiscalIdentityProfile identity, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<bool> IsFiscalIdentityInGovernedUseAsync(Guid fiscalIdentityId, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ListHeaderProfilesAsync(Guid? siteId, Guid? sitePosServerId, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfile> UpdateHeaderProfileDraftAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset approvedAt, string approvedByRef, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset retiredAt, string retiredByRef, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfileResolutionResult> ResolveEffectiveSalesInvoiceHeaderProfileAsync(Guid siteId, Guid sitePosServerId, DateTimeOffset effectiveAt, CancellationToken cancellationToken) =>
        throw NotConfigured();

    public Task<SalesInvoiceHeaderProfileUsage> GetHeaderProfileUsageAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken) =>
        throw NotConfigured();

    private static SalesInvoiceHeaderProfileAdminException NotConfigured() =>
        new("persistence_not_configured", "POS Server persistence is not configured.");
}
