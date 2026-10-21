using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public sealed class UnavailableFiscalBusinessDateResolutionRepository
    : IFiscalBusinessDateResolutionRepository
{
    public Task<FiscalBusinessDateResolution> ResolveAsync(
        FiscalBusinessDateResolutionQuery query,
        CancellationToken cancellationToken = default) =>
        throw new FiscalBusinessDateResolutionUnavailableException(
            "POS Server persistence is not configured for fiscal business-date resolution.");
}
