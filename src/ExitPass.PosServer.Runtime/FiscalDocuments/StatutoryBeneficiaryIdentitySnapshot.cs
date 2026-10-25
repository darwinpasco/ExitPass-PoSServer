namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public static class StatutoryBeneficiaryCaptureChannels
{
    public const string AuthorizedSiteOperator = "OPERATOR_CONSOLE";
    public const string ManagementPlatform = "MANAGEMENT_PLATFORM";

    public static bool IsAuthoritative(string value) =>
        value is AuthorizedSiteOperator or ManagementPlatform;
}

public sealed record StatutoryBeneficiaryIdentityInput(
    string? BeneficiaryFullName,
    string? StatutoryIdType,
    string? StatutoryIdNumber,
    string? BeneficiaryTin,
    string? CapturedByActorRef,
    string? CapturedByChannel,
    DateTimeOffset? CapturedAt,
    string? LastUpdatedByActorRef,
    string? LastUpdatedByChannel,
    DateTimeOffset? LastUpdatedAt,
    string? ReviewedByActorRef,
    DateTimeOffset? ReviewedAt,
    string? ApprovalDecisionReference,
    string? CorrelationReference,
    IReadOnlyList<string>? UnknownFieldNames = null);

public sealed record StatutoryBeneficiaryIdentitySnapshot(
    string BeneficiaryFullName,
    string StatutoryIdType,
    string StatutoryIdNumber,
    string? BeneficiaryTin,
    string CapturedByActorRef,
    string CapturedByChannel,
    DateTimeOffset CapturedAt,
    string LastUpdatedByActorRef,
    string LastUpdatedByChannel,
    DateTimeOffset LastUpdatedAt,
    string ReviewedByActorRef,
    DateTimeOffset ReviewedAt,
    string ApprovalDecisionReference,
    string CorrelationReference);

public sealed record ProtectedStatutoryBeneficiaryIdentity(
    byte[] Envelope,
    string KeyReference,
    string KeyVersion,
    short EnvelopeFormatVersion);

public interface IStatutoryBeneficiaryIdentityProtector
{
    bool IsConfigured { get; }
    ProtectedStatutoryBeneficiaryIdentity Protect(
        Guid fiscalDocumentId,
        Guid statutoryDecisionId,
        StatutoryBeneficiaryIdentitySnapshot identity);
    StatutoryBeneficiaryIdentitySnapshot Unprotect(
        Guid fiscalDocumentId,
        Guid statutoryDecisionId,
        ProtectedStatutoryBeneficiaryIdentity protectedIdentity);
}

public sealed class UnavailableStatutoryBeneficiaryIdentityProtector : IStatutoryBeneficiaryIdentityProtector
{
    public bool IsConfigured => false;

    public ProtectedStatutoryBeneficiaryIdentity Protect(Guid fiscalDocumentId, Guid statutoryDecisionId, StatutoryBeneficiaryIdentitySnapshot identity) =>
        throw new InvalidOperationException("Statutory beneficiary identity protection is not configured.");

    public StatutoryBeneficiaryIdentitySnapshot Unprotect(Guid fiscalDocumentId, Guid statutoryDecisionId, ProtectedStatutoryBeneficiaryIdentity protectedIdentity) =>
        throw new InvalidOperationException("Statutory beneficiary identity protection is not configured.");
}
