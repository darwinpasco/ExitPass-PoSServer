namespace ExitPass.PosServer.Runtime.FiscalReports;

public enum FiscalPeriodMutationKind
{
    FiscalDocumentCreate,
    FiscalDocumentVoid,
    Refund,
    Return,
    Adjustment,
    ServiceCharge
}

public static class FiscalPeriodMutationGuard
{
    public static void EnsureSupported(FiscalPeriodMutationKind mutationKind)
    {
        if (mutationKind is FiscalPeriodMutationKind.FiscalDocumentCreate or FiscalPeriodMutationKind.FiscalDocumentVoid)
        {
            return;
        }

        throw new FiscalCloseBoundaryException(
            FiscalCloseBoundaryErrorCode.UnsupportedCrossPeriodMutation,
            "This period-affecting fiscal mutation is not supported by the governed contract.");
    }
}
