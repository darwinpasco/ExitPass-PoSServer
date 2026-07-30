namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record AppliedStatutoryFiscalFactsSnapshot(
    Guid StatutoryDiscountDecisionCommandId,
    Guid StatutoryRequestReference,
    Guid StatutoryPayableBasisApplicationCommandId,
    Guid StatutoryValidationId,
    Guid ParkingSessionId,
    Guid SiteId,
    Guid SiteGroupId,
    string EntitlementType,
    string BenefitClassification,
    AppliedStatutoryPolicyReferenceSnapshot PolicyReference,
    Guid OriginalTariffSnapshotId,
    Guid AppliedTariffSnapshotId,
    long OriginalAmountMinorUnits,
    long VatExclusiveBasisAmountMinorUnits,
    long VatAmountMinorUnits,
    string VatTreatment,
    long StatutoryDiscountAmountMinorUnits,
    long FinalPayableAmountMinorUnits,
    string Currency,
    DateTimeOffset AppliedAt,
    string SourcePaymentChannel,
    Guid? TerminalCashTenderId = null,
    DateTimeOffset? SnapshotCreatedAt = null);

public sealed record AppliedStatutoryPolicyReferenceSnapshot(
    string ResolutionBasis,
    Guid? AppliedPolicyReferenceId = null,
    string? PolicyCode = null,
    Guid? PolicyVersionId = null,
    string? NationalLawReference = null,
    string? OrdinanceReference = null);
