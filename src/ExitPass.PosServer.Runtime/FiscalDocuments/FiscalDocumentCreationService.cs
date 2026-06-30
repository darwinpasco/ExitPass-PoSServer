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
        "payment_payload",
        "provider_callback",
        "card_number",
        "cvv",
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
            string.IsNullOrWhiteSpace(command.FiscalDocumentTypeCodeKey) ||
            command.SitePosServerId is null ||
            command.SitePosServerId == Guid.Empty ||
            command.FiscalDocumentTypeCodeId is null ||
            command.FiscalDocumentTypeCodeId == Guid.Empty ||
            command.FiscalDocumentStatusCodeId is null ||
            command.FiscalDocumentStatusCodeId == Guid.Empty)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                "Fiscal document request is missing required local fiscal schema context: sitePosServerRef, fiscalDocumentTypeCodeKey, sitePosServerId, fiscalDocumentTypeCodeId, and fiscalDocumentStatusCodeId.");
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

        var documentLinks = command.DocumentLinks ?? Array.Empty<FiscalDocumentLinkInput>();
        foreach (var documentLink in documentLinks)
        {
            if (documentLink.TargetFiscalDocumentId == Guid.Empty ||
                documentLink.LinkTypeCodeId == Guid.Empty ||
                IsBlankOptionalReference(documentLink.LinkReasonText) ||
                IsBlankOptionalReference(documentLink.CreatedByRef))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                    "Fiscal document links require a target fiscal document, link type, and non-blank optional reference fields.");
            }
        }

        var documentLines = command.DocumentLines ?? Array.Empty<FiscalDocumentLineInput>();
        if (documentLines.Count == 0)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                "Fiscal document creation requires at least one fiscal line.");
        }

        var lineSequences = new HashSet<int>();
        foreach (var documentLine in documentLines)
        {
            if (!lineSequences.Add(documentLine.LineSequence))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                    "Fiscal document line sequence values must be unique within the document.");
            }

            if (!IsValidDocumentLine(documentLine))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest,
                    "Fiscal document lines require positive sequence and quantity, required type and description, nonnegative amounts, valid currency, and consistent net amount.");
            }
        }

        var tenders = command.Tenders ?? Array.Empty<FiscalTenderInput>();
        if (tenders.Count == 0)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.MissingFiscalTender,
                "Fiscal document creation requires at least one fiscal tender allocation.");
        }

        if (ContainsSensitiveEvidence(tenders))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveTenderPayloadNotAllowed,
                "Fiscal tenders accept references only and must not receive raw payment credential, token, secret, provider callback, or payment payload values.");
        }

        var payableBasisCurrency = command.PayableBasis.CurrencyCode.Trim().ToUpperInvariant();
        foreach (var tender in tenders)
        {
            if (!IsValidTender(tender, payableBasisCurrency))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidFiscalTender,
                    "Fiscal tenders require a tender type, positive amount, matching currency, and non-blank optional reference fields.");
            }
        }

        var draft = new FiscalDocumentDraft(
            Guid.NewGuid(),
            command.SitePosServerId.Value,
            command.ChannelTerminalId,
            command.FiscalDocumentTypeCodeId.Value,
            command.FiscalDocumentStatusCodeId.Value,
            command.SitePosServerRef.Trim(),
            command.FiscalDocumentTypeCodeKey.Trim(),
            command.PayableBasis.PayableBasisRef.Trim(),
            command.PayableBasis.UpstreamFinalityRef.Trim(),
            command.PayableBasis.CurrencyCode.Trim().ToUpperInvariant(),
            command.PayableBasis.PayableAmountMinorUnits,
            command.BusinessDayDate,
            NormalizeOptionalReference(command.CentralPmsParkingSessionRef),
            NormalizeOptionalReference(command.CentralPmsPaymentAttemptRef),
            NormalizeOptionalReference(command.CentralPmsPaymentConfirmationRef),
            NormalizeOptionalReference(command.PaymentFinalityRef) ?? command.PayableBasis.UpstreamFinalityRef.Trim(),
            NormalizeOptionalReference(command.VendorAckRef),
            documentLinks.Select(NormalizeDocumentLink).ToArray(),
            documentLines.Select(NormalizeDocumentLine).OrderBy(line => line.LineSequence).ToArray(),
            tenders.Select(NormalizeTender).ToArray(),
            discountReferences.ToArray());

        var persistedDraft = await repository.CreateAsync(draft, cancellationToken).ConfigureAwait(false);
        return FiscalDocumentCreationResult.Success(persistedDraft);
    }

    private static string? NormalizeOptionalReference(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsBlankOptionalReference(string? value) =>
        value is not null && string.IsNullOrWhiteSpace(value);

    private static FiscalDocumentLinkInput NormalizeDocumentLink(FiscalDocumentLinkInput link) =>
        link with
        {
            LinkReasonText = NormalizeOptionalReference(link.LinkReasonText),
            CreatedByRef = NormalizeOptionalReference(link.CreatedByRef)
        };

    private static FiscalDocumentLineInput NormalizeDocumentLine(FiscalDocumentLineInput line) =>
        line with
        {
            Description = line.Description.Trim(),
            CurrencyCode = line.CurrencyCode.Trim().ToUpperInvariant(),
            SourceRef = NormalizeOptionalReference(line.SourceRef)
        };

    private static bool IsValidDocumentLine(FiscalDocumentLineInput line)
    {
        if (string.IsNullOrWhiteSpace(line.Description) ||
            string.IsNullOrWhiteSpace(line.CurrencyCode))
        {
            return false;
        }

        var normalizedCurrency = line.CurrencyCode.Trim().ToUpperInvariant();
        var expectedNetAmount = line.GrossAmountMinorUnits - line.DiscountAmountMinorUnits + line.TaxAmountMinorUnits;

        return line.LineSequence > 0 &&
            line.LineTypeCodeId != Guid.Empty &&
            (line.LineStatusCodeId is null || line.LineStatusCodeId != Guid.Empty) &&
            line.Quantity > 0 &&
            line.UnitAmountMinorUnits >= 0 &&
            line.GrossAmountMinorUnits >= 0 &&
            line.DiscountAmountMinorUnits >= 0 &&
            line.TaxAmountMinorUnits >= 0 &&
            line.NetAmountMinorUnits >= 0 &&
            line.NetAmountMinorUnits == expectedNetAmount &&
            normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsAsciiLetterUpper) &&
            !IsBlankOptionalReference(line.SourceRef) &&
            HasValidContext(line.LineContext);
    }

    private static FiscalTenderInput NormalizeTender(FiscalTenderInput tender) =>
        tender with
        {
            CurrencyCode = tender.CurrencyCode.Trim().ToUpperInvariant(),
            CentralPmsPaymentAttemptRef = NormalizeOptionalReference(tender.CentralPmsPaymentAttemptRef),
            CentralPmsPaymentConfirmationRef = NormalizeOptionalReference(tender.CentralPmsPaymentConfirmationRef),
            PaymentFinalityRef = NormalizeOptionalReference(tender.PaymentFinalityRef),
            ProviderRef = NormalizeOptionalReference(tender.ProviderRef)
        };

    private static bool IsValidTender(FiscalTenderInput tender, string payableBasisCurrency)
    {
        if (string.IsNullOrWhiteSpace(tender.CurrencyCode))
        {
            return false;
        }

        var normalizedCurrency = tender.CurrencyCode.Trim().ToUpperInvariant();

        return tender.TenderTypeCodeId != Guid.Empty &&
            tender.AmountMinorUnits > 0 &&
            normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsAsciiLetterUpper) &&
            normalizedCurrency == payableBasisCurrency &&
            !IsBlankOptionalReference(tender.CentralPmsPaymentAttemptRef) &&
            !IsBlankOptionalReference(tender.CentralPmsPaymentConfirmationRef) &&
            !IsBlankOptionalReference(tender.PaymentFinalityRef) &&
            !IsBlankOptionalReference(tender.ProviderRef) &&
            HasValidContext(tender.TenderContext);
    }

    private static bool HasValidContext(IReadOnlyDictionary<string, string>? context)
    {
        if (context is null)
        {
            return true;
        }

        return context.All(entry =>
            !string.IsNullOrWhiteSpace(entry.Key) &&
            !string.IsNullOrWhiteSpace(entry.Value));
    }

    private static bool ContainsSensitiveEvidence(FiscalDocumentCreationCommand command)
    {
        return ContainsSensitiveEvidence(command.ReferenceContext) ||
            ContainsSensitiveEvidence(command.PayableBasis?.ReferenceContext) ||
            ContainsSensitiveEvidence(command.DocumentLinks) ||
            ContainsSensitiveEvidence(command.DocumentLines) ||
            (command.PayableBasis?.DiscountReferences?.Any(discount =>
                ContainsSensitiveEvidence(discount.ReferenceContext)) ?? false);
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalTenderInput>? tenders)
    {
        if (tenders is null)
        {
            return false;
        }

        return tenders.Any(tender =>
            ContainsSensitiveEvidenceMarker(tender.CentralPmsPaymentAttemptRef) ||
            ContainsSensitiveEvidenceMarker(tender.CentralPmsPaymentConfirmationRef) ||
            ContainsSensitiveEvidenceMarker(tender.PaymentFinalityRef) ||
            ContainsSensitiveEvidenceMarker(tender.ProviderRef) ||
            ContainsSensitiveEvidence(tender.TenderContext));
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalDocumentLineInput>? documentLines)
    {
        if (documentLines is null)
        {
            return false;
        }

        return documentLines.Any(line =>
            ContainsSensitiveEvidenceMarker(line.Description) ||
            ContainsSensitiveEvidenceMarker(line.SourceRef) ||
            ContainsSensitiveEvidence(line.LineContext));
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalDocumentLinkInput>? documentLinks)
    {
        if (documentLinks is null)
        {
            return false;
        }

        return documentLinks.Any(link =>
            ContainsSensitiveEvidenceMarker(link.LinkReasonText) ||
            ContainsSensitiveEvidenceMarker(link.CreatedByRef));
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
