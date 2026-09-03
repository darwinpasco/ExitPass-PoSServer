using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class SalesInvoiceHeaderProfileAdminServiceTests
{
    private static readonly Guid SiteId = Guid.Parse("aaaaaaaa-1000-0000-0000-000000000001");
    private static readonly Guid SitePosServerId = Guid.Parse("bbbbbbbb-1000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-18T08:00:00Z");

    [Fact]
    public async Task FiscalIdentityCreateReadAndPermittedUpdateWorkBeforeGovernedUse()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);

        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var updated = await service.UpdateFiscalIdentityAsync(new(
            identity.FiscalIdentityId,
            "GOVERNED TEST BUSINESS NAME UPDATED",
            identity.RegisteredBusinessAddress,
            identity.Tin,
            identity.TaxpayerClassification,
            identity.Status,
            "admin-updater",
            Now.AddMinutes(1)));

        Assert.Equal(identity.FiscalIdentityId, updated.FiscalIdentityId);
        Assert.Equal("GOVERNED TEST BUSINESS NAME UPDATED", updated.RegisteredBusinessName);
        Assert.Equal("admin-updater", updated.UpdatedByRef);
        Assert.Equal(updated, await service.GetFiscalIdentityAsync(identity.FiscalIdentityId));
    }

    [Fact]
    public async Task IdentityUpdateIsBlockedAfterApprovedProfileOrSnapshotUse()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var profile = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId));

        await service.ApproveHeaderProfileAsync(profile.SalesInvoiceHeaderProfileId, Now, "admin-approver");

        var ex = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() => service.UpdateFiscalIdentityAsync(new(
            identity.FiscalIdentityId,
            "NEW NAME",
            identity.RegisteredBusinessAddress,
            identity.Tin,
            identity.TaxpayerClassification,
            identity.Status,
            "admin-updater",
            Now.AddMinutes(2))));
        Assert.Equal("fiscal_identity_immutable_after_use", ex.Code);
    }

    [Fact]
    public async Task HeaderProfileDraftCanBeCreatedReadAndUpdated()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var profile = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId) with
        {
            PosSerialNumber = null,
            BirAccreditationIssuedDate = null
        });

        var validation = await service.ValidateHeaderProfileAsync(profile.SalesInvoiceHeaderProfileId, Now);
        Assert.False(validation.IsComplete);
        Assert.Contains("pos_serial_number_missing", validation.FailureCodes);
        Assert.Contains("bir_accreditation_issued_date_missing", validation.FailureCodes);

        var updated = await service.UpdateHeaderProfileDraftAsync(ValidUpdateCommand(profile, identity.FiscalIdentityId));
        Assert.Equal("TEST-SERIAL-0001", updated.PosSerialNumber);
        Assert.Equal("admin-updater", updated.UpdatedByRef);
    }

    [Fact]
    public async Task DuplicateProfileVersionIsRejectedWithinSitePosServerScope()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());

        await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId));
        var ex = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId)));

        Assert.Equal("duplicate_profile_version", ex.Code);
    }

    [Fact]
    public async Task ApprovalRecordsActorTimestampAndBlocksInPlaceEdit()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var profile = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId));

        var approved = await service.ApproveHeaderProfileAsync(profile.SalesInvoiceHeaderProfileId, Now.AddMinutes(1), "admin-approver");

        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Approved, approved.LifecycleStatus);
        Assert.Equal("admin-approver", approved.ApprovedByRef);
        Assert.Equal(Now.AddMinutes(1), approved.ApprovedAt);
        var ex = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.UpdateHeaderProfileDraftAsync(ValidUpdateCommand(approved, identity.FiscalIdentityId)));
        Assert.Equal("profile_not_draft", ex.Code);
    }

    [Fact]
    public async Task ApprovalRejectsIncompleteUnsupportedInvalidDateAndOverlapLeavingDraft()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var incomplete = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId, "v-incomplete") with
        {
            PosSerialNumber = "",
            TemplateVersion = "unsupported",
            BirAccreditationValidUntil = new DateOnly(2025, 1, 1)
        });

        var incompleteEx = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.ApproveHeaderProfileAsync(incomplete.SalesInvoiceHeaderProfileId, Now, "admin-approver"));
        Assert.Equal("profile_incomplete", incompleteEx.Code);
        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Draft, (await service.GetHeaderProfileAsync(incomplete.SalesInvoiceHeaderProfileId))!.LifecycleStatus);

        var first = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId, "v1"));
        await service.ApproveHeaderProfileAsync(first.SalesInvoiceHeaderProfileId, Now, "admin-approver");
        var overlap = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId, "v2"));
        var overlapEx = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.ApproveHeaderProfileAsync(overlap.SalesInvoiceHeaderProfileId, Now, "admin-approver"));
        Assert.Equal("overlapping_approved_effective_window", overlapEx.Code);
    }

    [Fact]
    public async Task RetirementSucceedsForApprovedProfileAndRetiredProfileCannotBeReapproved()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());
        var draft = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId));

        var draftRetireEx = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.RetireHeaderProfileAsync(draft.SalesInvoiceHeaderProfileId, Now, "admin-retirer"));
        Assert.Equal("retirement_requires_approved_profile", draftRetireEx.Code);

        var approved = await service.ApproveHeaderProfileAsync(draft.SalesInvoiceHeaderProfileId, Now, "admin-approver");
        var retired = await service.RetireHeaderProfileAsync(approved.SalesInvoiceHeaderProfileId, Now.AddHours(1), "admin-retirer");

        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Retired, retired.LifecycleStatus);
        Assert.Equal(Now.AddHours(1), retired.RetiredAt);
        Assert.Equal(Now.AddHours(1), retired.EffectiveTo);
        var reapproveEx = await Assert.ThrowsAsync<SalesInvoiceHeaderProfileAdminException>(() =>
            service.ApproveHeaderProfileAsync(retired.SalesInvoiceHeaderProfileId, Now.AddHours(2), "admin-approver"));
        Assert.Equal("invalid_lifecycle_transition", reapproveEx.Code);
    }

    [Fact]
    public async Task EffectiveReadinessReportsReadyMissingExpiredAndUsage()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository, enforcementRequired: true);
        var identity = await service.CreateFiscalIdentityAsync(ValidIdentityCommand());

        var missing = await service.GetEffectiveReadinessAsync(SiteId, SitePosServerId, Now);
        Assert.Equal("NO_EFFECTIVE_PROFILE", missing.ResolutionStatus);
        Assert.True(missing.EnforcementRequired);

        var profile = await service.CreateHeaderProfileAsync(ValidProfileCommand(identity.FiscalIdentityId));
        var approved = await service.ApproveHeaderProfileAsync(profile.SalesInvoiceHeaderProfileId, Now, "admin-approver");
        var ready = await service.GetEffectiveReadinessAsync(SiteId, SitePosServerId, Now.AddMinutes(1));
        Assert.Equal("READY", ready.ResolutionStatus);
        Assert.Equal(approved.SalesInvoiceHeaderProfileId, ready.SalesInvoiceHeaderProfileId);
        Assert.Equal("currently_valid", ready.BirAccreditationPosture);
        Assert.Equal("complete", ready.PtuPosture);

        repository.AddUsage(approved.SalesInvoiceHeaderProfileId, Guid.Parse("ffffffff-1000-0000-0000-000000000001"), Now.AddMinutes(2));
        var usage = await service.GetHeaderProfileUsageAsync(approved.SalesInvoiceHeaderProfileId);
        Assert.Equal(1, usage.FiscalDocumentCount);
        Assert.True(usage.DestructiveMutationBlocked);
        Assert.Contains(Guid.Parse("ffffffff-1000-0000-0000-000000000001"), usage.SampleFiscalDocumentIds);
    }

    [Fact]
    public void AdminProfileModelDoesNotIntroduceExternalWorkflowBehavior()
    {
        var forbiddenNames = new[] { "Print", "Apt", "CentralPms", "Exit", "Gate" };
        foreach (var type in new[] { typeof(SalesInvoiceHeaderProfileAdminService), typeof(SalesInvoiceHeaderProfileUsage) })
        {
            foreach (var member in type.GetMembers())
            {
                foreach (var forbidden in forbiddenNames)
                {
                    Assert.DoesNotContain(forbidden, member.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static CreateFiscalIdentityProfileCommand ValidIdentityCommand() =>
        new(
            "GOVERNED TEST BUSINESS NAME",
            "GOVERNED TEST ADDRESS",
            "TEST-TIN-0001",
            "VAT_REGISTERED_TEST",
            SalesInvoiceHeaderProfileLifecycle.Draft,
            "admin-creator",
            Now);

    private static CreateSalesInvoiceHeaderProfileCommand ValidProfileCommand(Guid fiscalIdentityId, string version = "v1") =>
        new(
            fiscalIdentityId,
            SiteId,
            SitePosServerId,
            version,
            SalesInvoiceHeaderProfileVersions.TemplateVersion,
            SalesInvoiceHeaderProfileVersions.PresentationVersion,
            "TEST-SERIAL-0001",
            "TEST-MIN-0001",
            "GOVERNED TEST PARKING LOCATION",
            "TEST-BIR-ACCREDITATION-0001",
            new DateOnly(2026, 1, 15),
            new DateOnly(2027, 1, 15),
            "TEST-PTU-0001",
            new DateOnly(2026, 2, 10),
            "THIS SERVES AS YOUR SALES INVOICE",
            "CUSTOMER SERVICE TEST FOOTER",
            Now.AddDays(-1),
            null,
            "admin-creator",
            Now,
            "GOVERNED TEST SOFTWARE SUPPLIER",
            "GOVERNED TEST SOFTWARE ADDRESS",
            "TEST-SUPPLIER-TIN-0001");

    private static UpdateSalesInvoiceHeaderProfileDraftCommand ValidUpdateCommand(SalesInvoiceHeaderProfile profile, Guid fiscalIdentityId) =>
        new(
            profile.SalesInvoiceHeaderProfileId,
            fiscalIdentityId,
            SiteId,
            SitePosServerId,
            profile.ProfileVersion,
            SalesInvoiceHeaderProfileVersions.TemplateVersion,
            SalesInvoiceHeaderProfileVersions.PresentationVersion,
            "TEST-SERIAL-0001",
            "TEST-MIN-0001",
            "GOVERNED TEST PARKING LOCATION",
            "TEST-BIR-ACCREDITATION-0001",
            new DateOnly(2026, 1, 15),
            new DateOnly(2027, 1, 15),
            "TEST-PTU-0001",
            new DateOnly(2026, 2, 10),
            "THIS SERVES AS YOUR SALES INVOICE",
            "CUSTOMER SERVICE TEST FOOTER",
            Now.AddDays(-1),
            null,
            "admin-updater",
            Now.AddMinutes(1),
            "GOVERNED TEST SOFTWARE SUPPLIER",
            "GOVERNED TEST SOFTWARE ADDRESS",
            "TEST-SUPPLIER-TIN-0001");

    private sealed class InMemoryProfileRepository : ISalesInvoiceHeaderProfileRepository
    {
        private readonly Dictionary<Guid, FiscalIdentityProfile> identities = [];
        private readonly Dictionary<Guid, SalesInvoiceHeaderProfile> profiles = [];
        private readonly List<(Guid ProfileId, Guid DocumentId, DateTimeOffset SnapshotCreatedAt)> usage = [];

        public Task<FiscalIdentityProfile> CreateFiscalIdentityAsync(FiscalIdentityProfile identity, CancellationToken cancellationToken)
        {
            identities.Add(identity.FiscalIdentityId, identity);
            return Task.FromResult(identity);
        }

        public Task<FiscalIdentityProfile?> GetFiscalIdentityAsync(Guid fiscalIdentityId, CancellationToken cancellationToken) =>
            Task.FromResult(identities.GetValueOrDefault(fiscalIdentityId));

        public Task<FiscalIdentityProfile> UpdateFiscalIdentityAsync(FiscalIdentityProfile identity, CancellationToken cancellationToken)
        {
            identities[identity.FiscalIdentityId] = identity;
            return Task.FromResult(identity);
        }

        public Task<bool> IsFiscalIdentityInGovernedUseAsync(Guid fiscalIdentityId, CancellationToken cancellationToken) =>
            Task.FromResult(profiles.Values.Any(profile =>
                profile.FiscalIdentityId == fiscalIdentityId &&
                profile.LifecycleStatus is SalesInvoiceHeaderProfileLifecycle.Approved or SalesInvoiceHeaderProfileLifecycle.Retired) ||
                usage.Any(item => profiles.TryGetValue(item.ProfileId, out var profile) && profile.FiscalIdentityId == fiscalIdentityId));

        public Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken)
        {
            profiles.Add(profile.SalesInvoiceHeaderProfileId, Hydrate(profile));
            return Task.FromResult(profiles[profile.SalesInvoiceHeaderProfileId]);
        }

        public Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken) =>
            Task.FromResult(profiles.GetValueOrDefault(salesInvoiceHeaderProfileId));

        public Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ListHeaderProfilesAsync(Guid? siteId, Guid? sitePosServerId, CancellationToken cancellationToken)
        {
            var values = profiles.Values
                .Where(profile => siteId is null || profile.SiteId == siteId)
                .Where(profile => sitePosServerId is null || profile.SitePosServerId == sitePosServerId)
                .ToArray();
            return Task.FromResult<IReadOnlyList<SalesInvoiceHeaderProfile>>(values);
        }

        public Task<SalesInvoiceHeaderProfile> UpdateHeaderProfileDraftAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken)
        {
            profiles[profile.SalesInvoiceHeaderProfileId] = Hydrate(profile);
            return Task.FromResult(profiles[profile.SalesInvoiceHeaderProfileId]);
        }

        public Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset approvedAt, string approvedByRef, CancellationToken cancellationToken)
        {
            var profile = profiles[salesInvoiceHeaderProfileId] with
            {
                LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Approved,
                ApprovedAt = approvedAt,
                ApprovedByRef = approvedByRef,
                UpdatedAt = approvedAt,
                UpdatedByRef = approvedByRef
            };
            profiles[salesInvoiceHeaderProfileId] = profile;
            return Task.FromResult(profile);
        }

        public Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset retiredAt, string retiredByRef, CancellationToken cancellationToken)
        {
            var profile = profiles[salesInvoiceHeaderProfileId] with
            {
                LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Retired,
                RetiredAt = retiredAt,
                EffectiveTo = retiredAt,
                UpdatedAt = retiredAt,
                UpdatedByRef = retiredByRef
            };
            profiles[salesInvoiceHeaderProfileId] = profile;
            return Task.FromResult(profile);
        }

        public Task<SalesInvoiceHeaderProfileResolutionResult> ResolveEffectiveSalesInvoiceHeaderProfileAsync(Guid siteId, Guid sitePosServerId, DateTimeOffset effectiveAt, CancellationToken cancellationToken) =>
            Task.FromResult(new SalesInvoiceHeaderProfileService().ResolveEffective(profiles.Values, siteId, sitePosServerId, effectiveAt));

        public Task<SalesInvoiceHeaderProfileUsage> GetHeaderProfileUsageAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken)
        {
            var rows = usage.Where(item => item.ProfileId == salesInvoiceHeaderProfileId).OrderBy(item => item.SnapshotCreatedAt).ToArray();
            var profile = profiles.GetValueOrDefault(salesInvoiceHeaderProfileId);
            return Task.FromResult(new SalesInvoiceHeaderProfileUsage(
                salesInvoiceHeaderProfileId,
                profile?.ProfileVersion,
                profile?.FiscalIdentityId,
                rows.FirstOrDefault().SnapshotCreatedAt == default ? null : rows.First().SnapshotCreatedAt,
                rows.LastOrDefault().SnapshotCreatedAt == default ? null : rows.Last().SnapshotCreatedAt,
                rows.Length,
                rows.Select(row => row.DocumentId).ToArray(),
                rows.Length > 0 || profile?.HasBeenSnapshotted == true));
        }

        public void AddUsage(Guid profileId, Guid documentId, DateTimeOffset snapshotCreatedAt)
        {
            usage.Add((profileId, documentId, snapshotCreatedAt));
            profiles[profileId] = profiles[profileId] with { HasBeenSnapshotted = true };
        }

        private SalesInvoiceHeaderProfile Hydrate(SalesInvoiceHeaderProfile profile) =>
            profile with { FiscalIdentity = identities.GetValueOrDefault(profile.FiscalIdentityId) };
    }
}
