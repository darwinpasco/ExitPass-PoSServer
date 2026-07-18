using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class SalesInvoiceHeaderProfileServiceTests
{
    private readonly SalesInvoiceHeaderProfileService service = new();
    private static readonly Guid SiteId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid SitePosServerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset EffectiveAt = DateTimeOffset.Parse("2026-07-18T08:00:00Z");

    [Fact]
    public void FiscalIdentityCanBeRepresentedWithGovernedFields()
    {
        var identity = ValidIdentity();

        Assert.Equal("GOVERNED TEST BUSINESS NAME", identity.RegisteredBusinessName);
        Assert.Equal("GOVERNED TEST ADDRESS", identity.RegisteredBusinessAddress);
        Assert.Equal("TEST-TIN-0001", identity.Tin);
        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Approved, identity.Status);
    }

    [Fact]
    public void CompleteApprovedCurrentProfilePassesCompleteness()
    {
        var profile = ValidProfile();

        var result = service.EvaluateCompleteness(profile, SiteId, SitePosServerId, EffectiveAt);

        Assert.True(result.IsComplete);
        Assert.Empty(result.FailureCodes);
    }

    [Theory]
    [InlineData(SalesInvoiceHeaderProfileLifecycle.Draft, "profile_not_approved")]
    [InlineData(SalesInvoiceHeaderProfileLifecycle.Retired, "profile_not_approved")]
    public void DraftAndRetiredProfilesDoNotPassCompleteness(string status, string expectedFailure)
    {
        var profile = ValidProfile() with { LifecycleStatus = status, RetiredAt = status == SalesInvoiceHeaderProfileLifecycle.Retired ? EffectiveAt : null };

        var result = service.EvaluateCompleteness(profile, SiteId, SitePosServerId, EffectiveAt);

        Assert.False(result.IsComplete);
        Assert.Contains(expectedFailure, result.FailureCodes);
    }

    [Fact]
    public void ApprovedCurrentProfileResolves()
    {
        var result = service.ResolveEffective([ValidProfile()], SiteId, SitePosServerId, EffectiveAt);

        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.Resolved, result.Status);
        Assert.NotNull(result.Profile);
    }

    [Fact]
    public void FutureExpiredAndRetiredProfilesDoNotResolve()
    {
        var future = ValidProfile() with { EffectiveFrom = EffectiveAt.AddDays(1) };
        var expired = ValidProfile(Guid.Parse("cccccccc-0000-0000-0000-000000000002"), "v2") with { EffectiveTo = EffectiveAt.AddMinutes(-1) };
        var retired = ValidProfile(Guid.Parse("cccccccc-0000-0000-0000-000000000003"), "v3") with { RetiredAt = EffectiveAt, LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Retired };

        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.NotFound, service.ResolveEffective([future], SiteId, SitePosServerId, EffectiveAt).Status);
        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.NotFound, service.ResolveEffective([expired], SiteId, SitePosServerId, EffectiveAt).Status);
        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.NotFound, service.ResolveEffective([retired], SiteId, SitePosServerId, EffectiveAt).Status);
    }

    [Fact]
    public void OverlappingApprovedEffectiveWindowsAreRejected()
    {
        var first = ValidProfile();
        var second = ValidProfile(Guid.Parse("cccccccc-0000-0000-0000-000000000002"), "v2");

        var ex = Assert.Throws<InvalidOperationException>(() => service.ValidateNoApprovedOverlap(second, [first]));
        Assert.Contains("must not overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.Ambiguous, service.ResolveEffective([first, second], SiteId, SitePosServerId, EffectiveAt).Status);
    }

    [Fact]
    public void DifferentSitePosServersMayHaveIndependentProfiles()
    {
        var first = ValidProfile();
        var second = ValidProfile(Guid.Parse("cccccccc-0000-0000-0000-000000000002"), "v2") with
        {
            SitePosServerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002")
        };

        service.ValidateNoApprovedOverlap(second, [first]);
        Assert.Equal(SalesInvoiceHeaderProfileResolutionStatus.Resolved, service.ResolveEffective([first, second], SiteId, SitePosServerId, EffectiveAt).Status);
    }

    [Fact]
    public void BirAndPtuDatesRemainDistinctAndInvalidBirOrderingFails()
    {
        var profile = ValidProfile();
        Assert.NotEqual(profile.BirAccreditationIssuedDate, profile.BirAccreditationValidUntil);
        Assert.NotEqual(profile.BirAccreditationIssuedDate, profile.PtuIssuedDate);

        var invalid = profile with { BirAccreditationValidUntil = profile.BirAccreditationIssuedDate!.Value.AddDays(-1) };
        var result = service.EvaluateCompleteness(invalid, SiteId, SitePosServerId, EffectiveAt);

        Assert.False(result.IsComplete);
        Assert.Contains("bir_accreditation_valid_until_before_issued_date", result.FailureCodes);
    }

    [Fact]
    public void IncompleteProfileReportsAllMissingFieldsSafely()
    {
        var incomplete = ValidProfile() with
        {
            FiscalIdentity = ValidIdentity() with
            {
                RegisteredBusinessName = "",
                RegisteredBusinessAddress = "",
                Tin = ""
            },
            PosSerialNumber = "",
            MachineIdentificationNumber = "",
            BirAccreditationNumber = "",
            BirAccreditationIssuedDate = null,
            BirAccreditationValidUntil = null,
            PtuNumber = "",
            PtuIssuedDate = null,
            ParkingLocationDisplay = "",
            TemplateVersion = "unsupported",
            PresentationVersion = "unsupported"
        };

        var result = service.EvaluateCompleteness(incomplete, SiteId, SitePosServerId, EffectiveAt);

        Assert.False(result.IsComplete);
        Assert.Contains("registered_business_name_missing", result.FailureCodes);
        Assert.Contains("registered_business_address_missing", result.FailureCodes);
        Assert.Contains("tin_missing", result.FailureCodes);
        Assert.Contains("pos_serial_number_missing", result.FailureCodes);
        Assert.Contains("machine_identification_number_missing", result.FailureCodes);
        Assert.Contains("bir_accreditation_number_missing", result.FailureCodes);
        Assert.Contains("bir_accreditation_issued_date_missing", result.FailureCodes);
        Assert.Contains("bir_accreditation_valid_until_missing", result.FailureCodes);
        Assert.Contains("ptu_number_missing", result.FailureCodes);
        Assert.Contains("ptu_issued_date_missing", result.FailureCodes);
        Assert.Contains("parking_location_display_missing", result.FailureCodes);
        Assert.Contains("unsupported_template_version", result.FailureCodes);
        Assert.Contains("unsupported_presentation_version", result.FailureCodes);
    }

    [Fact]
    public void SnapshotIncludesTaxpayerBirPtuSiteVersionsAndRuntimeTerminal()
    {
        var snapshot = service.CreateSnapshot(ValidProfile(), "RUNTIME-TERMINAL-001", EffectiveAt, EffectiveAt.AddSeconds(1));

        Assert.Equal("GOVERNED TEST BUSINESS NAME", snapshot.RegisteredBusinessName);
        Assert.Equal("TEST-BIR-ACCREDITATION-0001", snapshot.BirAccreditationNumber);
        Assert.Equal(new DateOnly(2026, 1, 15), snapshot.BirAccreditationIssuedDate);
        Assert.Equal(new DateOnly(2027, 1, 15), snapshot.BirAccreditationValidUntil);
        Assert.Equal("TEST-PTU-0001", snapshot.PtuNumber);
        Assert.Equal(new DateOnly(2026, 2, 10), snapshot.PtuIssuedDate);
        Assert.Equal("RUNTIME-TERMINAL-001", snapshot.TerminalId);
        Assert.Equal(SalesInvoiceHeaderProfileVersions.TemplateVersion, snapshot.TemplateVersion);
        Assert.Equal(SalesInvoiceHeaderProfileVersions.PresentationVersion, snapshot.PresentationVersion);
    }

    [Fact]
    public void SnapshotIsImmutableWhenProfileIsRetiredOrSupersededLater()
    {
        var original = ValidProfile();
        var oldSnapshot = service.CreateSnapshot(original, "RUNTIME-TERMINAL-001", EffectiveAt, EffectiveAt);
        var newProfile = ValidProfile(Guid.Parse("cccccccc-0000-0000-0000-000000000002"), "v2") with
        {
            ParkingLocationDisplay = "NEW TEST PARKING LOCATION",
            EffectiveFrom = EffectiveAt.AddHours(1)
        };
        var newSnapshot = service.CreateSnapshot(newProfile, "RUNTIME-TERMINAL-002", EffectiveAt.AddHours(1), EffectiveAt.AddHours(1));

        Assert.Equal("GOVERNED TEST PARKING LOCATION", oldSnapshot.ParkingLocationDisplay);
        Assert.Equal("NEW TEST PARKING LOCATION", newSnapshot.ParkingLocationDisplay);
        Assert.Equal("v1", oldSnapshot.ProfileVersion);
        Assert.Equal("v2", newSnapshot.ProfileVersion);
    }

    [Fact]
    public void ProfileModelDoesNotExposePrintExitGateCentralPmsOrAptBehavior()
    {
        var forbiddenNames = new[] { "Print", "Exit", "Gate", "CentralPmsClient", "AssistedPaymentTerminal", "AptPlaceholder" };
        foreach (var type in new[] { typeof(SalesInvoiceHeaderProfile), typeof(SalesInvoiceHeaderSnapshot), typeof(FiscalIdentityProfile) })
        {
            foreach (var member in type.GetMembers())
            {
                foreach (var forbiddenName in forbiddenNames)
                {
                    Assert.DoesNotContain(forbiddenName, member.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static FiscalIdentityProfile ValidIdentity() =>
        new(
            Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            "GOVERNED TEST BUSINESS NAME",
            "GOVERNED TEST ADDRESS",
            "TEST-TIN-0001",
            "VAT_REGISTERED_TEST",
            SalesInvoiceHeaderProfileLifecycle.Approved,
            EffectiveAt.AddDays(-10),
            EffectiveAt.AddDays(-10),
            "profile-test",
            "profile-test");

    private static SalesInvoiceHeaderProfile ValidProfile(
        Guid? profileId = null,
        string profileVersion = "v1") =>
        new(
            profileId ?? Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            ValidIdentity().FiscalIdentityId,
            SiteId,
            SitePosServerId,
            profileVersion,
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
            EffectiveAt.AddDays(-1),
            null,
            SalesInvoiceHeaderProfileLifecycle.Approved,
            EffectiveAt.AddDays(-1),
            "approver-test",
            null,
            EffectiveAt.AddDays(-2),
            EffectiveAt.AddDays(-1),
            "profile-test",
            "approver-test",
            ValidIdentity());
}
