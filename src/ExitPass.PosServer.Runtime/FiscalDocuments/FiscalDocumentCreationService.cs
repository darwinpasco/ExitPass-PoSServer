using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentCreationService
{
    private static readonly HashSet<string> EntitlementTypes = new(StringComparer.Ordinal)
    {
        "SENIOR_CITIZEN",
        "PWD"
    };

    private static readonly HashSet<string> BenefitClassifications = new(StringComparer.Ordinal)
    {
        "VAT_EXEMPTION_ONLY",
        "STATUTORY_DISCOUNT_ONLY",
        "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT",
        "FREE_PARKING",
        "REDUCED_PARKING_RATE",
        "CAPPED_PARKING_FEE"
    };

    private static readonly HashSet<string> VatTreatments = new(StringComparer.Ordinal)
    {
        "VAT_EXEMPT",
        "VAT_EXCLUSIVE",
        "VAT_INCLUSIVE_NO_EXEMPTION",
        "NON_VAT",
        "ZERO_RATED",
        "NOT_APPLICABLE"
    };

    private static readonly HashSet<string> PolicyResolutionBases = new(StringComparer.Ordinal)
    {
        "NATIONAL_LAW",
        "LOCAL_ORDINANCE",
        "MIXED",
        "INTERNAL_POLICY_REFERENCE"
    };

    private static readonly HashSet<string> SourcePaymentChannels = new(StringComparer.Ordinal)
    {
        "WEBPAY",
        "ASSISTED_PAYMENT_TERMINAL",
        "OPERATOR_CONSOLE"
    };

    private static readonly string[] SensitiveEvidenceMarkers =
    [
        "raw_id",
        "senior_citizen_id",
        "pwd_id",
        "osca",
        "beneficiary_name",
        "beneficiary_address",
        "birth_date",
        "id_image",
        "identity_document",
        "evidence_payload",
        "evidence_image",
        "evidence_url",
        "object_storage",
        "signed_url",
        "reviewer",
        "approver",
        "representative",
        "caregiver",
        "companion",
        "driver_identity",
        "raw_ordinance",
        "ordinance_text",
        "ocr",
        "authorization",
        "bearer",
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

        var completionBasis = NormalizeRequiredCode(command.CompletionBasis).ToUpperInvariant();
        var completionAuthorityRef = NormalizeOptionalReference(command.CompletionAuthorityRef);
        if (completionBasis == FiscalCompletionBasisCodes.PaymentFinality)
        {
            completionAuthorityRef ??= NormalizeOptionalReference(command.CentralPmsPaymentConfirmationRef)
                ?? NormalizeOptionalReference(command.PaymentFinalityRef)
                ?? command.PayableBasis.UpstreamFinalityRef.Trim();

            if (command.PayableBasis.PayableAmountMinorUnits <= 0)
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidCompletionAuthority,
                    "PAYMENT_FINALITY fiscal issuance requires a positive payable amount.");
            }
        }
        else if (completionBasis == FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality)
        {
            if (completionAuthorityRef is null ||
                command.PayableBasis.PayableAmountMinorUnits != 0 ||
                command.AppliedStatutoryFiscalFacts is null ||
                NormalizeOptionalReference(command.CentralPmsPaymentAttemptRef) is not null ||
                NormalizeOptionalReference(command.CentralPmsPaymentConfirmationRef) is not null ||
                NormalizeOptionalReference(command.PaymentFinalityRef) is not null)
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidCompletionAuthority,
                    "ZERO_PAYABLE_STATUTORY_FINALITY requires complete statutory authority, zero payable, and no payment ancestry.");
            }
        }
        else
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.InvalidCompletionAuthority,
                "Fiscal document completion basis is unsupported.");
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

        var payableBasisCurrency = command.PayableBasis.CurrencyCode.Trim().ToUpperInvariant();
        var tenders = command.Tenders ?? Array.Empty<FiscalTenderInput>();
        if (completionBasis == FiscalCompletionBasisCodes.PaymentFinality && tenders.Count == 0)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.MissingFiscalTender,
                "Fiscal document creation requires at least one fiscal tender allocation.");
        }

        if (completionBasis == FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality && tenders.Count != 0)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.InvalidCompletionAuthority,
                "ZERO_PAYABLE_STATUTORY_FINALITY must not contain a monetary tender.");
        }

        if (ContainsSensitiveEvidence(tenders))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveTenderPayloadNotAllowed,
                "Fiscal tenders accept references only and must not receive raw payment credential, token, secret, provider callback, or payment payload values.");
        }

        foreach (var tender in tenders)
        {
            if (!IsValidTender(tender, payableBasisCurrency))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidFiscalTender,
                    "Fiscal tenders require a tender type, positive amount, matching currency, and non-blank optional reference fields.");
            }
        }

        var taxDetails = command.TaxDetails ?? Array.Empty<FiscalTaxDetailInput>();
        if (ContainsSensitiveEvidence(taxDetails))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveTaxDetailPayloadNotAllowed,
                "Fiscal tax details accept references only and must not receive raw evidence, credential, payment payload, token, secret, or provider callback values.");
        }

        foreach (var taxDetail in taxDetails)
        {
            if (!IsValidTaxDetail(taxDetail, payableBasisCurrency, lineSequences))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidFiscalTaxDetail,
                    "Fiscal tax details require tax type and classification codes, nonnegative amounts, matching currency, valid line scope, and non-blank context entries.");
            }
        }

        var discountPrivilegeDetails = command.DiscountPrivilegeDetails ?? Array.Empty<FiscalDiscountPrivilegeDetailInput>();
        if (ContainsSensitiveEvidence(discountPrivilegeDetails))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveDiscountPrivilegePayloadNotAllowed,
                "Fiscal discount/privilege details accept references only and must not receive raw evidence, credential, payment payload, token, secret, or provider callback values.");
        }

        foreach (var discountPrivilegeDetail in discountPrivilegeDetails)
        {
            if (!IsValidDiscountPrivilegeDetail(discountPrivilegeDetail, payableBasisCurrency, lineSequences))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidFiscalDiscountPrivilegeDetail,
                    "Fiscal discount/privilege details require a type code, nonnegative bounded amounts, matching currency, valid line scope, and non-blank optional references/context entries.");
            }
        }

        var totals = command.Totals ?? Array.Empty<FiscalTotalInput>();
        if (ContainsSensitiveEvidence(totals))
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.SensitiveTotalPayloadNotAllowed,
                "Fiscal totals accept references only and must not receive raw evidence, credential, payment payload, token, secret, or provider callback values.");
        }

        var totalTypeIds = new HashSet<Guid>();
        foreach (var total in totals)
        {
            if (!totalTypeIds.Add(total.TotalTypeCodeId) ||
                !IsValidTotal(total, payableBasisCurrency))
            {
                return FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidFiscalTotal,
                    "Fiscal totals require a unique total type code, nonnegative amount, matching currency, and non-blank context entries.");
            }
        }

        var statutoryValidation = NormalizeAppliedStatutoryFiscalFacts(
            command,
            payableBasisCurrency,
            tenders,
            taxDetails,
            discountPrivilegeDetails,
            totals);
        if (statutoryValidation.Failure is not null)
        {
            return statutoryValidation.Failure;
        }

        var customerInformation = NormalizeInvoiceCustomerInformation(
            command.InvoiceCustomerInformation,
            statutoryValidation.Snapshot is not null);
        if (customerInformation.Failure is not null)
        {
            return customerInformation.Failure;
        }

        var draft = new FiscalDocumentDraft(
            Guid.NewGuid(),
            command.SitePosServerId.Value,
            command.SiteId,
            command.ChannelTerminalId,
            command.FiscalDocumentTypeCodeId.Value,
            command.FiscalDocumentStatusCodeId.Value,
            command.SitePosServerRef.Trim(),
            command.FiscalDocumentTypeCodeKey.Trim(),
            NormalizeOptionalReference(command.RuntimeTerminalRef),
            command.PayableBasis.PayableBasisRef.Trim(),
            command.PayableBasis.UpstreamFinalityRef.Trim(),
            command.PayableBasis.CurrencyCode.Trim().ToUpperInvariant(),
            command.PayableBasis.PayableAmountMinorUnits,
            command.BusinessDayDate,
            NormalizeOptionalReference(command.CentralPmsParkingSessionRef),
            NormalizeOptionalReference(command.CentralPmsPaymentAttemptRef),
            NormalizeOptionalReference(command.CentralPmsPaymentConfirmationRef),
            completionBasis == FiscalCompletionBasisCodes.PaymentFinality
                ? NormalizeOptionalReference(command.PaymentFinalityRef) ?? command.PayableBasis.UpstreamFinalityRef.Trim()
                : null,
            NormalizeOptionalReference(command.VendorAckRef),
            documentLinks.Select(NormalizeDocumentLink).ToArray(),
            documentLines.Select(NormalizeDocumentLine).OrderBy(line => line.LineSequence).ToArray(),
            tenders.Select(NormalizeTender).ToArray(),
            taxDetails.Select(NormalizeTaxDetail).OrderBy(tax => tax.LineSequence ?? int.MaxValue).ThenBy(tax => tax.TaxTypeCodeId).ToArray(),
            discountPrivilegeDetails
                .Select(NormalizeDiscountPrivilegeDetail)
                .OrderBy(discount => discount.LineSequence ?? int.MaxValue)
                .ThenBy(discount => discount.DiscountPrivilegeTypeCodeId)
                .ToArray(),
            totals.Select(NormalizeTotal).OrderBy(total => total.TotalTypeCodeId).ToArray(),
            discountReferences.ToArray(),
            AppliedStatutoryFiscalFacts: statutoryValidation.Snapshot,
            CompletionBasis: completionBasis,
            CompletionAuthorityRef: completionAuthorityRef,
            InvoiceCustomerInformation: customerInformation.Snapshot);

        try
        {
            var idempotency = FiscalIssuanceIdempotencyResolver.Resolve(command);
            var persistenceResult = await repository.CreateAsync(draft, idempotency, cancellationToken).ConfigureAwait(false);

            return persistenceResult.Outcome == FiscalDocumentPersistenceOutcome.Replayed
                ? FiscalDocumentCreationResult.Replay(persistenceResult.Draft)
                : FiscalDocumentCreationResult.Success(persistenceResult.Draft);
        }
        catch (FiscalDocumentIdempotencyConflictException ex)
        {
            return FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.IdempotencyConflict,
                ex.Message);
        }
        catch (FiscalDocumentFiscalContextException ex)
        {
            return FiscalDocumentCreationResult.Failure(
                ex.ErrorCode,
                ex.Message);
        }
        catch (FiscalCloseBoundaryException ex)
        {
            return FiscalDocumentCreationResult.Failure(
                ex.ErrorCode switch
                {
                    FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable => FiscalDocumentCreationErrorCode.ReportingPeriodUnavailable,
                    FiscalCloseBoundaryErrorCode.ReportingPeriodAmbiguous => FiscalDocumentCreationErrorCode.ReportingPeriodAmbiguous,
                    FiscalCloseBoundaryErrorCode.ReportingPeriodClosed => FiscalDocumentCreationErrorCode.ReportingPeriodClosed,
                    FiscalCloseBoundaryErrorCode.ReportingPeriodAssignmentMismatch => FiscalDocumentCreationErrorCode.ReportingPeriodAssignmentMismatch,
                    FiscalCloseBoundaryErrorCode.LockTimeout => FiscalDocumentCreationErrorCode.FiscalCloseBoundaryLockTimeout,
                    _ => FiscalDocumentCreationErrorCode.FiscalCloseBoundaryRetryableConcurrencyFailure
                },
                ex.Message);
        }
    }

    private static (FiscalDocumentCreationResult? Failure, InvoiceCustomerInformationSnapshot? Snapshot)
        NormalizeInvoiceCustomerInformation(
            InvoiceCustomerInformationInput? value,
            bool approvedStatutoryBenefit)
    {
        if (value is null)
        {
            return (null, null);
        }

        var customerName = NormalizeOptionalReference(value.CustomerName);
        var address = NormalizeOptionalReference(value.Address);
        var tin = NormalizeOptionalReference(value.Tin);
        var businessStyle = NormalizeOptionalReference(value.BusinessStyle);
        var statutoryIdNumber = approvedStatutoryBenefit
            ? NormalizeOptionalReference(value.StatutoryIdNumber)
            : null;

        if (customerName?.Length > 160 ||
            address?.Length > 300 ||
            tin?.Length > 40 ||
            businessStyle?.Length > 160 ||
            statutoryIdNumber?.Length > 80)
        {
            return (
                FiscalDocumentCreationResult.Failure(
                    FiscalDocumentCreationErrorCode.InvalidInvoiceCustomerInformation,
                    "Invoice customer information exceeds an allowed field length."),
                null);
        }

        return (
            null,
            new InvoiceCustomerInformationSnapshot(
                customerName,
                address,
                tin,
                businessStyle,
                statutoryIdNumber));
    }

    private static (FiscalDocumentCreationResult? Failure, AppliedStatutoryFiscalFactsSnapshot? Snapshot)
        NormalizeAppliedStatutoryFiscalFacts(
            FiscalDocumentCreationCommand command,
            string payableBasisCurrency,
            IReadOnlyList<FiscalTenderInput> tenders,
            IReadOnlyList<FiscalTaxDetailInput> taxDetails,
            IReadOnlyList<FiscalDiscountPrivilegeDetailInput> discountPrivilegeDetails,
            IReadOnlyList<FiscalTotalInput> totals)
    {
        var facts = command.AppliedStatutoryFiscalFacts;
        if (facts is null)
        {
            return (null, null);
        }

        if ((facts.UnknownFieldNames?.Count ?? 0) > 0 ||
            (facts.PolicyReference?.UnknownFieldNames?.Count ?? 0) > 0 ||
            ContainsSensitiveEvidence(facts) ||
            ContainsSensitiveEvidence(facts.PolicyReference))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryProhibitedPrivacyField,
                "Applied statutory fiscal facts contain unsupported, prohibited, or privacy-sensitive fields."), null);
        }

        if (facts.StatutoryDiscountDecisionCommandId is null ||
            facts.StatutoryDiscountDecisionCommandId == Guid.Empty ||
            facts.StatutoryRequestReference is null ||
            facts.StatutoryRequestReference == Guid.Empty ||
            facts.StatutoryPayableBasisApplicationCommandId is null ||
            facts.StatutoryPayableBasisApplicationCommandId == Guid.Empty ||
            facts.StatutoryValidationId is null ||
            facts.StatutoryValidationId == Guid.Empty ||
            facts.ParkingSessionId is null ||
            facts.ParkingSessionId == Guid.Empty ||
            facts.SiteId is null ||
            facts.SiteId == Guid.Empty ||
            facts.SiteGroupId is null ||
            facts.SiteGroupId == Guid.Empty ||
            facts.PolicyReference is null ||
            facts.OriginalTariffSnapshotId is null ||
            facts.OriginalTariffSnapshotId == Guid.Empty ||
            facts.AppliedTariffSnapshotId is null ||
            facts.AppliedTariffSnapshotId == Guid.Empty ||
            facts.OriginalAmountMinorUnits is null ||
            facts.VatExclusiveBasisAmountMinorUnits is null ||
            facts.VatAmountMinorUnits is null ||
            facts.StatutoryDiscountAmountMinorUnits is null ||
            facts.FinalPayableAmountMinorUnits is null ||
            facts.AppliedAt is null)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryFactsIncomplete,
                "Applied statutory fiscal facts require a complete final Central PMS payment-time application snapshot."), null);
        }

        var entitlementType = NormalizeRequiredCode(facts.EntitlementType);
        if (!EntitlementTypes.Contains(entitlementType))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedEntitlementType,
                "Applied statutory fiscal facts use an unsupported entitlement classification."), null);
        }

        var benefitClassification = NormalizeRequiredCode(facts.BenefitClassification);
        if (!BenefitClassifications.Contains(benefitClassification))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedBenefitClassification,
                "Applied statutory fiscal facts use an unsupported benefit classification."), null);
        }

        var vatTreatment = NormalizeRequiredCode(facts.VatTreatment);
        if (!VatTreatments.Contains(vatTreatment))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedVatTreatment,
                "Applied statutory fiscal facts use an unsupported VAT treatment classification."), null);
        }

        var sourcePaymentChannel = NormalizeRequiredCode(facts.SourcePaymentChannel);
        if (!SourcePaymentChannels.Contains(sourcePaymentChannel))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedSourcePaymentChannel,
                "Applied statutory fiscal facts use an unsupported source payment channel."), null);
        }

        var policyResolutionBasis = NormalizeRequiredCode(facts.PolicyReference.ResolutionBasis);
        if (!PolicyResolutionBases.Contains(policyResolutionBasis))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedPolicyResolutionBasis,
                "Applied statutory fiscal facts use an unsupported policy resolution basis."), null);
        }

        var policyReference = new AppliedStatutoryPolicyReferenceSnapshot(
            policyResolutionBasis,
            facts.PolicyReference.AppliedPolicyReferenceId,
            NormalizeOptionalReference(facts.PolicyReference.PolicyCode),
            facts.PolicyReference.PolicyVersionId,
            NormalizeOptionalReference(facts.PolicyReference.NationalLawReference),
            NormalizeOptionalReference(facts.PolicyReference.OrdinanceReference));

        if (policyReference.AppliedPolicyReferenceId is null &&
            policyReference.PolicyCode is null &&
            policyReference.PolicyVersionId is null &&
            policyReference.NationalLawReference is null &&
            policyReference.OrdinanceReference is null)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryFactsIncomplete,
                "Applied statutory fiscal facts require a safe policy authority reference."), null);
        }

        var currency = NormalizeCurrency(facts.Currency);
        if (currency is null || currency != payableBasisCurrency)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryCurrencyMismatch,
                "Applied statutory fiscal facts currency must match the fiscal payable basis."), null);
        }

        var originalAmount = facts.OriginalAmountMinorUnits.Value;
        var vatExclusiveBasis = facts.VatExclusiveBasisAmountMinorUnits.Value;
        var vatAmount = facts.VatAmountMinorUnits.Value;
        var statutoryDiscountAmount = facts.StatutoryDiscountAmountMinorUnits.Value;
        var finalPayableAmount = facts.FinalPayableAmountMinorUnits.Value;

        if (originalAmount < 0 ||
            vatExclusiveBasis < 0 ||
            vatAmount < 0 ||
            statutoryDiscountAmount < 0 ||
            finalPayableAmount < 0 ||
            facts.AppliedTariffSnapshotId == facts.OriginalTariffSnapshotId)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryFactsNotFinal,
                "Applied statutory fiscal facts must represent a final nonnegative payment-time application snapshot."), null);
        }

        if (!string.Equals(
                NormalizeOptionalReference(command.CentralPmsParkingSessionRef),
                facts.ParkingSessionId.Value.ToString("D"),
                StringComparison.OrdinalIgnoreCase) ||
            command.SiteId is not null && command.SiteId != facts.SiteId)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryFactsNotFinal,
                "Applied statutory fiscal facts must match the fiscalized parking and Site scope."), null);
        }

        var zeroPayableStatutoryCompletion = string.Equals(
            NormalizeRequiredCode(command.CompletionBasis),
            FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality,
            StringComparison.OrdinalIgnoreCase);

        if (sourcePaymentChannel == "ASSISTED_PAYMENT_TERMINAL" &&
            !zeroPayableStatutoryCompletion &&
            facts.TerminalCashTenderId is null)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryFactsIncomplete,
                "Applied statutory fiscal facts require terminal cash tender linkage for assisted payment terminal issuance."), null);
        }

        var tenderTotal = tenders.Sum(tender => tender.AmountMinorUnits);
        var totalMax = totals.Count == 0 ? 0 : totals.Max(total => total.AmountMinorUnits);
        var taxTotal = taxDetails.Sum(tax => tax.TaxAmountMinorUnits);
        var discountTotal = discountPrivilegeDetails.Sum(discount => discount.DiscountAmountMinorUnits);
        var vatPrivilegeTotal = discountPrivilegeDetails.Sum(discount => discount.VatPrivilegeAmountMinorUnits);

        if (finalPayableAmount != command.PayableBasis!.PayableAmountMinorUnits ||
            finalPayableAmount != tenderTotal ||
            finalPayableAmount != totalMax ||
            vatAmount != taxTotal ||
            statutoryDiscountAmount > discountTotal + vatPrivilegeTotal ||
            finalPayableAmount + statutoryDiscountAmount > originalAmount)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryTotalMismatch,
                "Applied statutory fiscal facts must reconcile with fiscal payable basis, tenders, taxes, discounts, and totals."), null);
        }

        if (benefitClassification == "FREE_PARKING" && finalPayableAmount != 0)
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.AppliedStatutoryTotalMismatch,
                "Free parking statutory facts must have a zero final payable amount."), null);
        }

        if (zeroPayableStatutoryCompletion &&
            (benefitClassification != "FREE_PARKING" ||
             originalAmount <= 0 ||
             finalPayableAmount != 0 ||
             vatExclusiveBasis + vatAmount != originalAmount ||
             statutoryDiscountAmount != vatExclusiveBasis ||
             !string.Equals(
                 command.AppliedStatutoryFiscalFacts!.AppliedTariffSnapshotId!.Value.ToString("D"),
                 command.PayableBasis!.PayableBasisRef.Trim(),
                 StringComparison.OrdinalIgnoreCase)))
        {
            return (FiscalDocumentCreationResult.Failure(
                FiscalDocumentCreationErrorCode.InvalidCompletionAuthority,
                "ZERO_PAYABLE_STATUTORY_FINALITY fiscal facts do not reconcile with the canonical applied payable basis."), null);
        }

        return (null, new AppliedStatutoryFiscalFactsSnapshot(
            facts.StatutoryDiscountDecisionCommandId.Value,
            facts.StatutoryRequestReference.Value,
            facts.StatutoryPayableBasisApplicationCommandId.Value,
            facts.StatutoryValidationId.Value,
            facts.ParkingSessionId.Value,
            facts.SiteId.Value,
            facts.SiteGroupId.Value,
            entitlementType,
            benefitClassification,
            policyReference,
            facts.OriginalTariffSnapshotId.Value,
            facts.AppliedTariffSnapshotId.Value,
            originalAmount,
            vatExclusiveBasis,
            vatAmount,
            vatTreatment,
            statutoryDiscountAmount,
            finalPayableAmount,
            currency,
            facts.AppliedAt.Value.ToUniversalTime(),
            sourcePaymentChannel,
            facts.TerminalCashTenderId,
            DateTimeOffset.UtcNow));
    }

    private static string NormalizeRequiredCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string? NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var currency = value.Trim().ToUpperInvariant();
        return currency.Length == 3 && currency.All(char.IsAsciiLetterUpper)
            ? currency
            : null;
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

    private static FiscalTaxDetailInput NormalizeTaxDetail(FiscalTaxDetailInput taxDetail) =>
        taxDetail with
        {
            CurrencyCode = taxDetail.CurrencyCode.Trim().ToUpperInvariant()
        };

    private static bool IsValidTaxDetail(
        FiscalTaxDetailInput taxDetail,
        string payableBasisCurrency,
        IReadOnlySet<int> lineSequences)
    {
        if (string.IsNullOrWhiteSpace(taxDetail.CurrencyCode))
        {
            return false;
        }

        var normalizedCurrency = taxDetail.CurrencyCode.Trim().ToUpperInvariant();

        return taxDetail.TaxTypeCodeId != Guid.Empty &&
            taxDetail.TaxClassificationCodeId != Guid.Empty &&
            taxDetail.TaxableAmountMinorUnits >= 0 &&
            taxDetail.TaxAmountMinorUnits >= 0 &&
            (taxDetail.TaxRate is null || taxDetail.TaxRate >= 0) &&
            normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsAsciiLetterUpper) &&
            normalizedCurrency == payableBasisCurrency &&
            (taxDetail.LineSequence is null || lineSequences.Contains(taxDetail.LineSequence.Value)) &&
            HasValidContext(taxDetail.TaxContext);
    }

    private static FiscalDiscountPrivilegeDetailInput NormalizeDiscountPrivilegeDetail(
        FiscalDiscountPrivilegeDetailInput discountPrivilegeDetail) =>
        discountPrivilegeDetail with
        {
            CurrencyCode = discountPrivilegeDetail.CurrencyCode.Trim().ToUpperInvariant(),
            BeneficiaryRef = NormalizeOptionalReference(discountPrivilegeDetail.BeneficiaryRef),
            EvidenceRef = NormalizeOptionalReference(discountPrivilegeDetail.EvidenceRef),
            ApprovalRef = NormalizeOptionalReference(discountPrivilegeDetail.ApprovalRef)
        };

    private static bool IsValidDiscountPrivilegeDetail(
        FiscalDiscountPrivilegeDetailInput discountPrivilegeDetail,
        string payableBasisCurrency,
        IReadOnlySet<int> lineSequences)
    {
        if (string.IsNullOrWhiteSpace(discountPrivilegeDetail.CurrencyCode))
        {
            return false;
        }

        var normalizedCurrency = discountPrivilegeDetail.CurrencyCode.Trim().ToUpperInvariant();

        return discountPrivilegeDetail.DiscountPrivilegeTypeCodeId != Guid.Empty &&
            discountPrivilegeDetail.BasisAmountMinorUnits >= 0 &&
            discountPrivilegeDetail.DiscountAmountMinorUnits >= 0 &&
            discountPrivilegeDetail.VatPrivilegeAmountMinorUnits >= 0 &&
            discountPrivilegeDetail.DiscountAmountMinorUnits <= discountPrivilegeDetail.BasisAmountMinorUnits &&
            discountPrivilegeDetail.VatPrivilegeAmountMinorUnits <= discountPrivilegeDetail.BasisAmountMinorUnits &&
            normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsAsciiLetterUpper) &&
            normalizedCurrency == payableBasisCurrency &&
            (discountPrivilegeDetail.LineSequence is null || lineSequences.Contains(discountPrivilegeDetail.LineSequence.Value)) &&
            !IsBlankOptionalReference(discountPrivilegeDetail.BeneficiaryRef) &&
            !IsBlankOptionalReference(discountPrivilegeDetail.EvidenceRef) &&
            !IsBlankOptionalReference(discountPrivilegeDetail.ApprovalRef) &&
            HasValidContext(discountPrivilegeDetail.DiscountPrivilegeContext);
    }

    private static FiscalTotalInput NormalizeTotal(FiscalTotalInput total) =>
        total with
        {
            CurrencyCode = total.CurrencyCode.Trim().ToUpperInvariant()
        };

    private static bool IsValidTotal(FiscalTotalInput total, string payableBasisCurrency)
    {
        if (string.IsNullOrWhiteSpace(total.CurrencyCode))
        {
            return false;
        }

        var normalizedCurrency = total.CurrencyCode.Trim().ToUpperInvariant();

        return total.TotalTypeCodeId != Guid.Empty &&
            total.AmountMinorUnits >= 0 &&
            normalizedCurrency.Length == 3 &&
            normalizedCurrency.All(char.IsAsciiLetterUpper) &&
            normalizedCurrency == payableBasisCurrency &&
            HasValidContext(total.TotalContext);
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

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalTaxDetailInput>? taxDetails)
    {
        if (taxDetails is null)
        {
            return false;
        }

        return taxDetails.Any(taxDetail => ContainsSensitiveEvidence(taxDetail.TaxContext));
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalDiscountPrivilegeDetailInput>? discountPrivilegeDetails)
    {
        if (discountPrivilegeDetails is null)
        {
            return false;
        }

        return discountPrivilegeDetails.Any(discountPrivilegeDetail =>
            ContainsSensitiveEvidenceMarker(discountPrivilegeDetail.BeneficiaryRef) ||
            ContainsSensitiveEvidenceMarker(discountPrivilegeDetail.EvidenceRef) ||
            ContainsSensitiveEvidenceMarker(discountPrivilegeDetail.ApprovalRef) ||
            ContainsSensitiveEvidence(discountPrivilegeDetail.DiscountPrivilegeContext));
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<FiscalTotalInput>? totals)
    {
        if (totals is null)
        {
            return false;
        }

        return totals.Any(total => ContainsSensitiveEvidence(total.TotalContext));
    }

    private static bool ContainsSensitiveEvidence(AppliedStatutoryFiscalFactsInput facts) =>
        ContainsSensitiveEvidenceMarker(facts.EntitlementType) ||
        ContainsSensitiveEvidenceMarker(facts.BenefitClassification) ||
        ContainsSensitiveEvidenceMarker(facts.VatTreatment) ||
        ContainsSensitiveEvidenceMarker(facts.Currency) ||
        ContainsSensitiveEvidenceMarker(facts.SourcePaymentChannel) ||
        ContainsSensitiveEvidence(facts.UnknownFieldNames);

    private static bool ContainsSensitiveEvidence(AppliedStatutoryPolicyReferenceInput? policyReference)
    {
        if (policyReference is null)
        {
            return false;
        }

        return ContainsSensitiveEvidenceMarker(policyReference.ResolutionBasis) ||
            ContainsSensitiveEvidenceMarker(policyReference.PolicyCode) ||
            ContainsSensitiveEvidenceMarker(policyReference.NationalLawReference) ||
            ContainsSensitiveEvidenceMarker(policyReference.OrdinanceReference) ||
            ContainsSensitiveEvidence(policyReference.UnknownFieldNames);
    }

    private static bool ContainsSensitiveEvidence(IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return false;
        }

        return values.Any(ContainsSensitiveEvidenceMarker);
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
