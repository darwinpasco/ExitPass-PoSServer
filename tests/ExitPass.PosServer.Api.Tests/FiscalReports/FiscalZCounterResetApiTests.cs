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

public sealed class FiscalZCounterResetApiTests
{
    private static readonly Guid SiteId = Guid.Parse("7b000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("7b000000-0000-4000-8000-000000000002");

    [Fact]
    public async Task DedicatedResetPermissionIsRequiredAndScoped()
    {
        var configuration = Configuration();
        var reset = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, new StringValues("reset-key"), new StringValues(FiscalZCounterResetAuthorization.ResetPermission));
        var readOnly = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, new StringValues("read-key"), new StringValues(FiscalZCounterResetAuthorization.ResetPermission));
        Assert.True(reset.Succeeded);
        Assert.True(readOnly.Succeeded);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value
            .GetPolicy(FiscalZCounterResetAuthorization.ResetPolicyName)!;
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(reset), null, policy)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(readOnly), null, policy)).Succeeded);
        Assert.True(FiscalZCounterResetAuthorization.IsInScope(Principal(reset), SiteId, IdentityId, "PHP"));
        Assert.False(FiscalZCounterResetAuthorization.IsInScope(Principal(reset), SiteId, IdentityId, "USD"));
    }

    [Fact]
    public async Task ExplicitConfirmationAndScopeAreCheckedBeforeMutation()
    {
        var repository = new StubRepository();
        var service = new FiscalZCounterResetService(repository);
        var request = new ResetFiscalZCounterRequest(
            "reset-operation", SiteId, IdentityId, "PHP", 31, "BIR demonstration", false);

        var notConfirmed = await FiscalZCounterResetEndpoint.ResetAsync(request, service, Context(), default);
        Assert.Equal(StatusCodes.Status400BadRequest, notConfirmed.HttpStatusCode);
        Assert.Equal("fiscal_z_counter_reset_confirmation_required", notConfirmed.Code);
        Assert.Equal(0, repository.ResetCalls);

        var wrongScope = await FiscalZCounterResetEndpoint.ResetAsync(
            request with { ConfirmReset = true, CurrencyCode = "USD" }, service, Context(), default);
        Assert.Equal(StatusCodes.Status403Forbidden, wrongScope.HttpStatusCode);
        Assert.Equal(0, repository.ResetCalls);
    }

    [Fact]
    public async Task ConfirmedRequestRecordsDelegatedAdministratorAndMapsReplay()
    {
        var repository = new StubRepository
        {
            Result = new(FiscalZCounterResetOutcome.Replayed, Reset: ResetRecord())
        };
        var service = new FiscalZCounterResetService(repository);
        var context = Context();
        context.Request.Headers["X-Actor-Reference"] = "management-admin";

        var response = await FiscalZCounterResetEndpoint.ResetAsync(
            new("reset-operation", SiteId, IdentityId, "PHP", 31, "BIR demonstration", true),
            service, context, default);

        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Equal("fiscal_z_counter_reset_replayed", response.Code);
        Assert.Equal("management-admin", repository.LastCommand!.RequestedByRef);
        Assert.Equal("central-pms-service", repository.LastCommand.ServiceIdentityRef);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?>
        {
            ["PosServer:Admin:ApiKeys:0:Principal"] = "reset-service",
            ["PosServer:Admin:ApiKeys:0:Key"] = "reset-key",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalZCounterResetAuthorization.ResetPermission,
            ["PosServer:Admin:ApiKeys:0:Permissions:1"] = FiscalZCounterResetAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SiteId.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"] = "PHP",
            ["PosServer:Admin:ApiKeys:1:Principal"] = "read-service",
            ["PosServer:Admin:ApiKeys:1:Key"] = "read-key",
            ["PosServer:Admin:ApiKeys:1:Permissions:0"] = FiscalZCounterResetAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:1:SitePosServerIds:0"] = SiteId.ToString("D"),
            ["PosServer:Admin:ApiKeys:1:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
            ["PosServer:Admin:ApiKeys:1:CurrencyCodes:0"] = "PHP"
        }).Build();

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!) };
        claims.AddRange(authentication.Permissions.Select(value =>
            new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value =>
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value =>
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        claims.AddRange(authentication.CurrencyScopes.Select(value =>
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "reset-correlation";
        context.User = new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "central-pms-service"),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, SiteId.ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, IdentityId.ToString("D")),
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, "PHP")
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
        return context;
    }

    private static FiscalZCounterResetRecord ResetRecord() => new(
        Guid.NewGuid(), "reset-operation", SiteId, IdentityId, "PHP", 31, 0, 0, 1,
        123_456, 123_456, 31, 32, "management-admin", "central-pms-service",
        "BIR demonstration", DateTimeOffset.Parse("2026-10-20T08:00:00Z"), "EJ-RESET-001");

    private sealed class StubRepository : IFiscalZCounterResetRepository
    {
        public FiscalZCounterResetResult Result { get; set; } = new(FiscalZCounterResetOutcome.Created);
        public int ResetCalls { get; private set; }
        public FiscalZCounterResetCommand? LastCommand { get; private set; }

        public Task<FiscalZCounterResetResult> ReadStateAsync(
            FiscalZCounterStateQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalZCounterResetResult(FiscalZCounterResetOutcome.Replayed));

        public Task<FiscalZCounterResetResult> ResetAsync(
            FiscalZCounterResetCommand command, CancellationToken cancellationToken = default)
        {
            ResetCalls++;
            LastCommand = command;
            return Task.FromResult(Result);
        }
    }
}
