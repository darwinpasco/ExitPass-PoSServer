using System.Runtime.CompilerServices;
using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class SalesInvoiceHeaderProfileAdminEndpointTests
{
    private static readonly Guid SiteId = Guid.Parse("aaaaaaaa-2000-0000-0000-000000000001");
    private static readonly Guid SitePosServerId = Guid.Parse("bbbbbbbb-2000-0000-0000-000000000001");
    private static readonly DateTimeOffset EffectiveAt = DateTimeOffset.Parse("2026-07-18T08:00:00Z");

    [Fact]
    public void AdminRoutesRequireDedicatedPolicyAndAvoidExternalWorkflowSurface()
    {
        var source = File.ReadAllText(FindRouteSourcePath());
        var adminSource = source[source.IndexOf("MapSalesInvoiceHeaderProfileAdminEndpoints", StringComparison.Ordinal)..];

        Assert.Contains("/v1/admin/fiscal-identities", adminSource, StringComparison.Ordinal);
        Assert.Contains("/v1/admin/sales-invoice-header-profiles", adminSource, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName)", adminSource, StringComparison.Ordinal);
        Assert.Contains("/{salesInvoiceHeaderProfileId:guid}/validate", adminSource, StringComparison.Ordinal);
        Assert.Contains("/{salesInvoiceHeaderProfileId:guid}/approve", adminSource, StringComparison.Ordinal);
        Assert.Contains("/{salesInvoiceHeaderProfileId:guid}/retire", adminSource, StringComparison.Ordinal);
        Assert.Contains("/effective-readiness", adminSource, StringComparison.Ordinal);
        Assert.Contains("/{salesInvoiceHeaderProfileId:guid}/usage", adminSource, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", adminSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", adminSource, StringComparison.Ordinal);
        Assert.DoesNotContain("print", adminSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("apt", adminSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("centralpms", adminSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exitauthorization", adminSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gate", adminSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AuthorizationPolicyRequiresDedicatedPermission()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POS_SALES_INVOICE_HEADER_PROFILE_ADMIN_API_KEY"] = "test-admin-key"
            })
            .Build();

        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName));
        Assert.Equal("PosServerAdminApiKey", SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme);
        Assert.Equal("sales_invoice_header_profile.admin", SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission);
        Assert.Equal("X-PosServer-Admin-Key", SalesInvoiceHeaderProfileAdminAuthorization.ApiKeyHeaderName);
        Assert.Equal("X-PosServer-Admin-Permission", SalesInvoiceHeaderProfileAdminAuthorization.PermissionHeaderName);
    }

    [Fact]
    public void AuthenticationHandlerRejectsUnauthenticatedAndUnauthorizedPrincipalsByDesign()
    {
        var source = File.ReadAllText(FindAuthorizationSourcePath());

        Assert.Contains("AuthenticateResult.NoResult()", source, StringComparison.Ordinal);
        Assert.Contains("AuthenticateResult.Fail(\"Invalid POS Server profile administration API credential.", source, StringComparison.Ordinal);
        Assert.Contains("RequireClaim(", File.ReadAllText(FindServiceCollectionSourcePath()), StringComparison.Ordinal);
        Assert.Contains("RequiredPermission", File.ReadAllText(FindServiceCollectionSourcePath()), StringComparison.Ordinal);
    }

    [Fact]
    public void AdminApiKeyMappingDerivesPermissionFromTrustedConfiguration()
    {
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("test-admin-key"),
            StringValues.Empty);

        Assert.True(authentication.Succeeded);
        Assert.Equal("management-platform-admin", authentication.PrincipalName);
        Assert.Contains(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission, authentication.Permissions);
    }

    [Fact]
    public void NonAdminApiKeyCannotSelfAssertAdminPermissionHeader()
    {
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("test-readiness-key"),
            new StringValues(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission));

        Assert.True(authentication.Succeeded);
        Assert.Equal("readiness-probe", authentication.PrincipalName);
        Assert.DoesNotContain(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission, authentication.Permissions);
    }

    [Fact]
    public async Task AuthorizationPolicyAcceptsOnlyServerDerivedAdminPermission()
    {
        var services = new ServiceCollection();
        var configuration = AdminApiKeyConfiguration();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value
            .GetPolicy(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName);
        Assert.NotNull(policy);

        var adminAuthentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            new StringValues("test-admin-key"),
            StringValues.Empty);
        var forgedNonAdminAuthentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            new StringValues("test-readiness-key"),
            new StringValues(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission));

        Assert.True((await authorization.AuthorizeAsync(ToPrincipal(adminAuthentication), null, policy)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(ToPrincipal(forgedNonAdminAuthentication), null, policy)).Succeeded);
    }

    [Fact]
    public void RandomCallerCannotSelfAssertAdminPermissionHeader()
    {
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("random-caller-key"),
            new StringValues(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission));

        Assert.False(authentication.Succeeded);
        Assert.Equal(PosServerAdminApiKeyAuthenticationStatus.InvalidCredential, authentication.Status);
    }

    [Fact]
    public void PermissionHeaderIsOptionalAndCannotOverrideServerDerivedPermission()
    {
        var adminWithMatchingRequestedPermission = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("test-admin-key"),
            new StringValues(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission));
        var adminWithMalformedRequestedPermission = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("test-admin-key"),
            new StringValues($"read,{SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission}"));
        var adminWithDuplicateRequestedPermissionHeaders = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            AdminApiKeyConfiguration(),
            new StringValues("test-admin-key"),
            new StringValues([
                "readiness.check",
                SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission
            ]));

        Assert.Contains(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission, adminWithMatchingRequestedPermission.Permissions);
        Assert.True(adminWithMalformedRequestedPermission.Succeeded);
        Assert.DoesNotContain(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission, adminWithMalformedRequestedPermission.Permissions);
        Assert.True(adminWithDuplicateRequestedPermissionHeaders.Succeeded);
        Assert.DoesNotContain(SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission, adminWithDuplicateRequestedPermissionHeaders.Permissions);
    }

    [Fact]
    public void MissingInvalidDisabledAndMalformedCredentialsFailSafely()
    {
        var configuration = AdminApiKeyConfiguration();
        var missing = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, StringValues.Empty, StringValues.Empty);
        var invalid = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("invalid-key"), StringValues.Empty);
        var disabled = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("test-disabled-key"), StringValues.Empty);
        var duplicatedKeyHeaders = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            new StringValues(["test-admin-key", "test-readiness-key"]),
            StringValues.Empty);

        Assert.Equal(PosServerAdminApiKeyAuthenticationStatus.MissingCredential, missing.Status);
        Assert.Equal(PosServerAdminApiKeyAuthenticationStatus.InvalidCredential, invalid.Status);
        Assert.Equal(PosServerAdminApiKeyAuthenticationStatus.DisabledCredential, disabled.Status);
        Assert.Equal(PosServerAdminApiKeyAuthenticationStatus.MissingCredential, duplicatedKeyHeaders.Status);
    }

    [Fact]
    public void ApiKeyConfigurationRejectsEmptySecretsAndDuplicateIdentities()
    {
        var emptySecret = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PosServer:Admin:ApiKeys:0:Principal"] = "management-platform-admin",
                ["PosServer:Admin:ApiKeys:0:Key"] = "",
                ["PosServer:Admin:ApiKeys:0:Permissions:0"] = SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission
            })
            .Build();
        var duplicateIdentity = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PosServer:Admin:ApiKeys:0:Principal"] = "management-platform-admin",
                ["PosServer:Admin:ApiKeys:0:Key"] = "first-key",
                ["PosServer:Admin:ApiKeys:0:Permissions:0"] = SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission,
                ["PosServer:Admin:ApiKeys:1:Principal"] = "management-platform-admin",
                ["PosServer:Admin:ApiKeys:1:Key"] = "second-key",
                ["PosServer:Admin:ApiKeys:1:Permissions:0"] = "readiness.check"
            })
            .Build();
        var missingProductionConfiguration = new ConfigurationBuilder().Build();

        Assert.False(SalesInvoiceHeaderProfileAdminAuthorization.ResolveConfiguredApiKeys(emptySecret).IsValid);
        Assert.False(SalesInvoiceHeaderProfileAdminAuthorization.ResolveConfiguredApiKeys(duplicateIdentity).IsValid);
        Assert.False(SalesInvoiceHeaderProfileAdminAuthorization.ResolveConfiguredApiKeys(missingProductionConfiguration).IsValid);
    }

    [Fact]
    public void PermissionClaimIsNotConstructedFromUntrustedRequestHeader()
    {
        var source = File.ReadAllText(FindAuthorizationSourcePath());

        Assert.Contains("ResolveServerDerivedPermissions", source, StringComparison.Ordinal);
        Assert.Contains("CryptographicOperations.FixedTimeEquals", source, StringComparison.Ordinal);
        Assert.DoesNotContain("suppliedPermission", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, requestedPermission", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, Request.Headers", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingCorrelationIdIsRejectedSafely()
    {
        var service = new SalesInvoiceHeaderProfileAdminService(new InMemoryProfileRepository());

        var response = await SalesInvoiceHeaderProfileAdminEndpoint.CreateFiscalIdentityAsync(
            ValidIdentityRequest(),
            service,
            new DefaultHttpContext());

        Assert.False(response.Succeeded);
        Assert.Equal("missing_correlation_id", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task FiscalIdentityCanBeCreatedReadAndUpdatedWithCorrelation()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var context = Context("corr-admin-001");

        var created = await SalesInvoiceHeaderProfileAdminEndpoint.CreateFiscalIdentityAsync(ValidIdentityRequest(), service, context);
        var identity = Assert.IsType<FiscalIdentityProfile>(created.Resource);
        var read = await SalesInvoiceHeaderProfileAdminEndpoint.GetFiscalIdentityAsync(identity.FiscalIdentityId, service, context);
        var updated = await SalesInvoiceHeaderProfileAdminEndpoint.UpdateFiscalIdentityAsync(
            identity.FiscalIdentityId,
            ValidIdentityUpdateRequest() with { RegisteredBusinessName = "GOVERNED TEST BUSINESS NAME UPDATED" },
            service,
            context);

        Assert.True(created.Succeeded);
        Assert.Equal("corr-admin-001", created.CorrelationId);
        Assert.True(read.Succeeded);
        Assert.True(updated.Succeeded);
        Assert.Equal("GOVERNED TEST BUSINESS NAME UPDATED", Assert.IsType<FiscalIdentityProfile>(updated.Resource).RegisteredBusinessName);
    }

    [Fact]
    public async Task ProfileLifecycleReadinessUsageAndImmutableSnapshotVisibilityAreExposed()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository, enforcementRequired: true);
        var context = Context("corr-admin-002");
        var identity = Assert.IsType<FiscalIdentityProfile>((await SalesInvoiceHeaderProfileAdminEndpoint.CreateFiscalIdentityAsync(ValidIdentityRequest(), service, context)).Resource);
        var draftResponse = await SalesInvoiceHeaderProfileAdminEndpoint.CreateHeaderProfileAsync(ValidProfileRequest(identity.FiscalIdentityId) with
        {
            PosSerialNumber = null
        }, service, context);
        var draft = Assert.IsType<SalesInvoiceHeaderProfile>(draftResponse.Resource);

        var validation = Assert.IsType<ValidateSalesInvoiceHeaderProfileResult>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.ValidateHeaderProfileAsync(draft.SalesInvoiceHeaderProfileId, service, context)).Resource);
        Assert.False(validation.IsComplete);
        Assert.Contains("pos_serial_number_missing", validation.FailureCodes);

        var updated = Assert.IsType<SalesInvoiceHeaderProfile>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.UpdateHeaderProfileDraftAsync(
                draft.SalesInvoiceHeaderProfileId,
                ValidProfileRequest(identity.FiscalIdentityId) with { UpdatedByRef = "admin-updater" },
                service,
                context)).Resource);
        Assert.Equal("TEST-SERIAL-0001", updated.PosSerialNumber);
        Assert.Equal("GOVERNED TEST SOFTWARE SUPPLIER", updated.SupplierDeveloperRegisteredName);
        Assert.Equal("GOVERNED TEST SOFTWARE ADDRESS", updated.SupplierDeveloperAddress);
        Assert.Equal("TEST-SUPPLIER-TIN-0001", updated.SupplierDeveloperTin);

        var approved = Assert.IsType<SalesInvoiceHeaderProfile>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.ApproveHeaderProfileAsync(
                updated.SalesInvoiceHeaderProfileId,
                new ApproveSalesInvoiceHeaderProfileRequest("admin-approver", EffectiveAt),
                service,
                context)).Resource);
        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Approved, approved.LifecycleStatus);
        Assert.Equal("admin-approver", approved.ApprovedByRef);

        var readiness = Assert.IsType<SalesInvoiceHeaderProfileReadiness>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.GetEffectiveReadinessAsync(SiteId, SitePosServerId, EffectiveAt.AddMinutes(1), service, context)).Resource);
        Assert.Equal("READY", readiness.ResolutionStatus);
        Assert.True(readiness.EnforcementRequired);
        Assert.Equal("currently_valid", readiness.BirAccreditationPosture);
        Assert.Equal("complete", readiness.PtuPosture);

        repository.AddUsage(approved.SalesInvoiceHeaderProfileId, Guid.Parse("ffffffff-2000-0000-0000-000000000001"), EffectiveAt.AddMinutes(2));
        var usage = Assert.IsType<SalesInvoiceHeaderProfileUsage>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.GetHeaderProfileUsageAsync(approved.SalesInvoiceHeaderProfileId, service, context)).Resource);
        Assert.Equal(1, usage.FiscalDocumentCount);
        Assert.True(usage.DestructiveMutationBlocked);

        var retired = Assert.IsType<SalesInvoiceHeaderProfile>(
            (await SalesInvoiceHeaderProfileAdminEndpoint.RetireHeaderProfileAsync(
                approved.SalesInvoiceHeaderProfileId,
                new RetireSalesInvoiceHeaderProfileRequest("admin-retirer", EffectiveAt.AddHours(1)),
                service,
                context)).Resource);
        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Retired, retired.LifecycleStatus);
    }

    [Fact]
    public async Task ApprovalOverlapReturnsConflictAndLeavesProfileDraft()
    {
        var repository = new InMemoryProfileRepository();
        var service = new SalesInvoiceHeaderProfileAdminService(repository);
        var context = Context("corr-admin-003");
        var identity = Assert.IsType<FiscalIdentityProfile>((await SalesInvoiceHeaderProfileAdminEndpoint.CreateFiscalIdentityAsync(ValidIdentityRequest(), service, context)).Resource);
        var first = Assert.IsType<SalesInvoiceHeaderProfile>((await SalesInvoiceHeaderProfileAdminEndpoint.CreateHeaderProfileAsync(ValidProfileRequest(identity.FiscalIdentityId), service, context)).Resource);
        await SalesInvoiceHeaderProfileAdminEndpoint.ApproveHeaderProfileAsync(first.SalesInvoiceHeaderProfileId, new("admin-approver", EffectiveAt), service, context);
        var second = Assert.IsType<SalesInvoiceHeaderProfile>((await SalesInvoiceHeaderProfileAdminEndpoint.CreateHeaderProfileAsync(ValidProfileRequest(identity.FiscalIdentityId) with { ProfileVersion = "v2" }, service, context)).Resource);

        var conflict = await SalesInvoiceHeaderProfileAdminEndpoint.ApproveHeaderProfileAsync(second.SalesInvoiceHeaderProfileId, new("admin-approver", EffectiveAt), service, context);

        Assert.False(conflict.Succeeded);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.HttpStatusCode);
        Assert.Equal("overlapping_approved_effective_window", conflict.Code);
        Assert.Equal(SalesInvoiceHeaderProfileLifecycle.Draft, (await service.GetHeaderProfileAsync(second.SalesInvoiceHeaderProfileId))!.LifecycleStatus);
    }

    private static DefaultHttpContext Context(string correlationId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = correlationId;
        return context;
    }

    private static CreateFiscalIdentityRequest ValidIdentityRequest() =>
        new(
            "GOVERNED TEST BUSINESS NAME",
            "GOVERNED TEST ADDRESS",
            "TEST-TIN-0001",
            "VAT_REGISTERED_TEST",
            SalesInvoiceHeaderProfileLifecycle.Draft,
            "admin-creator");

    private static UpdateFiscalIdentityRequest ValidIdentityUpdateRequest() =>
        new(
            "GOVERNED TEST BUSINESS NAME",
            "GOVERNED TEST ADDRESS",
            "TEST-TIN-0001",
            "VAT_REGISTERED_TEST",
            SalesInvoiceHeaderProfileLifecycle.Draft,
            "admin-updater");

    private static CreateSalesInvoiceHeaderProfileRequest ValidProfileRequest(Guid identityId) =>
        new(
            identityId,
            SiteId,
            SitePosServerId,
            "v1",
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
            "admin-creator",
            SupplierDeveloperRegisteredName: "GOVERNED TEST SOFTWARE SUPPLIER",
            SupplierDeveloperAddress: "GOVERNED TEST SOFTWARE ADDRESS",
            SupplierDeveloperTin: "TEST-SUPPLIER-TIN-0001");

    private static IConfiguration AdminApiKeyConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PosServer:Admin:ApiKeys:0:Principal"] = "management-platform-admin",
                ["PosServer:Admin:ApiKeys:0:Key"] = "test-admin-key",
                ["PosServer:Admin:ApiKeys:0:Permissions:0"] = SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission,
                ["PosServer:Admin:ApiKeys:0:Enabled"] = "true",
                ["PosServer:Admin:ApiKeys:1:Principal"] = "readiness-probe",
                ["PosServer:Admin:ApiKeys:1:Key"] = "test-readiness-key",
                ["PosServer:Admin:ApiKeys:1:Permissions:0"] = "readiness.check",
                ["PosServer:Admin:ApiKeys:1:Enabled"] = "true",
                ["PosServer:Admin:ApiKeys:2:Principal"] = "disabled-admin",
                ["PosServer:Admin:ApiKeys:2:Key"] = "test-disabled-key",
                ["PosServer:Admin:ApiKeys:2:Permissions:0"] = SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission,
                ["PosServer:Admin:ApiKeys:2:Enabled"] = "false"
            })
            .Build();

    private static ClaimsPrincipal ToPrincipal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authentication.PrincipalName ?? "unauthenticated")
        };
        claims.AddRange(authentication.Permissions.Select(permission =>
            new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private static string FindRouteSourcePath([CallerFilePath] string testFilePath = "") =>
        FindSourcePath(testFilePath, "src", "ExitPass.PosServer.Api", "FiscalDocuments", "FiscalDocumentEndpointRouteBuilderExtensions.cs");

    private static string FindAuthorizationSourcePath([CallerFilePath] string testFilePath = "") =>
        FindSourcePath(testFilePath, "src", "ExitPass.PosServer.Api", "FiscalDocuments", "SalesInvoiceHeaderProfileAdminAuthorization.cs");

    private static string FindServiceCollectionSourcePath([CallerFilePath] string testFilePath = "") =>
        FindSourcePath(testFilePath, "src", "ExitPass.PosServer.Api", "FiscalDocuments", "FiscalDocumentServiceCollectionExtensions.cs");

    private static string FindSourcePath(string start, params string[] segments)
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(start) ?? string.Empty);
        while (current is not null)
        {
            var candidate = Path.Combine(new[] { current.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate {segments[^1]}.");
    }

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
            Task.FromResult(false);

        public Task<SalesInvoiceHeaderProfile> CreateHeaderProfileAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken)
        {
            profiles.Add(profile.SalesInvoiceHeaderProfileId, Hydrate(profile));
            return Task.FromResult(profiles[profile.SalesInvoiceHeaderProfileId]);
        }

        public Task<SalesInvoiceHeaderProfile?> GetHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken) =>
            Task.FromResult(profiles.GetValueOrDefault(salesInvoiceHeaderProfileId));

        public Task<IReadOnlyList<SalesInvoiceHeaderProfile>> ListHeaderProfilesAsync(Guid? siteId, Guid? sitePosServerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SalesInvoiceHeaderProfile>>(profiles.Values
                .Where(profile => siteId is null || profile.SiteId == siteId)
                .Where(profile => sitePosServerId is null || profile.SitePosServerId == sitePosServerId)
                .ToArray());

        public Task<SalesInvoiceHeaderProfile> UpdateHeaderProfileDraftAsync(SalesInvoiceHeaderProfile profile, CancellationToken cancellationToken)
        {
            profiles[profile.SalesInvoiceHeaderProfileId] = Hydrate(profile);
            return Task.FromResult(profiles[profile.SalesInvoiceHeaderProfileId]);
        }

        public Task<SalesInvoiceHeaderProfile> ApproveHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset approvedAt, string approvedByRef, CancellationToken cancellationToken)
        {
            var approved = profiles[salesInvoiceHeaderProfileId] with
            {
                LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Approved,
                ApprovedAt = approvedAt,
                ApprovedByRef = approvedByRef,
                UpdatedAt = approvedAt,
                UpdatedByRef = approvedByRef
            };
            profiles[salesInvoiceHeaderProfileId] = approved;
            return Task.FromResult(approved);
        }

        public Task<SalesInvoiceHeaderProfile> RetireHeaderProfileAsync(Guid salesInvoiceHeaderProfileId, DateTimeOffset retiredAt, string retiredByRef, CancellationToken cancellationToken)
        {
            var retired = profiles[salesInvoiceHeaderProfileId] with
            {
                LifecycleStatus = SalesInvoiceHeaderProfileLifecycle.Retired,
                RetiredAt = retiredAt,
                EffectiveTo = retiredAt,
                UpdatedAt = retiredAt,
                UpdatedByRef = retiredByRef
            };
            profiles[salesInvoiceHeaderProfileId] = retired;
            return Task.FromResult(retired);
        }

        public Task<SalesInvoiceHeaderProfileResolutionResult> ResolveEffectiveSalesInvoiceHeaderProfileAsync(Guid siteId, Guid sitePosServerId, DateTimeOffset effectiveAt, CancellationToken cancellationToken) =>
            Task.FromResult(new SalesInvoiceHeaderProfileService().ResolveEffective(profiles.Values, siteId, sitePosServerId, effectiveAt));

        public Task<SalesInvoiceHeaderProfileUsage> GetHeaderProfileUsageAsync(Guid salesInvoiceHeaderProfileId, CancellationToken cancellationToken)
        {
            var rows = usage.Where(row => row.ProfileId == salesInvoiceHeaderProfileId).OrderBy(row => row.SnapshotCreatedAt).ToArray();
            var profile = profiles.GetValueOrDefault(salesInvoiceHeaderProfileId);
            return Task.FromResult(new SalesInvoiceHeaderProfileUsage(
                salesInvoiceHeaderProfileId,
                profile?.ProfileVersion,
                profile?.FiscalIdentityId,
                rows.Length == 0 ? null : rows.First().SnapshotCreatedAt,
                rows.Length == 0 ? null : rows.Last().SnapshotCreatedAt,
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
