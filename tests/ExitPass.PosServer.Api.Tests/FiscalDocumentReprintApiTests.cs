using System.Security.Claims;
using ExitPass.PosServer.Api.ElectronicJournal;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentReprintApiTests
{
    private static readonly Guid Site = Guid.Parse("84000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("84000000-0000-4000-8000-000000000002");

    [Fact]
    public void PermissionRequiresExactScopeAndProductionAuthority()
    {
        var exact = Principal(Site.ToString("D"), Identity.ToString("D"), "PHP", "PRODUCTION");
        Assert.True(FiscalDocumentReprintAuthorization.IsInScope(exact, Site, Identity, "PHP"));
        Assert.True(FiscalDocumentReprintAuthorization.IsHostingAuthorityAllowed(exact, new Host(Environments.Production)));
        Assert.False(FiscalDocumentReprintAuthorization.IsInScope(Principal("*", "*", "*", "PRODUCTION"), Site, Identity, "PHP"));
        Assert.False(FiscalDocumentReprintAuthorization.IsHostingAuthorityAllowed(
            Principal(Site.ToString("D"), Identity.ToString("D"), "PHP", "FIXTURE"), new Host(Environments.Production)));
    }

    [Fact]
    public void ConfigRejectsWildcardReprintAuthorityAndPermissionCannotEscalate()
    {
        var values = ConfigurationValues();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, "synthetic-reprint-key", ElectronicJournalAuthorization.ReadPermission);
        Assert.True(authentication.Succeeded);
        Assert.Empty(authentication.Permissions);
        values["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = "*";
        Assert.False(SalesInvoiceHeaderProfileAdminAuthorization.ResolveConfiguredApiKeys(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build()).IsValid);
    }

    [Fact]
    public async Task WrongScopeAndFixtureAuthorityAreHiddenBeforePersistence()
    {
        var repository = new RecordingRepository();
        var service = new FiscalDocumentReprintService(repository);
        var context = Context(Principal(Site.ToString("D"), Identity.ToString("D"), "PHP", "PRODUCTION"));
        var wrongScope = await FiscalDocumentReprintEndpoint.RecordAsync(
            Guid.NewGuid(), new("op", Guid.NewGuid(), Identity, "PHP", "operator_request"),
            service, context, new Host(Environments.Production));
        Assert.Equal(StatusCodes.Status404NotFound, wrongScope.HttpStatusCode);

        context.User = Principal(Site.ToString("D"), Identity.ToString("D"), "PHP", "FIXTURE");
        var fixture = await FiscalDocumentReprintEndpoint.RecordAsync(
            Guid.NewGuid(), new("op", Site, Identity, "PHP", "operator_request"),
            service, context, new Host(Environments.Production));
        Assert.Equal(StatusCodes.Status404NotFound, fixture.HttpStatusCode);
        Assert.Equal(0, repository.WriteCount);
    }

    [Fact]
    public void RouteAndPolicyAreRegistered()
    {
        var root = FindRepositoryRoot();
        var routes = File.ReadAllText(Path.Combine(root, "src", "ExitPass.PosServer.Api", "FiscalDocuments", "FiscalDocumentEndpointRouteBuilderExtensions.cs"));
        var services = File.ReadAllText(Path.Combine(root, "src", "ExitPass.PosServer.Api", "FiscalDocuments", "FiscalDocumentServiceCollectionExtensions.cs"));
        Assert.Contains("/{fiscalDocumentId:guid}/reprints", routes, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(FiscalDocumentReprintAuthorization.RecordPolicyName)", routes, StringComparison.Ordinal);
        Assert.Contains("FiscalDocumentReprintAuthorization.RecordPermission", services, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> ConfigurationValues() => new()
    {
        ["PosServer:Admin:ApiKeys:0:Principal"] = "reprint-service",
        ["PosServer:Admin:ApiKeys:0:Key"] = "synthetic-reprint-key",
        ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalDocumentReprintAuthorization.RecordPermission,
        ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = Site.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = Identity.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"] = "PHP",
        ["PosServer:Admin:ApiKeys:0:AuthorityClass"] = "PRODUCTION"
    };

    private static ClaimsPrincipal Principal(string site, string identity, string currency, string authority) => new(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "reprint-service"),
        new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalDocumentReprintAuthorization.RecordPermission),
        new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, site),
        new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, identity),
        new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, currency),
        new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, authority)
    }, "test"));

    private static DefaultHttpContext Context(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext { User = principal };
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "reprint-correlation";
        return context;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ExitPass.PosServer.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException();
    }

    private sealed class RecordingRepository : IFiscalDocumentReprintRepository
    {
        public int WriteCount { get; private set; }
        public Task<FiscalDocumentReprintResult> RecordAsync(
            FiscalDocumentReprintCommand command, string semanticRequestHash, CancellationToken cancellationToken = default)
        {
            WriteCount++;
            return Task.FromResult(new FiscalDocumentReprintResult(FiscalDocumentReprintOutcome.Created));
        }
    }

    private sealed class Host(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
