namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public interface ISalesInvoiceHeaderProfileRepository
{
    Task<FiscalIdentityProfile> CreateFiscalIdentityAsync(
        FiscalIdentityProfile identity,
        CancellationToken cancellationToken);

    Task<FiscalIdentityProfile?> GetFiscalIdentityAsync(
        Guid fiscalIdentityId,
        CancellationToken cancellationToken);

    Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(
        SalesInvoiceHeaderProfile profile,
        CancellationToken cancellationToken);

    Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        CancellationToken cancellationToken);

    Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset approvedAt,
        string approvedByRef,
        CancellationToken cancellationToken);

    Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset retiredAt,
        string retiredByRef,
        CancellationToken cancellationToken);

    Task<SalesInvoiceHeaderProfileResolutionResult> ResolveEffectiveSalesInvoiceHeaderProfileAsync(
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken);
}
