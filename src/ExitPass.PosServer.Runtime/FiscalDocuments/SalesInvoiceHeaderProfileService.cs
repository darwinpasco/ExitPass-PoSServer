namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class SalesInvoiceHeaderProfileService
{
    public SalesInvoiceHeaderProfileCompletenessResult EvaluateCompleteness(
        SalesInvoiceHeaderProfile profile,
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset effectiveAt)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var failures = new List<string>();
        Require(!string.IsNullOrWhiteSpace(profile.FiscalIdentity?.RegisteredBusinessName), "registered_business_name_missing");
        Require(!string.IsNullOrWhiteSpace(profile.FiscalIdentity?.RegisteredBusinessAddress), "registered_business_address_missing");
        Require(!string.IsNullOrWhiteSpace(profile.FiscalIdentity?.Tin), "tin_missing");
        Require(!string.IsNullOrWhiteSpace(profile.PosSerialNumber), "pos_serial_number_missing");
        Require(!string.IsNullOrWhiteSpace(profile.MachineIdentificationNumber), "machine_identification_number_missing");
        Require(!string.IsNullOrWhiteSpace(profile.BirAccreditationNumber), "bir_accreditation_number_missing");
        Require(profile.BirAccreditationIssuedDate is not null, "bir_accreditation_issued_date_missing");
        Require(profile.BirAccreditationValidUntil is not null, "bir_accreditation_valid_until_missing");
        Require(!string.IsNullOrWhiteSpace(profile.PtuNumber), "ptu_number_missing");
        Require(profile.PtuIssuedDate is not null, "ptu_issued_date_missing");
        Require(!string.IsNullOrWhiteSpace(profile.SupplierDeveloperRegisteredName), "supplier_developer_registered_name_missing");
        Require(!string.IsNullOrWhiteSpace(profile.SupplierDeveloperAddress), "supplier_developer_address_missing");
        Require(!string.IsNullOrWhiteSpace(profile.SupplierDeveloperTin), "supplier_developer_tin_missing");
        Require(!string.IsNullOrWhiteSpace(profile.ParkingLocationDisplay), "parking_location_display_missing");
        Require(!string.IsNullOrWhiteSpace(profile.SalesInvoiceLegalStatement), "sales_invoice_legal_statement_missing");
        Require(!string.IsNullOrWhiteSpace(profile.CustomerServiceFooter), "customer_service_footer_missing");
        Require(
            string.Equals(profile.TemplateVersion, SalesInvoiceHeaderProfileVersions.TemplateVersion, StringComparison.Ordinal),
            "unsupported_template_version");
        Require(
            string.Equals(profile.PresentationVersion, SalesInvoiceHeaderProfileVersions.PresentationVersion, StringComparison.Ordinal),
            "unsupported_presentation_version");
        Require(
            string.Equals(profile.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal),
            "profile_not_approved");
        Require(profile.SiteId == siteId, "site_mismatch");
        Require(profile.SitePosServerId == sitePosServerId, "site_pos_server_mismatch");
        Require(profile.EffectiveFrom <= effectiveAt, "profile_not_effective_yet");
        Require(profile.EffectiveTo is null || profile.EffectiveTo > effectiveAt, "profile_expired");
        Require(profile.EffectiveTo is null || profile.EffectiveTo > profile.EffectiveFrom, "invalid_effective_window");
        Require(profile.RetiredAt is null, "profile_retired");

        if (profile.BirAccreditationIssuedDate is not null &&
            profile.BirAccreditationValidUntil is not null &&
            profile.BirAccreditationValidUntil.Value < profile.BirAccreditationIssuedDate.Value)
        {
            failures.Add("bir_accreditation_valid_until_before_issued_date");
        }

        return failures.Count == 0
            ? SalesInvoiceHeaderProfileCompletenessResult.Complete()
            : SalesInvoiceHeaderProfileCompletenessResult.Incomplete(failures);

        void Require(bool condition, string failureCode)
        {
            if (!condition)
            {
                failures.Add(failureCode);
            }
        }
    }

    public SalesInvoiceHeaderProfileResolutionResult ResolveEffective(
        IEnumerable<SalesInvoiceHeaderProfile> profiles,
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset effectiveAt)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var matches = profiles
            .Where(profile =>
                profile.SiteId == siteId &&
                profile.SitePosServerId == sitePosServerId &&
                string.Equals(profile.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal) &&
                profile.RetiredAt is null &&
                profile.EffectiveFrom <= effectiveAt &&
                (profile.EffectiveTo is null || profile.EffectiveTo > effectiveAt))
            .OrderBy(profile => profile.EffectiveFrom)
            .ThenBy(profile => profile.SalesInvoiceHeaderProfileId)
            .ToArray();

        if (matches.Length == 0)
        {
            return SalesInvoiceHeaderProfileResolutionResult.NotFound();
        }

        if (matches.Length > 1)
        {
            return SalesInvoiceHeaderProfileResolutionResult.Ambiguous();
        }

        var match = matches[0];
        var completeness = EvaluateCompleteness(match, siteId, sitePosServerId, effectiveAt);
        return completeness.IsComplete
            ? SalesInvoiceHeaderProfileResolutionResult.Resolved(match)
            : SalesInvoiceHeaderProfileResolutionResult.Incomplete(match, completeness);
    }

    public void ValidateNoApprovedOverlap(
        SalesInvoiceHeaderProfile candidate,
        IEnumerable<SalesInvoiceHeaderProfile> existingProfiles)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(existingProfiles);

        if (!string.Equals(candidate.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal))
        {
            return;
        }

        var overlaps = existingProfiles.Any(existing =>
            existing.SalesInvoiceHeaderProfileId != candidate.SalesInvoiceHeaderProfileId &&
            existing.SiteId == candidate.SiteId &&
            existing.SitePosServerId == candidate.SitePosServerId &&
            string.Equals(existing.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal) &&
            existing.RetiredAt is null &&
            candidate.EffectiveFrom < (existing.EffectiveTo ?? DateTimeOffset.MaxValue) &&
            existing.EffectiveFrom < (candidate.EffectiveTo ?? DateTimeOffset.MaxValue));

        if (overlaps)
        {
            throw new InvalidOperationException("Approved Sales Invoice header profile effective windows must not overlap for the same Site POS Server.");
        }
    }

    public SalesInvoiceHeaderSnapshot CreateSnapshot(
        SalesInvoiceHeaderProfile profile,
        string? terminalId,
        DateTimeOffset effectiveAt,
        DateTimeOffset snapshotCreatedAt)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var completeness = EvaluateCompleteness(profile, profile.SiteId, profile.SitePosServerId, effectiveAt);
        if (!completeness.IsComplete || profile.FiscalIdentity is null)
        {
            throw new InvalidOperationException("Sales Invoice header profile snapshot requires a complete approved profile.");
        }

        return new SalesInvoiceHeaderSnapshot(
            profile.FiscalIdentityId,
            profile.SalesInvoiceHeaderProfileId,
            profile.ProfileVersion.Trim(),
            profile.FiscalIdentity.RegisteredBusinessName.Trim(),
            profile.FiscalIdentity.RegisteredBusinessAddress.Trim(),
            profile.FiscalIdentity.Tin.Trim(),
            profile.PosSerialNumber!.Trim(),
            profile.MachineIdentificationNumber!.Trim(),
            profile.ParkingLocationDisplay!.Trim(),
            string.IsNullOrWhiteSpace(terminalId) ? null : terminalId.Trim(),
            profile.BirAccreditationNumber!.Trim(),
            profile.BirAccreditationIssuedDate!.Value,
            profile.BirAccreditationValidUntil!.Value,
            profile.PtuNumber!.Trim(),
            profile.PtuIssuedDate!.Value,
            profile.SalesInvoiceLegalStatement!.Trim(),
            profile.CustomerServiceFooter!.Trim(),
            profile.TemplateVersion,
            profile.PresentationVersion,
            effectiveAt,
            snapshotCreatedAt,
            profile.SupplierDeveloperRegisteredName!.Trim(),
            profile.SupplierDeveloperAddress!.Trim(),
            profile.SupplierDeveloperTin!.Trim());
    }
}
