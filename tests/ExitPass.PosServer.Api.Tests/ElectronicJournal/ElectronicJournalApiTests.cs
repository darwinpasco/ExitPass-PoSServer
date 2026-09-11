using System.Security.Claims;
using System.Text.Json;
using ExitPass.PosServer.Api.ElectronicJournal;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.ElectronicJournal;

public sealed class ElectronicJournalApiTests
{
    private static readonly Guid Site = Guid.Parse("82000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("82000000-0000-4000-8000-000000000002");

    [Fact]
    public void PermissionsAreSeparateAndGlobalScopeIsNeverImplicit()
    {
        Assert.True(ElectronicJournalAuthorization.IsElectronicJournalPermission(ElectronicJournalAuthorization.ReadPermission));
        Assert.True(ElectronicJournalAuthorization.IsElectronicJournalPermission(ElectronicJournalAuthorization.ExportPermission));
        Assert.NotEqual(ElectronicJournalAuthorization.ReadPermission, ElectronicJournalAuthorization.ExportPermission);
        var global = Principal(ElectronicJournalAuthorization.ReadPermission, "*", "*", "*");
        Assert.False(ElectronicJournalAuthorization.IsInScope(global, Site, Identity, "PHP"));
        Assert.True(ElectronicJournalAuthorization.IsInScope(
            Principal(ElectronicJournalAuthorization.ReadPermission, Site.ToString("D"), Identity.ToString("D"), "PHP"), Site, Identity, "PHP"));
    }

    [Fact]
    public void ConfiguredJournalAuthorityRequiresExactScopesAndClientPermissionCannotEscalate()
    {
        var values = new Dictionary<string, string?>
        {
            ["PosServer:Admin:ApiKeys:0:Principal"] = "ej-reader",
            ["PosServer:Admin:ApiKeys:0:Key"] = "synthetic-secret",
            ["PosServer:Admin:ApiKeys:0:Permissions:0"] = ElectronicJournalAuthorization.ReadPermission,
            ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = Site.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = Identity.ToString("D"),
            ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"] = "PHP",
            ["PosServer:Admin:ApiKeys:0:AuthorityClass"] = "PRODUCTION"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration, "synthetic-secret", ElectronicJournalAuthorization.ExportPermission);
        Assert.True(authentication.Succeeded);
        Assert.Empty(authentication.Permissions);
        Assert.Equal("PRODUCTION", authentication.AuthorityClass);

        values["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = "*";
        Assert.False(SalesInvoiceHeaderProfileAdminAuthorization.ResolveConfiguredApiKeys(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build()).IsValid);
    }

    [Fact]
    public async Task ProductionRejectsFixtureAuthorityBeforeRepositoryRead()
    {
        var repository = new FakeRepository();
        var service = new ElectronicJournalService(repository, new ElectronicJournalExportRenderer());
        var context = Context(Principal(ElectronicJournalAuthorization.ReadPermission, Site.ToString("D"), Identity.ToString("D"), "PHP", "FIXTURE"));
        var result = await ElectronicJournalEndpoint.ReadAsync(new ElectronicJournalFilterRequest
        {
            SitePosServerId = Site, FiscalIdentityId = Identity, CurrencyCode = "PHP"
        }, service, context, new Host(Environments.Production), default);

        Assert.NotNull(result);
        Assert.Equal(0, repository.ReadCount);
    }

    [Fact]
    public async Task OmittedPageSizeUsesGovernedDefault()
    {
        var repository = new FakeRepository();
        var service = new ElectronicJournalService(repository, new ElectronicJournalExportRenderer());
        var context = Context(Principal(ElectronicJournalAuthorization.ReadPermission, Site.ToString("D"), Identity.ToString("D"), "PHP"));
        await ElectronicJournalEndpoint.ReadAsync(new ElectronicJournalFilterRequest
        {
            SitePosServerId = Site, FiscalIdentityId = Identity, CurrencyCode = "PHP"
        }, service, context, new Host(Environments.Production), default);

        Assert.Equal(ElectronicJournalContract.DefaultPageSize, repository.LastQuery?.PageSize);
    }

    [Fact]
    public void RouteAndPolicyContractsAreRegistered()
    {
        var routes = File.ReadAllText(Find("src", "ExitPass.PosServer.Api", "ElectronicJournal", "ElectronicJournalEndpointRouteBuilderExtensions.cs"));
        Assert.Contains("/v1/electronic-journal", routes, StringComparison.Ordinal);
        Assert.Contains("/events", routes, StringComparison.Ordinal);
        Assert.Contains("/exports/{format}", routes, StringComparison.Ordinal);
        Assert.Contains("/integrity-verifications", routes, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(ElectronicJournalAuthorization.ReadPolicyName)", routes, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(ElectronicJournalAuthorization.ExportPolicyName)", routes, StringComparison.Ordinal);
        Assert.Contains("RequireAuthorization(ElectronicJournalAuthorization.IntegrityPolicyName)", routes, StringComparison.Ordinal);
    }

    [Fact]
    public void MachineReadableContractIsValidAndMatchesRuntimeLimits()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Find("contracts", "pos-server", "electronic-journal-api.v1.json")));
        var root = document.RootElement;
        Assert.Equal("pos-server-electronic-journal-api:v1", root.GetProperty("contract_version").GetString());
        Assert.Equal(ElectronicJournalContract.EventSchemaVersion, root.GetProperty("event_schema_version").GetString());
        Assert.Equal(ElectronicJournalContract.CurrentSemanticHashVersion, root.GetProperty("semantic_hash_version").GetString());
        Assert.Equal(
            new[] { ElectronicJournalContract.LegacySemanticHashVersion, ElectronicJournalContract.CurrentSemanticHashVersion },
            root.GetProperty("supported_semantic_hash_versions").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(ElectronicJournalContract.MaximumPageSize, root.GetProperty("pagination").GetProperty("maximum_page_size").GetInt32());
        Assert.Equal(ElectronicJournalContract.MaximumExportEvents, root.GetProperty("pagination").GetProperty("maximum_export_events").GetInt32());
        Assert.Contains(root.GetProperty("routes").EnumerateArray(), route =>
            route.GetProperty("path").GetString() == "/v1/electronic-journal/events" &&
            route.GetProperty("permission").GetString() == ElectronicJournalAuthorization.ReadPermission);
    }

    private static ClaimsPrincipal Principal(string permission, string site, string identity, string currency, string authority = "PRODUCTION") => new(
        new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "ej-service"),
            new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, site),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, identity),
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, currency),
            new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, authority)
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));

    private static DefaultHttpContext Context(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext { User = principal };
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "ej-correlation";
        return context;
    }

    private static string Find(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(parts[^1]);
    }

    private sealed class FakeRepository : IElectronicJournalRepository
    {
        public int ReadCount { get; private set; }
        public ElectronicJournalQuery? LastQuery { get; private set; }
        public Task<ElectronicJournalPageResult> ReadAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default)
        { ReadCount++; LastQuery = query; return Task.FromResult(new ElectronicJournalPageResult(ElectronicJournalOutcome.NotFound)); }
        public Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ElectronicJournalIntegrityOutcome(ElectronicJournalOutcome.NotFound));
        public Task RecordAccessAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, string action, string result,
            string actorReference, string serviceIdentityReference, string correlationReference, string supportReference, int eventCount,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Host(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
