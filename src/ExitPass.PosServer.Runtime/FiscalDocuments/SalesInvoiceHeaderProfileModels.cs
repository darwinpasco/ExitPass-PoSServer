namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class SalesInvoiceHeaderProfileVersions
{
    public const string TemplateVersion = "digital-sales-invoice-json-v1";
    public const string PresentationVersion = "digital-sales-invoice-presentation-json-v1";
}

public static class SalesInvoiceHeaderProfileLifecycle
{
    public const string Draft = "DRAFT";
    public const string Approved = "APPROVED";
    public const string Retired = "RETIRED";
}

public sealed record FiscalIdentityProfile(
    Guid FiscalIdentityId,
    string RegisteredBusinessName,
    string RegisteredBusinessAddress,
    string Tin,
    string? TaxpayerClassification,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedByRef,
    string? UpdatedByRef);

public sealed record SalesInvoiceHeaderProfile(
    Guid SalesInvoiceHeaderProfileId,
    Guid FiscalIdentityId,
    Guid SiteId,
    Guid SitePosServerId,
    string ProfileVersion,
    string TemplateVersion,
    string PresentationVersion,
    string? PosSerialNumber,
    string? MachineIdentificationNumber,
    string? ParkingLocationDisplay,
    string? BirAccreditationNumber,
    DateOnly? BirAccreditationIssuedDate,
    DateOnly? BirAccreditationValidUntil,
    string? PtuNumber,
    DateOnly? PtuIssuedDate,
    string? SalesInvoiceLegalStatement,
    string? CustomerServiceFooter,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string LifecycleStatus,
    DateTimeOffset? ApprovedAt,
    string? ApprovedByRef,
    DateTimeOffset? RetiredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? CreatedByRef,
    string? UpdatedByRef,
    FiscalIdentityProfile? FiscalIdentity = null,
    bool HasBeenSnapshotted = false,
    string? SupplierDeveloperRegisteredName = null,
    string? SupplierDeveloperAddress = null,
    string? SupplierDeveloperTin = null);

public sealed record SalesInvoiceHeaderSnapshot(
    Guid FiscalIdentityId,
    Guid SalesInvoiceHeaderProfileId,
    string ProfileVersion,
    string RegisteredBusinessName,
    string RegisteredBusinessAddress,
    string Tin,
    string PosSerialNumber,
    string MachineIdentificationNumber,
    string ParkingLocationDisplay,
    string? TerminalId,
    string BirAccreditationNumber,
    DateOnly BirAccreditationIssuedDate,
    DateOnly BirAccreditationValidUntil,
    string PtuNumber,
    DateOnly PtuIssuedDate,
    string SalesInvoiceLegalStatement,
    string CustomerServiceFooter,
    string TemplateVersion,
    string PresentationVersion,
    DateTimeOffset EffectiveAt,
    DateTimeOffset SnapshotCreatedAt,
    string SupplierDeveloperRegisteredName,
    string SupplierDeveloperAddress,
    string SupplierDeveloperTin);

public sealed record SalesInvoiceHeaderProfileCompletenessResult(
    bool IsComplete,
    IReadOnlyList<string> FailureCodes)
{
    public static SalesInvoiceHeaderProfileCompletenessResult Complete() => new(true, []);

    public static SalesInvoiceHeaderProfileCompletenessResult Incomplete(IEnumerable<string> failureCodes) =>
        new(false, failureCodes.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
}

public enum SalesInvoiceHeaderProfileResolutionStatus
{
    Resolved = 0,
    NotFound = 1,
    Ambiguous = 2,
    Incomplete = 3,
    UnsupportedVersion = 4
}

public sealed record SalesInvoiceHeaderProfileResolutionResult(
    SalesInvoiceHeaderProfileResolutionStatus Status,
    SalesInvoiceHeaderProfile? Profile,
    SalesInvoiceHeaderProfileCompletenessResult Completeness)
{
    public static SalesInvoiceHeaderProfileResolutionResult Resolved(SalesInvoiceHeaderProfile profile) =>
        new(SalesInvoiceHeaderProfileResolutionStatus.Resolved, profile, SalesInvoiceHeaderProfileCompletenessResult.Complete());

    public static SalesInvoiceHeaderProfileResolutionResult NotFound() =>
        new(SalesInvoiceHeaderProfileResolutionStatus.NotFound, null, SalesInvoiceHeaderProfileCompletenessResult.Incomplete(["profile_not_found"]));

    public static SalesInvoiceHeaderProfileResolutionResult Ambiguous() =>
        new(SalesInvoiceHeaderProfileResolutionStatus.Ambiguous, null, SalesInvoiceHeaderProfileCompletenessResult.Incomplete(["multiple_effective_profiles"]));

    public static SalesInvoiceHeaderProfileResolutionResult Incomplete(
        SalesInvoiceHeaderProfile profile,
        SalesInvoiceHeaderProfileCompletenessResult completeness) =>
        new(SalesInvoiceHeaderProfileResolutionStatus.Incomplete, profile, completeness);
}

public sealed record SalesInvoiceHeaderProfileUsage(
    Guid SalesInvoiceHeaderProfileId,
    string? ProfileVersion,
    Guid? FiscalIdentityId,
    DateTimeOffset? FirstSnapshotCreatedAt,
    DateTimeOffset? LatestSnapshotCreatedAt,
    long FiscalDocumentCount,
    IReadOnlyList<Guid> SampleFiscalDocumentIds,
    bool DestructiveMutationBlocked);
