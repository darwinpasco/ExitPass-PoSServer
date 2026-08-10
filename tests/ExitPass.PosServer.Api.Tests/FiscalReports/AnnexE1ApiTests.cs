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

public sealed class AnnexE1ApiTests
{
    [Fact]
    public async Task SixDedicatedPoliciesRemainIndependent()
    {
        var services=new ServiceCollection();services.AddLogging();services.AddPosServerFiscalDocumentApi(Configuration());using var provider=services.BuildServiceProvider();var options=provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;var authorization=provider.GetRequiredService<IAuthorizationService>();var principal=Principal(AnnexE1Authorization.ReadPermission);
        Assert.True((await authorization.AuthorizeAsync(principal,null,options.GetPolicy(AnnexE1Authorization.ReadPolicy)!)).Succeeded);
        foreach(var policy in new[]{AnnexE1Authorization.RecordFactPolicy,AnnexE1Authorization.AttestZeroPolicy,AnnexE1Authorization.GeneratePolicy,AnnexE1Authorization.DownloadPolicy,AnnexE1Authorization.CorrectPolicy})Assert.False((await authorization.AuthorizeAsync(principal,null,options.GetPolicy(policy)!)).Succeeded);
    }

    [Fact]
    public async Task WrongScopeAndProductionFixtureFailBeforePersistence()
    {
        var repo=new Stub();var service=new AnnexE1Service(repo);var request=Generation();
        var wrong=await AnnexE1Endpoint.GenerateAsync(request with{CurrencyCode="USD"},service,Context("PRODUCTION"),new Host(Environments.Development),default);Assert.Equal(404,wrong.HttpStatusCode);Assert.Equal(0,repo.GenerateCalls);
        var fixture=await AnnexE1Endpoint.GenerateAsync(request,service,Context("FIXTURE"),new Host(Environments.Production),default);Assert.Equal(403,fixture.HttpStatusCode);Assert.Equal(0,repo.GenerateCalls);
    }

    [Fact]
    public async Task CorrectionRequiresSeparatePermissionAndSafeErrors()
    {
        var repo=new Stub();var service=new AnnexE1Service(repo);var response=await AnnexE1Endpoint.GenerateAsync(Generation() with{SupersedesWorkbookId=Guid.NewGuid(),CorrectionReason="authorized_restatement",CorrectionApprovalReference="approval"},service,Context("PRODUCTION",AnnexE1Authorization.GeneratePermission),new Host(Environments.Production),default);Assert.Equal(403,response.HttpStatusCode);Assert.Equal(0,repo.GenerateCalls);
        repo.WorkbookResult=new(AnnexE1Outcome.MissingAccountingFact,SafeMessage:"A governed fact is missing.");var failure=await AnnexE1Endpoint.GenerateAsync(Generation(),service,Context("PRODUCTION",AnnexE1Authorization.GeneratePermission),new Host(Environments.Production),default);Assert.Equal(422,failure.HttpStatusCode);Assert.DoesNotContain("sql",System.Text.Json.JsonSerializer.Serialize(failure),StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoutesAndContractExposeOnlyLocalBoundedOperations()
    {
        var root=FindRoot();var routes=File.ReadAllText(Path.Combine(root,"src","ExitPass.PosServer.Api","FiscalReports","AnnexE1EndpointRouteBuilderExtensions.cs"));Assert.Contains("/v1/fiscal-reports/annex-e/e1",routes);Assert.Contains("known-zero-attestations",routes);Assert.Contains("/content",routes);Assert.DoesNotContain("submit",routes,StringComparison.OrdinalIgnoreCase);
        using var contract=System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"contracts","pos-server","annex-e1-api.v1.json")));Assert.Equal(AnnexE1Contract.Profile,contract.RootElement.GetProperty("profile").GetString());
    }

    private static IConfiguration Configuration()=>new ConfigurationBuilder().AddInMemoryCollection().Build();
    private static AnnexE1GenerationRequest Generation()=>new("operation",Site,Identity,"PHP",2026,8,AnnexE1Contract.Profile,null,null,null);
    private static DefaultHttpContext Context(string authority,params string[] permissions){var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,"operator"),new(BirSalesSummaryAuthorization.AuthorityClassClaimType,authority),new(FiscalXReadingAuthorization.SitePosServerScopeClaimType,Site.ToString()),new(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType,Identity.ToString()),new(FiscalZReadingAuthorization.CurrencyScopeClaimType,"PHP")};claims.AddRange(permissions.Select(p=>new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,p)));return new(){User=new ClaimsPrincipal(new ClaimsIdentity(claims,"test")),TraceIdentifier="correlation"};}
    private static ClaimsPrincipal Principal(string permission)=>Context("PRODUCTION",permission).User;
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!Directory.Exists(Path.Combine(d.FullName,"contracts")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException();}
    private static readonly Guid Site=Guid.Parse("91000000-0000-4000-8000-000000000001");private static readonly Guid Identity=Guid.Parse("91000000-0000-4000-8000-000000000002");
    private sealed class Host(string name):IHostEnvironment{public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="tests";public string ContentRootPath{get;set;}=".";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
    private sealed class Stub:IAnnexE1Repository{public int GenerateCalls;public AnnexE1WorkbookResult WorkbookResult=new(AnnexE1Outcome.Created);public Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand c,CancellationToken t=default)=>Task.FromResult(new AnnexE1FactResult(AnnexE1Outcome.Created));public Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand c,CancellationToken t=default){GenerateCalls++;return Task.FromResult(WorkbookResult);}public Task<AnnexE1WorkbookResult> GetAsync(Guid i,CancellationToken t=default)=>Task.FromResult(WorkbookResult);public Task<AnnexE1ArtifactResult> DownloadAsync(Guid i,CancellationToken t=default)=>Task.FromResult(new AnnexE1ArtifactResult(AnnexE1Outcome.NotFound));}
}
