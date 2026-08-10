namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AnnexE1Service(IAnnexE1Repository repository)
{
    public Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand command, CancellationToken cancellationToken = default)
    {
        if (!ValidReference(command.OperationKey, 200) || command.SitePosServerId == Guid.Empty ||
            command.FiscalIdentityId == Guid.Empty || command.FiscalReportingPeriodId == Guid.Empty ||
            !ValidCurrency(command.CurrencyCode) || !AnnexE1FactTypes.All.Contains(command.FactType) ||
            command.FactStatus is not (AnnexE1FactStatuses.Recorded or AnnexE1FactStatuses.AttestedZero) ||
            command.AmountMinorUnits < 0 || command.SourceDocumentCount < 0 ||
            !ValidReference(command.ApprovalReference, 200) || !ValidReference(command.RequestedByRef, 200) ||
            !ValidReference(command.ServiceIdentityRef, 200) || !ValidReference(command.CorrelationId, 200) ||
            !OptionalReference(command.FirstSourceReference, 160) || !OptionalReference(command.LastSourceReference, 160) ||
            !OptionalReference(command.SourceEventReference, 200) || !OptionalReference(command.CorrectionReason, 100))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "The Annex E-1 accounting-fact request is incomplete or invalid."));
        if (command.FactStatus == AnnexE1FactStatuses.AttestedZero && (command.AmountMinorUnits != 0 || command.SourceDocumentCount != 0 ||
            command.FirstSourceReference is not null || command.LastSourceReference is not null || command.SourceEventReference is not null))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "An Annex E-1 zero attestation cannot carry recorded source values."));
        if (command.FactStatus == AnnexE1FactStatuses.Recorded &&
            command.FactType is not (AnnexE1FactTypes.ManualSiOrNetIncome or AnnexE1FactTypes.SalesOverrunOverflowNetIncome))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.UnsupportedPrivilege, SafeMessage: "The bounded Annex E-1 profile permits only explicit zero evidence for this unresolved classification."));
        if (command.FactStatus == AnnexE1FactStatuses.Recorded &&
            (command.AmountMinorUnits == 0 || command.SourceDocumentCount == 0 ||
             string.IsNullOrWhiteSpace(command.FirstSourceReference) || string.IsNullOrWhiteSpace(command.LastSourceReference)))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "A recorded Annex E-1 accounting fact requires positive source value, count, and bounded source references."));
        if (command.FactStatus == AnnexE1FactStatuses.Recorded && command.FactType == AnnexE1FactTypes.SalesOverrunOverflowNetIncome &&
            string.IsNullOrWhiteSpace(command.SourceEventReference))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "A recorded accumulated-sales-capacity fact requires its governed event reference."));
        if ((command.SupersedesFactId is null) != string.IsNullOrWhiteSpace(command.CorrectionReason))
            return Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "An Annex E-1 fact correction requires both predecessor and controlled reason."));

        return repository.RecordFactAsync(command with
        {
            OperationKey = command.OperationKey.Trim(), CurrencyCode = command.CurrencyCode.Trim(), FactType = command.FactType.Trim(),
            FactStatus = command.FactStatus.Trim(), ApprovalReference = command.ApprovalReference.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(), ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim(), FirstSourceReference = command.FirstSourceReference?.Trim(),
            LastSourceReference = command.LastSourceReference?.Trim(), SourceEventReference = command.SourceEventReference?.Trim(),
            CorrectionReason = command.CorrectionReason?.Trim()
        }, cancellationToken);
    }

    public Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand command, CancellationToken cancellationToken = default)
    {
        var hasCorrectionData = command.SupersedesWorkbookId is not null ||
            command.CorrectionReason is not null ||
            command.CorrectionApprovalReference is not null;
        var hasCompleteCorrectionData = command.SupersedesWorkbookId is not null &&
            command.CorrectionReason is not null &&
            command.CorrectionApprovalReference is not null;

        if (!ValidReference(command.OperationKey, 200) || command.SitePosServerId == Guid.Empty || command.FiscalIdentityId == Guid.Empty ||
            !ValidCurrency(command.CurrencyCode) || command.CurrencyCode != "PHP" || command.CalendarYear is < 2000 or > 9999 ||
            command.CalendarMonth is < 1 or > 12 || command.Profile != AnnexE1Contract.Profile ||
            !ValidReference(command.RequestedByRef, 200) || !ValidReference(command.ServiceIdentityRef, 200) ||
            !ValidReference(command.CorrelationId, 200) || !OptionalReference(command.CorrectionReason, 100) ||
            !OptionalReference(command.CorrectionApprovalReference, 200) ||
            (hasCorrectionData && !hasCompleteCorrectionData))
            return Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "The Annex E-1 generation request is incomplete or invalid."));

        return repository.GenerateAsync(command with
        {
            OperationKey = command.OperationKey.Trim(), CurrencyCode = command.CurrencyCode.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(), ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim(), CorrectionReason = command.CorrectionReason?.Trim(),
            CorrectionApprovalReference = command.CorrectionApprovalReference?.Trim()
        }, cancellationToken);
    }

    public Task<AnnexE1WorkbookResult> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        id == Guid.Empty ? Task.FromResult(new AnnexE1WorkbookResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "An Annex E-1 reference is required.")) : repository.GetAsync(id, cancellationToken);

    public Task<AnnexE1ArtifactResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default) =>
        id == Guid.Empty ? Task.FromResult(new AnnexE1ArtifactResult(AnnexE1Outcome.InvalidRequest, SafeMessage: "An Annex E-1 reference is required.")) : repository.DownloadAsync(id, cancellationToken);

    private static bool ValidCurrency(string? value) => value is { Length: 3 } && value.All(character => character is >= 'A' and <= 'Z');
    private static bool ValidReference(string? value, int length) => !string.IsNullOrWhiteSpace(value) && value.Length <= length && !value.Contains('\r') && !value.Contains('\n');
    private static bool OptionalReference(string? value, int length) => value is null || ValidReference(value, length);
}
