using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class FiscalZCloseStateInitializationApiTests
{
    private static readonly Guid SiteId = Guid.Parse("75000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("75000000-0000-4000-8000-000000000002");

    [Fact]
    public async Task PermissionAndScopeAreDerivedOnlyFromTrustedServerConfiguration()
    {
        var configuration = Configuration();
        var authorized = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, new StringValues("state-admin-key"), StringValues.Empty);
        var ordinary = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, new StringValues("ordinary-key"),
            new StringValues(FiscalZCloseStateInitializationAuthorization.Permission));

        Assert.Contains(FiscalZCloseStateInitializationAuthorization.Permission, authorized.Permissions);
        Assert.DoesNotContain(FiscalZCloseStateInitializationAuthorization.Permission, ordinary.Permissions);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value
            .GetPolicy(FiscalZCloseStateInitializationAuthorization.PolicyName)!;
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(authorized), null, policy)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(ordinary), null, policy)).Succeeded);
        Assert.True(FiscalZCloseStateInitializationAuthorization.IsInScope(Principal(authorized), SiteId, IdentityId));
        Assert.False(FiscalZCloseStateInitializationAuthorization.IsInScope(Principal(authorized), Guid.NewGuid(), IdentityId));
    }

    [Fact]
    public async Task EndpointRejectsWrongScopeBeforeRepositoryMutation()
    {
        var repository = new StubRepository();
        var service = new FiscalZCloseStateInitializationService(repository);
        var context = Context(Guid.NewGuid());

        var response = await FiscalZCloseStateInitializationEndpoint.InitializeAsync(Request(), service, context);

        Assert.Equal(StatusCodes.Status403Forbidden, response.HttpStatusCode);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task EndpointReturnsSafeInitializedResultForAuthorizedScope()
    {
        var repository = new StubRepository();
        var response = await FiscalZCloseStateInitializationEndpoint.InitializeAsync(
            Request(), new FiscalZCloseStateInitializationService(repository), Context(SiteId));
        var serialized = System.Text.Json.JsonSerializer.Serialize(response);

        Assert.True(response.Succeeded);
        Assert.Equal(StatusCodes.Status201Created, response.HttpStatusCode);
        Assert.Equal("initialized", response.ResultClassification);
        Assert.DoesNotContain("semantic", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static InitializeFiscalZCloseStateRequest Request() => new(
        "state-init-operation", SiteId, IdentityId, "PHP", "approved_new_scope_zero",
        0, 0, 0, "approved-design-authority-20260804");

    private static DefaultHttpContext Context(Guid siteScope)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "safe-correlation";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "z-state-admin-service"),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, siteScope.ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, IdentityId.ToString("D"))
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
        return context;
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PosServer:Admin:ApiKeys:0:Principal"] = "z-state-admin-service",
        ["PosServer:Admin:ApiKeys:0:Key"] = "state-admin-key",
        ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalZCloseStateInitializationAuthorization.Permission,
        ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SiteId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
        ["PosServer:Admin:ApiKeys:1:Principal"] = "ordinary-service",
        ["PosServer:Admin:ApiKeys:1:Key"] = "ordinary-key",
        ["PosServer:Admin:ApiKeys:1:Permissions:0"] = "fiscal_document.create"
    }).Build();

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!) };
        claims.AddRange(authentication.Permissions.Select(value => new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value => new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value => new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private sealed class StubRepository : IFiscalZCloseStateRepository
    {
        public int Calls { get; private set; }
        public Task<FiscalZCloseStateInitializationResult> InitializeAsync(
            InitializeFiscalZCloseStateCommand command,
            string semanticRequestHash,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(FiscalZCloseStateInitializationResult.Initialized(new(
                Guid.NewGuid(), command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode,
                FiscalZCloseStateContract.ContractVersion, command.ResetCounterValue, command.ZCounterValue,
                command.GrandTotalAmountMinorUnits, 1, null, null, command.Provenance,
                DateTimeOffset.UtcNow, command.ApprovalReference, command.OperationReference, DateTimeOffset.UtcNow)));
        }
    }
}
