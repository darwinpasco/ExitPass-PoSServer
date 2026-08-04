namespace ExitPass.PosServer.Runtime.FiscalReports;

public enum FiscalCloseBoundaryErrorCode
{
    ReportingPeriodUnavailable,
    ReportingPeriodAmbiguous,
    ReportingPeriodClosed,
    ReportingPeriodAssignmentMismatch,
    StateInitializationRequired,
    StaleStateVersion,
    PriorPeriodSequenceInvalid,
    LockTimeout,
    RetryableConcurrencyFailure,
    UnsupportedCrossPeriodMutation
}

public sealed class FiscalCloseBoundaryException : Exception
{
    public FiscalCloseBoundaryException(FiscalCloseBoundaryErrorCode errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public FiscalCloseBoundaryErrorCode ErrorCode { get; }
}

public sealed record FiscalReportingPeriodAssignment(
    Guid FiscalReportingPeriodId,
    Guid FiscalReportingContractVersionId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    long PeriodSequence,
    Guid? ExpectedPriorPeriodId,
    DateTimeOffset FiscalEventAt);
