using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class BirSalesSummaryApiTests
{
    [Fact]
    public async Task DedicatedPoliciesAndScopeAreRequired()
    {
        var configuration = Configuration();
        var services = new ServiceCollection(); services.AddLogging(); services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, "summary-key", default);
        var principal = Principal(authentication);
        Assert.True((await authorization.AuthorizeAsync(principal, null, options.GetPolicy(BirSalesSummaryAuthorization.GeneratePolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, options.GetPolicy(BirSalesSummaryAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, options.GetPolicy(BirSalesSummaryAuthorization.ExportPolicyName)!)).Succeeded);
        Assert.True(BirSalesSummaryAuthorization.IsInScope(principal, SiteId, IdentityId, "PHP"));
        Assert.False(BirSalesSummaryAuthorization.IsInScope(principal, Guid.NewGuid(), IdentityId, "PHP"));
        Assert.False(BirSalesSummaryAuthorization.IsInScope(principal, SiteId, IdentityId, "USD"));
    }

    [Fact]
    public async Task ScopeAndProductionFixtureAuthorityDenialsOccurBeforePersistence()
    {
        var repository = new StubRepository(new(BirSalesSummaryOutcome.Created, Record()));
        var service = new BirSalesSummaryService(repository);
        var wrongScope = await BirSalesSummaryEndpoint.GenerateAsync(Request() with { CurrencyCode = "USD" }, service, Context("PRODUCTION"), new Host("Development"));
        Assert.Equal(403, wrongScope.HttpStatusCode);
        Assert.Equal(0, repository.GenerateCalls);

        var fixture = await BirSalesSummaryEndpoint.GenerateAsync(Request(), service, Context("FIXTURE"), new Host(Environments.Production));
        Assert.Equal(403, fixture.HttpStatusCode);
        Assert.Equal(0, repository.GenerateCalls);
    }

    [Fact]
    public async Task GenerationReplayConflictAndClosedPeriodFailuresMapSafely()
    {
        var repository = new StubRepository(new(BirSalesSummaryOutcome.Created, Record()));
        var service = new BirSalesSummaryService(repository);
        var created = await BirSalesSummaryEndpoint.GenerateAsync(Request(), service, Context("PRODUCTION"), new Host(Environments.Production));
        Assert.Equal(201, created.HttpStatusCode);

        repository.Result = new(BirSalesSummaryOutcome.Replayed, Record());
        Assert.Equal(200, (await BirSalesSummaryEndpoint.GenerateAsync(Request(), service, Context("PRODUCTION"), new Host(Environments.Production))).HttpStatusCode);
        repository.Result = new(BirSalesSummaryOutcome.PeriodNotClosed, SafeMessage: "The period is not closed.");
        Assert.Equal(409, (await BirSalesSummaryEndpoint.GenerateAsync(Request(), service, Context("PRODUCTION"), new Host(Environments.Production))).HttpStatusCode);
        repository.Result = new(BirSalesSummaryOutcome.ReconciliationFailure, SafeMessage: "Reconciliation failed safely.");
        var failure = await BirSalesSummaryEndpoint.GenerateAsync(Request(), service, Context("PRODUCTION"), new Host(Environments.Production));
        Assert.Equal(422, failure.HttpStatusCode);
        var text = System.Text.Json.JsonSerializer.Serialize(failure);
        Assert.DoesNotContain("sql", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoutesExposeOnlyGovernedGenerationReadAndJsonCsvExport()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Api", "FiscalReports", "BirSalesSummaryEndpointRouteBuilderExtensions.cs"));
        Assert.Contains("/v1/fiscal-reports/bir-sales-summaries", source, StringComparison.Ordinal);
        Assert.Contains("GeneratePolicyName", source, StringComparison.Ordinal);
        Assert.Contains("ReadPolicyName", source, StringComparison.Ordinal);
        Assert.Contains("ExportPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("annex", source, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PosServer:Admin:ApiKeys:0:Principal"]="summary-service", ["PosServer:Admin:ApiKeys:0:Key"]="summary-key",
        ["PosServer:Admin:ApiKeys:0:Permissions:0"]=BirSalesSummaryAuthorization.GeneratePermission,
        ["PosServer:Admin:ApiKeys:0:Permissions:1"]=BirSalesSummaryAuthorization.ReadPermission,
        ["PosServer:Admin:ApiKeys:0:Permissions:2"]=BirSalesSummaryAuthorization.ExportPermission,
        ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"]=SiteId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"]=IdentityId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"]="PHP"
    }).Build();

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!), new(BirSalesSummaryAuthorization.AuthorityClassClaimType, authentication.AuthorityClass!) };
        claims.AddRange(authentication.Permissions.Select(value => new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value => new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value => new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        claims.AddRange(authentication.CurrencyScopes.Select(value => new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }
    private static DefaultHttpContext Context(string authorityClass)
    {
        var context=new DefaultHttpContext();context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName]="summary-correlation";
        context.User=new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,"summary-service"),new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType,authorityClass),new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType,SiteId.ToString("D")),new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType,IdentityId.ToString("D")),new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType,"PHP")],SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));return context;
    }
    private static GenerateBirSalesSummaryRequest Request()=>new("summary-operation",SiteId,IdentityId,"PHP",PeriodId,"Z-TEST");
    private static BirSalesSummaryRecord Record()=>new(Guid.NewGuid(),Guid.NewGuid(),"summary-operation",BirSalesSummaryContract.ReportKind,BirSalesSummaryContract.ContractVersion,BirSalesSummaryContract.SemanticHashVersion,BirSalesSummaryContract.ReportingProfile,Guid.NewGuid(),"Z-TEST",SiteId,IdentityId,PeriodId,new(2026,8,3),new(2026,8,3),new(2026,8,4),"PHP",0,null,null,new(0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0),[],[],[],new(0,0,0,1,0,0,0,1,2),new(Guid.NewGuid(),"v1","serial","min","accreditation",new(2026,1,1),new(2027,1,1),"ptu",new(2026,1,1)),DateTimeOffset.Parse("2026-08-04T01:00:00Z"),DateTimeOffset.Parse("2026-08-04T01:00:00Z"),"correlation","BIRSS-TEST",true);
    private static string FindSource(params string[] parts){var directory=new DirectoryInfo(AppContext.BaseDirectory);while(directory is not null){var candidate=Path.Combine([directory.FullName,..parts]);if(File.Exists(candidate))return candidate;directory=directory.Parent;}throw new FileNotFoundException();}
    private static readonly Guid SiteId=Guid.Parse("84000000-0000-4000-8000-000000000001");private static readonly Guid IdentityId=Guid.Parse("84000000-0000-4000-8000-000000000002");private static readonly Guid PeriodId=Guid.Parse("84000000-0000-4000-8000-000000000003");
    private sealed class StubRepository(BirSalesSummaryResult result):IBirSalesSummaryRepository{public BirSalesSummaryResult Result{get;set;}=result;public int GenerateCalls{get;private set;}public Task<BirSalesSummaryResult>GenerateAsync(BirSalesSummaryCommand command,CancellationToken cancellationToken=default){GenerateCalls++;return Task.FromResult(Result);}public Task<BirSalesSummaryResult>GetByIdAsync(Guid id,CancellationToken cancellationToken=default)=>Task.FromResult(Result);}
    private sealed class Host(string environmentName):IHostEnvironment{public string EnvironmentName{get;set;}=environmentName;public string ApplicationName{get;set;}="tests";public string ContentRootPath{get;set;}=Directory.GetCurrentDirectory();public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
