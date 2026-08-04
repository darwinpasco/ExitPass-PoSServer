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

public sealed class FiscalZReadingApiTests
{
    private static readonly Guid SiteId = Guid.Parse("79000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("79000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("79000000-0000-4000-8000-000000000003");

    [Fact]
    public void RoutesAreBoundedAndSeparatelyAuthorized()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Api", "FiscalReports", "FiscalZReadingEndpointRouteBuilderExtensions.cs"));
        Assert.Contains("/v1/fiscal-reports/z-readings", source, StringComparison.Ordinal);
        Assert.Contains("ClosePolicyName", source, StringComparison.Ordinal);
        Assert.Contains("ReadPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("render", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("export", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DedicatedPoliciesAndThreeDimensionalScopeAreServerDerived()
    {
        var configuration = Configuration();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("z-close-key"), StringValues.Empty);
        Assert.True(authentication.Succeeded);
        Assert.Contains(FiscalZReadingAuthorization.ClosePermission, authentication.Permissions);
        Assert.Contains("PHP", authentication.CurrencyScopes);

        var services = new ServiceCollection(); services.AddLogging(); services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = Principal(authentication);
        Assert.True((await authorization.AuthorizeAsync(principal, null, options.GetPolicy(FiscalZReadingAuthorization.ClosePolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, options.GetPolicy(FiscalZReadingAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.True(FiscalZReadingAuthorization.IsInScope(principal, SiteId, IdentityId, "PHP"));
        Assert.False(FiscalZReadingAuthorization.IsInScope(principal, SiteId, IdentityId, "USD"));
    }

    [Theory]
    [InlineData("ordinary-key")]
    [InlineData("x-key")]
    [InlineData("initialize-key")]
    [InlineData("apt-key")]
    [InlineData("webpay-key")]
    public async Task NonClosePermissionsCannotClose(string key)
    {
        var configuration = Configuration();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, new StringValues(key), new StringValues(FiscalZReadingAuthorization.ClosePermission));
        Assert.True(authentication.Succeeded);
        Assert.DoesNotContain(FiscalZReadingAuthorization.ClosePermission, authentication.Permissions);
        var services = new ServiceCollection(); services.AddLogging(); services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy(FiscalZReadingAuthorization.ClosePolicyName)!;
        Assert.False((await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(Principal(authentication), null, policy)).Succeeded);
    }

    [Fact]
    public async Task ScopeDenialOccursBeforeRepositoryMutation()
    {
        var repository = new StubRepository(new(FiscalZReadingOutcome.Created, Record()));
        var service = new FiscalZReadingService(repository);
        var response = await FiscalZReadingEndpoint.CloseAsync(
            new("z-operation", SiteId, IdentityId, "USD", PeriodId, 1), service, Context());
        Assert.Equal(StatusCodes.Status403Forbidden, response.HttpStatusCode);
        Assert.Equal(0, repository.CloseCalls);
    }

    [Fact]
    public async Task CloseReplayConflictAndReadbackMapToSafeApiResults()
    {
        var record = Record();
        var repository = new StubRepository(new(FiscalZReadingOutcome.Created, record));
        var service = new FiscalZReadingService(repository);
        var created = await FiscalZReadingEndpoint.CloseAsync(new("z-operation", SiteId, IdentityId, "PHP", PeriodId, 1), service, Context());
        Assert.Equal(StatusCodes.Status201Created, created.HttpStatusCode);
        Assert.Same(record, created.ZReading);

        repository.Result = new(FiscalZReadingOutcome.Conflict, SafeMessage: "The operation is already bound.");
        var conflict = await FiscalZReadingEndpoint.CloseAsync(new("z-operation", SiteId, IdentityId, "PHP", PeriodId, 1), service, Context());
        var serialized = System.Text.Json.JsonSerializer.Serialize(conflict);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.HttpStatusCode);
        Assert.DoesNotContain("hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", serialized, StringComparison.OrdinalIgnoreCase);

        repository.Result = new(FiscalZReadingOutcome.Replayed, record);
        var read = await FiscalZReadingEndpoint.GetAsync(record.FiscalReportReference, service, Context());
        Assert.Equal(StatusCodes.Status200OK, read.HttpStatusCode);
        Assert.Same(record, read.ZReading);
    }

    private static IConfiguration Configuration()
    {
        var values = new Dictionary<string, string?>
        {
            ["PosServer:Admin:ApiKeys:0:Principal"]="z-close-service",["PosServer:Admin:ApiKeys:0:Key"]="z-close-key",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"]=FiscalZReadingAuthorization.ClosePermission,["PosServer:Admin:ApiKeys:0:Permissions:1"]=FiscalZReadingAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"]=SiteId.ToString("D"),["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"]=IdentityId.ToString("D"),["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"]="PHP",
            ["PosServer:Admin:ApiKeys:1:Principal"]="ordinary",["PosServer:Admin:ApiKeys:1:Key"]="ordinary-key",["PosServer:Admin:ApiKeys:1:Permissions:0"]="fiscal_document.create",
            ["PosServer:Admin:ApiKeys:2:Principal"]="x",["PosServer:Admin:ApiKeys:2:Key"]="x-key",["PosServer:Admin:ApiKeys:2:Permissions:0"]=FiscalXReadingAuthorization.GeneratePermission,["PosServer:Admin:ApiKeys:2:SitePosServerIds:0"]="*",["PosServer:Admin:ApiKeys:2:FiscalIdentityIds:0"]="*",
            ["PosServer:Admin:ApiKeys:3:Principal"]="initializer",["PosServer:Admin:ApiKeys:3:Key"]="initialize-key",["PosServer:Admin:ApiKeys:3:Permissions:0"]=FiscalZCloseStateInitializationAuthorization.Permission,["PosServer:Admin:ApiKeys:3:SitePosServerIds:0"]="*",["PosServer:Admin:ApiKeys:3:FiscalIdentityIds:0"]="*",
            ["PosServer:Admin:ApiKeys:4:Principal"]="apt",["PosServer:Admin:ApiKeys:4:Key"]="apt-key",["PosServer:Admin:ApiKeys:4:Permissions:0"]="apt.payment.execute",
            ["PosServer:Admin:ApiKeys:5:Principal"]="webpay",["PosServer:Admin:ApiKeys:5:Key"]="webpay-key",["PosServer:Admin:ApiKeys:5:Permissions:0"]="webpay.payment.execute"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!) };
        claims.AddRange(authentication.Permissions.Select(value => new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value => new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value => new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        claims.AddRange(authentication.CurrencyScopes.Select(value => new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext(); context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName]="z-correlation";
        context.User = new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"z-service"),new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType,SiteId.ToString("D")),new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType,IdentityId.ToString("D")),new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType,"PHP")],SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
        return context;
    }

    private static FiscalZReadingRecord Record() => new(Guid.NewGuid(),"Z-TEST",Guid.NewGuid(),"z-operation","Z_READING",FiscalZReadingContract.ContractVersion,FiscalZReadingContract.SemanticHashVersion,SiteId,IdentityId,PeriodId,null,new(2026,8,3),DateTimeOffset.Parse("2026-08-03T00:00:00Z"),DateTimeOffset.Parse("2026-08-04T00:00:00Z"),"Asia/Manila",new(0,0),"PHP",DateTimeOffset.Parse("2026-08-04T01:00:00Z"),DateTimeOffset.Parse("2026-08-04T01:00:00Z"),DateTimeOffset.Parse("2026-08-04T01:00:00Z"),0,new(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0),[],[],[],new(0,0,0,1,0,0,0,1,2),"CLOSED","z-correlation","Z-TEST",true);
    private static string FindSource(params string[] parts) { var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null){var candidate=Path.Combine([directory.FullName,..parts]);if(File.Exists(candidate))return candidate;directory=directory.Parent;}throw new FileNotFoundException(string.Join('/',parts)); }

    private sealed class StubRepository(FiscalZReadingResult result) : IFiscalZReadingRepository
    {
        public FiscalZReadingResult Result { get; set; } = result;
        public int CloseCalls { get; private set; }
        public Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command,CancellationToken cancellationToken=default){CloseCalls++;return Task.FromResult(Result);}
        public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference,CancellationToken cancellationToken=default)=>Task.FromResult(Result);
    }
}
