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

public sealed class MonthlyESalesApiTests
{
    [Fact]
    public async Task ReadAndExportPermissionsRemainNarrowAndIndependent()
    {
        var services=new ServiceCollection();services.AddLogging();services.AddPosServerFiscalDocumentApi(new ConfigurationBuilder().AddInMemoryCollection().Build());using var provider=services.BuildServiceProvider();
        var options=provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;var authorization=provider.GetRequiredService<IAuthorizationService>();
        var reader=Principal(MonthlyESalesAuthorization.ReadPermission);
        Assert.True((await authorization.AuthorizeAsync(reader,null,options.GetPolicy(MonthlyESalesAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(reader,null,options.GetPolicy(MonthlyESalesAuthorization.ExportPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(reader,null,options.GetPolicy(AnnexE1Authorization.RecordFactPolicy)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(reader,null,options.GetPolicy(AnnexE1Authorization.CorrectPolicy)!)).Succeeded);
    }

    [Fact]
    public async Task ExactScopeIsRequiredBeforeReadOnlyRepositoryCall()
    {
        var repository=new Stub();var service=new MonthlyESalesService(repository);var request=new MonthlyESalesRequest(Site,Identity,"PHP",2026,9);
        var success=await MonthlyESalesEndpoint.ReadAsync(request,service,Context(),new Host(Environments.Production),default);
        Assert.Equal(200,success.HttpStatusCode);Assert.Equal(1,repository.Calls);Assert.Equal(new MonthlyESalesQuery(Site,Identity,"PHP",2026,9),repository.Query);
        var wrongSite=await MonthlyESalesEndpoint.ReadAsync(request with{SitePosServerId=Guid.NewGuid()},service,Context(),new Host(Environments.Production),default);
        var wrongIdentity=await MonthlyESalesEndpoint.ReadAsync(request with{FiscalIdentityId=Guid.NewGuid()},service,Context(),new Host(Environments.Production),default);
        var wrongCurrency=await MonthlyESalesEndpoint.ReadAsync(request with{CurrencyCode="USD"},service,Context(),new Host(Environments.Production),default);
        Assert.All(new[]{wrongSite,wrongIdentity,wrongCurrency},value=>Assert.Equal(404,value.HttpStatusCode));Assert.Equal(1,repository.Calls);
    }

    [Fact]
    public async Task IncompleteMonthReturnsSafeConflictWithoutMutation()
    {
        var repository=new Stub{Result=new(MonthlyESalesOutcome.IncompleteAuthoritativeSources,SafeMessage:"Monthly eSales cannot be finalized because committed BIR Sales Summaries are missing for: 2026-09-04.")};
        var response=await MonthlyESalesEndpoint.ReadAsync(new(Site,Identity,"PHP",2026,9),new(repository),Context(),new Host(Environments.Production),default);
        Assert.Equal(409,response.HttpStatusCode);Assert.Equal("monthly_esales_sources_incomplete",response.Code);Assert.Contains("2026-09-04",response.Message);
    }

    private static DefaultHttpContext Context(){var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"central-pms"),new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType,"PRODUCTION"),new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType,Site.ToString()),new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType,Identity.ToString()),new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType,"PHP"),new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,MonthlyESalesAuthorization.ReadPermission)};return new(){User=new ClaimsPrincipal(new ClaimsIdentity(claims,"test")),TraceIdentifier="trace",Request={Headers={ [SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName]=Guid.NewGuid().ToString("D") }}};}
    private static ClaimsPrincipal Principal(string permission){var context=Context();context.User.AddIdentity(new ClaimsIdentity([new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,permission)]));return context.User;}
    private static MonthlyESalesReport Report()=>new(Site,Identity,2026,9,"PHP","999-999-999-000",new(2026,10,10),"ODD_TIN","TECHNICAL_UAT","UAT fiscal identifier - not for BIR submission.",[]);
    private static readonly Guid Site=Guid.Parse("91000000-0000-4000-8000-000000000001");private static readonly Guid Identity=Guid.Parse("91000000-0000-4000-8000-000000000002");
    private sealed class Host(string name):IHostEnvironment{public string EnvironmentName{get;set;}=name;public string ApplicationName{get;set;}="tests";public string ContentRootPath{get;set;}=".";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
    private sealed class Stub:IMonthlyESalesRepository{public int Calls;public MonthlyESalesQuery? Query;public MonthlyESalesResult Result=new(MonthlyESalesOutcome.Succeeded,Report());public Task<MonthlyESalesResult> ReadAsync(MonthlyESalesQuery query,CancellationToken cancellationToken=default){Calls++;Query=query;return Task.FromResult(Result);}public Task<MonthlyESalesPdfResult>ReadPdfAsync(MonthlyESalesQuery query,CancellationToken cancellationToken=default)=>Task.FromResult(new MonthlyESalesPdfResult(MonthlyESalesOutcome.PersistenceFailure,SafeMessage:"not configured"));}
}
