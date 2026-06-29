namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentCreationService
{
    private static readonly string[] SensitiveEvidenceMarkers =
    [
        "raw_id",
        "id_image",
        "identity_document",
        "evidence_payload",
        "evidence_image",
        "credential",
        "token",
        "secret"
    ];

    private readonly IFiscalDocumentRepository repository;

    public FiscalDocumentCreationService(IFiscalDocumentRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<FiscalDocumentCreationResult> CreateAsync(
        FiscalDocumentCreationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SitePosServerRef) ||
            string.IsNullOrWhiteSpace(command.FiscalDocumentTypeCodeKey))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                "Fiscal document request is missing required local fiscal context.");
        }

        if (command.PayableBasis is null ||
            string.IsNullOrWhiteSpace(command.PayableBasis.PayableBasisRef))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.MissingPayableBasis,
                "Fiscal document creation requires an upstream approved payable-basis reference.");
        }

        if (string.IsNullOrWhiteSpace(command.PayableBasis.UpstreamFinalityRef))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.MissingUpstreamFinalityReference,
                "Fiscal document creation requires an upstream payment/finality reference.");
        }

        if (ContainsSensitiveEvidence(command))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveEvidencePayloadNotAllowed,
                "POS Server accepts references only and must not receive raw statutory discount evidence payloads.");
        }

        var discountReferences = command.PayableBasis.DiscountReferences ?? Array.Empty<FiscalDiscountReferenceInput>();
        foreach (var discountReference in discountReferences)
        {
            if (discountReference.AppliesStatutoryDiscountTreatment &&
                discountReference.Status != FiscalDiscountReferenceStatus.Approved)
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.UnapprovedDiscountReference,
                    "POS Server cannot fiscalize statutory discount treatment without an approved upstream discount validation reference.");
            }
        }

        var draft = new FiscalDocumentDraft(
            Guid.NewGuid(),
            command.SitePosServerRef.Trim(),
            command.FiscalDocumentTypeCodeKey.Trim(),
            command.PayableBasis.PayableBasisRef.Trim(),
            command.PayableBasis.UpstreamFinalityRef.Trim(),
            command.PayableBasis.CurrencyCode.Trim().ToUpperInvariant(),
            command.PayableBasis.PayableAmountMinorUnits,
            NormalizeOptionalReference(command.CentralPmsPaymentAttemptRef),
            NormalizeOptionalReference(command.CentralPmsPaymentConfirmationRef),
            NormalizeOptionalReference(command.PaymentFinalityRef),
            NormalizeOptionalReference(command.VendorAckRef),
            discountReferences.ToArray());

        var persistedDraft = await repository.CreateAsync(draft, cancellationToken).ConfigureAwait(false);
        return FiscalDocumentCreationResult.Success(persistedDraft);
    }

    private static string? NormalizeOptionalReference(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ContainsSensitiveEvidence(FiscalDocumentCreationCommand command)
    {
        return ContainsSensitiveEvidence(command.ReferenceContext) ||
            ContainsSensitiveEvidence(command.PayableBasis?.ReferenceContext) ||
            (command.PayableBasis?.DiscountReferences?.Any(discount =>
                ContainsSensitiveEvidence(discount.ReferenceContext)) ?? false);
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyDictionary<string, string>? context)
    {
        if (context is null)
        {
            return false;
        }

        return context.Any(entry =>
            ContainsSensitiveEvidenceMarker(entry.Key) ||
            ContainsSensitiveEvidenceMarker(entry.Value));
    }

    private static bool ContainsSensitiveEvidenceMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return SensitiveEvidenceMarkers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
