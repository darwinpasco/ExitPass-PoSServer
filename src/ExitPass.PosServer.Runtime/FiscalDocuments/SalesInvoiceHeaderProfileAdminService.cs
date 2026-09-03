namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class SalesInvoiceHeaderProfileAdminService
{
    private static readonly IReadOnlyDictionary<string, string> ValidationMessages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["registered_business_name_missing"] = "Registered business name is required.",
        ["registered_business_address_missing"] = "Registered business address is required.",
        ["tin_missing"] = "TIN is required.",
        ["pos_serial_number_missing"] = "POS serial number is required.",
        ["machine_identification_number_missing"] = "Machine Identification Number is required.",
        ["bir_accreditation_number_missing"] = "BIR accreditation number is required.",
        ["bir_accreditation_issued_date_missing"] = "BIR accreditation issued date is required.",
        ["bir_accreditation_valid_until_missing"] = "BIR accreditation valid-until date is required.",
        ["bir_accreditation_valid_until_before_issued_date"] = "BIR accreditation valid-until date must not be before the issued date.",
        ["ptu_number_missing"] = "PTU number is required.",
        ["ptu_issued_date_missing"] = "PTU issued date is required.",
        ["parking_location_display_missing"] = "Parking location display is required.",
        ["sales_invoice_legal_statement_missing"] = "Sales Invoice legal statement is required.",
        ["customer_service_footer_missing"] = "Customer service footer is required.",
        ["unsupported_template_version"] = "Template version is not supported by this POS Server.",
        ["unsupported_presentation_version"] = "Presentation version is not supported by this POS Server.",
        ["profile_not_approved"] = "Profile must be approved before issuance.",
        ["profile_not_effective_yet"] = "Profile is not effective yet.",
        ["profile_expired"] = "Profile is expired.",
        ["profile_retired"] = "Profile is retired.",
        ["invalid_effective_window"] = "Effective-to must be later than effective-from.",
        ["site_mismatch"] = "Profile does not belong to the requested Site.",
        ["site_pos_server_mismatch"] = "Profile does not belong to the requested Site POS Server.",
        ["multiple_effective_profiles"] = "Multiple approved profiles are effective for the requested Site POS Server.",
        ["profile_not_found"] = "No effective profile was found."
    };

    private readonly ISalesInvoiceHeaderProfileRepository repository;
    private readonly SalesInvoiceHeaderProfileService profileService = new();
    private readonly bool enforcementRequired;

    public SalesInvoiceHeaderProfileAdminService(
        ISalesInvoiceHeaderProfileRepository repository,
        bool enforcementRequired = false)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.enforcementRequired = enforcementRequired;
    }

    public async Task<FiscalIdentityProfile> CreateFiscalIdentityAsync(
        CreateFiscalIdentityProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireActor(command.ActorRef, "created_by_ref_required");
        var now = command.RequestedAt;
        var identity = new FiscalIdentityProfile(
            Guid.NewGuid(),
            RequireText(command.RegisteredBusinessName, "registered_business_name_missing"),
            RequireText(command.RegisteredBusinessAddress, "registered_business_address_missing"),
            RequireText(command.Tin, "tin_missing"),
            Normalize(command.TaxpayerClassification),
            NormalizeStatus(command.Status),
            now,
            now,
            command.ActorRef.Trim(),
            command.ActorRef.Trim());

        return await repository.CreateFiscalIdentityAsync(identity, cancellationToken).ConfigureAwait(false);
    }

    public Task<FiscalIdentityProfile?> GetFiscalIdentityAsync(Guid fiscalIdentityId, CancellationToken cancellationToken = default) =>
        repository.GetFiscalIdentityAsync(fiscalIdentityId, cancellationToken);

    public async Task<FiscalIdentityProfile> UpdateFiscalIdentityAsync(
        UpdateFiscalIdentityProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireActor(command.ActorRef, "updated_by_ref_required");
        var existing = await repository.GetFiscalIdentityAsync(command.FiscalIdentityId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("fiscal_identity_not_found", "Fiscal Identity was not found.");
        if (await repository.IsFiscalIdentityInGovernedUseAsync(command.FiscalIdentityId, cancellationToken).ConfigureAwait(false))
        {
            throw new SalesInvoiceHeaderProfileAdminException("fiscal_identity_immutable_after_use", "Fiscal Identity is referenced by an approved profile or issued snapshot.");
        }

        var updated = existing with
        {
            RegisteredBusinessName = RequireText(command.RegisteredBusinessName, "registered_business_name_missing"),
            RegisteredBusinessAddress = RequireText(command.RegisteredBusinessAddress, "registered_business_address_missing"),
            Tin = RequireText(command.Tin, "tin_missing"),
            TaxpayerClassification = Normalize(command.TaxpayerClassification),
            Status = NormalizeStatus(command.Status),
            UpdatedAt = command.RequestedAt,
            UpdatedByRef = command.ActorRef.Trim()
        };

        return await repository.UpdateFiscalIdentityAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(
        CreateSalesInvoiceHeaderProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireActor(command.ActorRef, "created_by_ref_required");
        await RequireFiscalIdentity(command.FiscalIdentityId, cancellationToken).ConfigureAwait(false);
        await RejectDuplicateVersion(command.SitePosServerId, command.ProfileVersion, null, cancellationToken).ConfigureAwait(false);

        var profile = ToProfile(command, SalesInvoiceHeaderProfileLifecycle.Draft);
        return await repository.CreateHeaderProfileAsync(profile, cancellationToken).ConfigureAwait(false);
    }

    public Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken = default) =>
        repository.GetHeaderProfileAsync(salesInvoiceHeaderProfileId, cancellationToken);

    public Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ListHeaderProfilesAsync(
        Guid? siteId,
        Guid? sitePosServerId,
        CancellationToken cancellationToken = default) =>
        repository.ListHeaderProfilesAsync(siteId, sitePosServerId, cancellationToken);

    public async Task<SalesInvoiceHeaderProfile> UpdateHeaderProfileDraftAsync(
        UpdateSalesInvoiceHeaderProfileDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        RequireActor(command.ActorRef, "updated_by_ref_required");
        var existing = await repository.GetHeaderProfileAsync(command.SalesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("sales_invoice_header_profile_not_found", "Sales Invoice header profile was not found.");
        if (!string.Equals(existing.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Draft, StringComparison.Ordinal))
        {
            throw new SalesInvoiceHeaderProfileAdminException("profile_not_draft", "Only DRAFT profiles may be edited in place.");
        }

        if (existing.HasBeenSnapshotted)
        {
            throw new SalesInvoiceHeaderProfileAdminException("profile_immutable_after_use", "Profile has issued snapshot usage and cannot be edited.");
        }

        await RequireFiscalIdentity(command.FiscalIdentityId, cancellationToken).ConfigureAwait(false);
        await RejectDuplicateVersion(command.SitePosServerId, command.ProfileVersion, command.SalesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false);

        var updated = ToProfile(command, existing);
        return await repository.UpdateHeaderProfileDraftAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ValidateSalesInvoiceHeaderProfileResult> ValidateHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken = default)
    {
        var profile = await repository.GetHeaderProfileAsync(salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("sales_invoice_header_profile_not_found", "Sales Invoice header profile was not found.");
        return await ValidateProfile(profile, evaluatedAt, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset approvedAt,
        string approvedByRef,
        CancellationToken cancellationToken = default)
    {
        RequireActor(approvedByRef, "approved_by_ref_required");
        var profile = await repository.GetHeaderProfileAsync(salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("sales_invoice_header_profile_not_found", "Sales Invoice header profile was not found.");
        if (!string.Equals(profile.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Draft, StringComparison.Ordinal))
        {
            throw new SalesInvoiceHeaderProfileAdminException("invalid_lifecycle_transition", "Only DRAFT profiles may be approved.");
        }

        var validation = await ValidateProfile(
            profile with
            {
                LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Approved,
                ApprovedAt = approvedAt,
                ApprovedByRef = approvedByRef.Trim()
            },
            profile.EffectiveFrom,
            cancellationToken).ConfigureAwait(false);
        if (!validation.IsComplete)
        {
            if (string.Equals(validation.OverlapPosture, "overlap", StringComparison.Ordinal))
            {
                throw new SalesInvoiceHeaderProfileAdminException(
                    "overlapping_approved_effective_window",
                    "Approved Sales Invoice header profile effective windows must not overlap for the same Site POS Server.");
            }

            throw new SalesInvoiceHeaderProfileAdminException("profile_incomplete", string.Join(",", validation.FailureCodes));
        }

        return await repository.ApproveHeaderProfileAsync(salesInvoiceHeaderProfileId, approvedAt, approvedByRef, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        DateTimeOffset retiredAt,
        string retiredByRef,
        CancellationToken cancellationToken = default)
    {
        RequireActor(retiredByRef, "retired_by_ref_required");
        var profile = await repository.GetHeaderProfileAsync(salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("sales_invoice_header_profile_not_found", "Sales Invoice header profile was not found.");
        if (!string.Equals(profile.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal))
        {
            throw new SalesInvoiceHeaderProfileAdminException("retirement_requires_approved_profile", "Only APPROVED profiles may be retired.");
        }

        return await repository.RetireHeaderProfileAsync(salesInvoiceHeaderProfileId, retiredAt, retiredByRef, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SalesInvoiceHeaderProfileReadiness> GetEffectiveReadinessAsync(
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken = default)
    {
        var profiles = await repository.ListHeaderProfilesAsync(siteId, sitePosServerId, cancellationToken).ConfigureAwait(false);
        var resolution = profileService.ResolveEffective(profiles, siteId, sitePosServerId, effectiveAt);
        var profile = resolution.Profile;
        var failureCodes = resolution.Completeness.FailureCodes;
        var status = resolution.Status switch
        {
            SalesInvoiceHeaderProfileResolutionStatus.Resolved => "READY",
            SalesInvoiceHeaderProfileResolutionStatus.Ambiguous => "AMBIGUOUS",
            SalesInvoiceHeaderProfileResolutionStatus.Incomplete => "INCOMPLETE",
            SalesInvoiceHeaderProfileResolutionStatus.UnsupportedVersion => "UNSUPPORTED_VERSION",
            _ => ResolveNoProfileStatus(profiles, effectiveAt)
        };

        return new SalesInvoiceHeaderProfileReadiness(
            siteId,
            sitePosServerId,
            effectiveAt,
            status,
            profile?.SalesInvoiceHeaderProfileId,
            profile?.ProfileVersion,
            profile?.FiscalIdentityId,
            profile?.LifecycleStatus,
            resolution.Status == SalesInvoiceHeaderProfileResolutionStatus.Resolved,
            enforcementRequired,
            failureCodes,
            BirPosture(profile, effectiveAt),
            PtuPosture(profile),
            VersionPosture(profile),
            resolution.Status == SalesInvoiceHeaderProfileResolutionStatus.Ambiguous ? "ambiguous" : "clear",
            profiles.Count == 0 ? null : profiles.Max(profile => profile.UpdatedAt),
            DateTimeOffset.UtcNow);
    }

    public Task<SalesInvoiceHeaderProfileUsage> GetHeaderProfileUsageAsync(
        Guid salesInvoiceHeaderProfileId,
        CancellationToken cancellationToken = default) =>
        repository.GetHeaderProfileUsageAsync(salesInvoiceHeaderProfileId, cancellationToken);

    private async Task<ValidateSalesInvoiceHeaderProfileResult> ValidateProfile(
        SalesInvoiceHeaderProfile profile,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken)
    {
        var identity = profile.FiscalIdentity ??
            await repository.GetFiscalIdentityAsync(profile.FiscalIdentityId, cancellationToken).ConfigureAwait(false);
        var hydrated = profile with { FiscalIdentity = identity };
        var completeness = profileService.EvaluateCompleteness(hydrated, hydrated.SiteId, hydrated.SitePosServerId, evaluatedAt);
        var profiles = await repository.ListHeaderProfilesAsync(hydrated.SiteId, hydrated.SitePosServerId, cancellationToken).ConfigureAwait(false);
        var overlapPosture = HasApprovedOverlap(hydrated, profiles) ? "overlap" : "clear";
        var failures = overlapPosture == "overlap"
            ? completeness.FailureCodes.Concat(["overlapping_approved_effective_window"]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
            : completeness.FailureCodes;

        return new ValidateSalesInvoiceHeaderProfileResult(
            hydrated.SalesInvoiceHeaderProfileId,
            hydrated.LifecycleStatus,
            failures.Count == 0,
            failures,
            failures.ToDictionary(code => code, code => ValidationMessages.TryGetValue(code, out var message) ? message : "Validation failed.", StringComparer.Ordinal),
            string.Equals(hydrated.TemplateVersion, SalesInvoiceHeaderProfileVersions.TemplateVersion, StringComparison.Ordinal) ? "supported" : "unsupported",
            string.Equals(hydrated.PresentationVersion, SalesInvoiceHeaderProfileVersions.PresentationVersion, StringComparison.Ordinal) ? "supported" : "unsupported",
            hydrated.EffectiveTo is null || hydrated.EffectiveTo > hydrated.EffectiveFrom ? "valid" : "invalid",
            overlapPosture,
            identity is null ? "missing" : "present",
            evaluatedAt);
    }

    private static string ResolveNoProfileStatus(IReadOnlyList<SalesInvoiceHeaderProfile> profiles, DateTimeOffset effectiveAt)
    {
        if (profiles.Any(profile => string.Equals(profile.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Retired, StringComparison.Ordinal)))
        {
            return "RETIRED";
        }

        if (profiles.Any(profile => profile.EffectiveTo is not null && profile.EffectiveTo <= effectiveAt))
        {
            return "EXPIRED";
        }

        return "NO_EFFECTIVE_PROFILE";
    }

    private static string BirPosture(SalesInvoiceHeaderProfile? profile, DateTimeOffset effectiveAt)
    {
        if (profile?.BirAccreditationIssuedDate is null || profile.BirAccreditationValidUntil is null)
        {
            return "missing";
        }

        var date = DateOnly.FromDateTime(effectiveAt.UtcDateTime);
        if (date < profile.BirAccreditationIssuedDate.Value)
        {
            return "not_yet_valid";
        }

        return date <= profile.BirAccreditationValidUntil.Value ? "currently_valid" : "expired";
    }

    private static string PtuPosture(SalesInvoiceHeaderProfile? profile) =>
        !string.IsNullOrWhiteSpace(profile?.PtuNumber) && profile.PtuIssuedDate is not null ? "complete" : "missing";

    private static string VersionPosture(SalesInvoiceHeaderProfile? profile) =>
        profile is not null &&
        string.Equals(profile.TemplateVersion, SalesInvoiceHeaderProfileVersions.TemplateVersion, StringComparison.Ordinal) &&
        string.Equals(profile.PresentationVersion, SalesInvoiceHeaderProfileVersions.PresentationVersion, StringComparison.Ordinal)
            ? "supported"
            : "unsupported_or_missing";

    private static bool HasApprovedOverlap(
        SalesInvoiceHeaderProfile candidate,
        IReadOnlyList<SalesInvoiceHeaderProfile> profiles)
    {
        if (!string.Equals(candidate.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal))
        {
            return false;
        }

        return profiles.Any(existing =>
            existing.SalesInvoiceHeaderProfileId != candidate.SalesInvoiceHeaderProfileId &&
            existing.SiteId == candidate.SiteId &&
            existing.SitePosServerId == candidate.SitePosServerId &&
            string.Equals(existing.LifecycleStatus, SalesInvoiceHeaderProfileLifecycle.Approved, StringComparison.Ordinal) &&
            existing.RetiredAt is null &&
            candidate.EffectiveFrom < (existing.EffectiveTo ?? DateTimeOffset.MaxValue) &&
            existing.EffectiveFrom < (candidate.EffectiveTo ?? DateTimeOffset.MaxValue));
    }

    private async Task RequireFiscalIdentity(Guid fiscalIdentityId, CancellationToken cancellationToken)
    {
        _ = await repository.GetFiscalIdentityAsync(fiscalIdentityId, cancellationToken).ConfigureAwait(false) ??
            throw new SalesInvoiceHeaderProfileAdminException("fiscal_identity_not_found", "Fiscal Identity was not found.");
    }

    private async Task RejectDuplicateVersion(
        Guid sitePosServerId,
        string profileVersion,
        Guid? currentProfileId,
        CancellationToken cancellationToken)
    {
        var profiles = await repository.ListHeaderProfilesAsync(null, sitePosServerId, cancellationToken).ConfigureAwait(false);
        if (profiles.Any(profile =>
            profile.SalesInvoiceHeaderProfileId != currentProfileId &&
            string.Equals(profile.ProfileVersion, profileVersion.Trim(), StringComparison.Ordinal)))
        {
            throw new SalesInvoiceHeaderProfileAdminException("duplicate_profile_version", "Profile version already exists for this Site POS Server.");
        }
    }

    private static SalesInvoiceHeaderProfile ToProfile(
        CreateSalesInvoiceHeaderProfileCommand command,
        string lifecycleStatus) =>
        new(
            Guid.NewGuid(),
            command.FiscalIdentityId,
            command.SiteId,
            command.SitePosServerId,
            RequireText(command.ProfileVersion, "profile_version_missing"),
            RequireText(command.TemplateVersion, "template_version_missing"),
            RequireText(command.PresentationVersion, "presentation_version_missing"),
            Normalize(command.PosSerialNumber),
            Normalize(command.MachineIdentificationNumber),
            Normalize(command.ParkingLocationDisplay),
            Normalize(command.BirAccreditationNumber),
            command.BirAccreditationIssuedDate,
            command.BirAccreditationValidUntil,
            Normalize(command.PtuNumber),
            command.PtuIssuedDate,
            Normalize(command.SalesInvoiceLegalStatement),
            Normalize(command.CustomerServiceFooter),
            command.EffectiveFrom,
            command.EffectiveTo,
            lifecycleStatus,
            null,
            null,
            null,
            command.RequestedAt,
            command.RequestedAt,
            command.ActorRef.Trim(),
            command.ActorRef.Trim())
        {
            SupplierDeveloperRegisteredName = Normalize(command.SupplierDeveloperRegisteredName),
            SupplierDeveloperAddress = Normalize(command.SupplierDeveloperAddress),
            SupplierDeveloperTin = Normalize(command.SupplierDeveloperTin)
        };

    private static SalesInvoiceHeaderProfile ToProfile(
        UpdateSalesInvoiceHeaderProfileDraftCommand command,
        SalesInvoiceHeaderProfile existing) =>
        existing with
        {
            FiscalIdentityId = command.FiscalIdentityId,
            SiteId = command.SiteId,
            SitePosServerId = command.SitePosServerId,
            ProfileVersion = RequireText(command.ProfileVersion, "profile_version_missing"),
            TemplateVersion = RequireText(command.TemplateVersion, "template_version_missing"),
            PresentationVersion = RequireText(command.PresentationVersion, "presentation_version_missing"),
            PosSerialNumber = Normalize(command.PosSerialNumber),
            MachineIdentificationNumber = Normalize(command.MachineIdentificationNumber),
            ParkingLocationDisplay = Normalize(command.ParkingLocationDisplay),
            BirAccreditationNumber = Normalize(command.BirAccreditationNumber),
            BirAccreditationIssuedDate = command.BirAccreditationIssuedDate,
            BirAccreditationValidUntil = command.BirAccreditationValidUntil,
            PtuNumber = Normalize(command.PtuNumber),
            PtuIssuedDate = command.PtuIssuedDate,
            SalesInvoiceLegalStatement = Normalize(command.SalesInvoiceLegalStatement),
            CustomerServiceFooter = Normalize(command.CustomerServiceFooter),
            SupplierDeveloperRegisteredName = Normalize(command.SupplierDeveloperRegisteredName),
            SupplierDeveloperAddress = Normalize(command.SupplierDeveloperAddress),
            SupplierDeveloperTin = Normalize(command.SupplierDeveloperTin),
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = command.EffectiveTo,
            UpdatedAt = command.RequestedAt,
            UpdatedByRef = command.ActorRef.Trim()
        };

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? SalesInvoiceHeaderProfileLifecycle.Draft : status.Trim().ToUpperInvariant();

    private static string RequireText(string? value, string code) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new SalesInvoiceHeaderProfileAdminException(code, code)
            : value.Trim();

    private static void RequireActor(string? actorRef, string code)
    {
        if (string.IsNullOrWhiteSpace(actorRef))
        {
            throw new SalesInvoiceHeaderProfileAdminException(code, code);
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
