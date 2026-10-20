using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

internal sealed class UnavailableFiscalZCounterResetRepository : IFiscalZCounterResetRepository
{
    private static readonly FiscalZCounterResetResult Failure = new(
        FiscalZCounterResetOutcome.PersistenceFailure,
        SafeMessage: "POS Server PostgreSQL persistence is not configured for manual Z Counter administration.");

    public Task<FiscalZCounterResetResult> ReadStateAsync(FiscalZCounterStateQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
    public Task<FiscalZCounterResetResult> ResetAsync(FiscalZCounterResetCommand command, CancellationToken cancellationToken = default) => Task.FromResult(Failure);
}
