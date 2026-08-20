using ExitPass.PosServer.Api.FiscalDocuments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentAuthorizationTests
{
    [Fact]
    public async Task UnauthenticatedFiscalDocumentBusinessRoutesFailBeforeBusinessHandling()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosServer"] = "Host=127.0.0.1;Database=pos_test;Username=pos_test;Password=not-used",
                    ["PosServer:Admin:ApiKeys:0:Principal"] = "central-pms",
                    ["PosServer:Admin:ApiKeys:0:Key"] = "synthetic-test-key",
                    ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalDocumentAuthorization.CreatePermission,
                    ["PosServer:Admin:ApiKeys:0:Permissions:1"] = FiscalDocumentAuthorization.ReadPermission,
                    ["PosServer:Admin:ApiKeys:0:Permissions:2"] = FiscalDocumentAuthorization.VoidPermission
                })));
        using var client = factory.CreateClient();
        var id = Guid.Parse("00000000-0000-4000-8000-000000000099");

        using var create = await client.PostAsJsonAsync("/v1/fiscal-documents/", new { });
        using var read = await client.GetAsync($"/v1/fiscal-documents/{id:D}");
        using var voidRequest = await client.PostAsJsonAsync($"/v1/fiscal-documents/{id:D}/void", new { });
        using var digital = await client.GetAsync($"/v1/fiscal-documents/{id:D}/digital-sales-invoice");
        using var presentation = await client.GetAsync($"/v1/fiscal-documents/{id:D}/digital-sales-invoice/presentation");

        Assert.All([create, read, voidRequest, digital, presentation], response =>
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
    }

    [Fact]
    public void FiscalDocumentRoutesRequireNamedServicePolicies()
    {
        var source = File.ReadAllText(FindSourceFile("FiscalDocumentEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("RequireAuthorization(FiscalDocumentAuthorization.CreatePolicyName)", source, StringComparison.Ordinal);
        Assert.Equal(3, Count(source, "RequireAuthorization(FiscalDocumentAuthorization.ReadPolicyName)"));
        Assert.Contains("RequireAuthorization(FiscalDocumentAuthorization.VoidPolicyName)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PoliciesRequireServerDerivedFiscalDocumentPermissions()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PosServer"] = "Host=127.0.0.1;Database=pos_test;Username=pos_test;Password=not-used",
                ["PosServer:Admin:ApiKeys:0:Principal"] = "central-pms",
                ["PosServer:Admin:ApiKeys:0:Key"] = "synthetic-test-key",
                ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalDocumentAuthorization.CreatePermission,
                ["PosServer:Admin:ApiKeys:0:Permissions:1"] = FiscalDocumentAuthorization.ReadPermission,
                ["PosServer:Admin:ApiKeys:0:Permissions:2"] = FiscalDocumentAuthorization.VoidPermission
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        AssertPolicy(options, FiscalDocumentAuthorization.CreatePolicyName, FiscalDocumentAuthorization.CreatePermission);
        AssertPolicy(options, FiscalDocumentAuthorization.ReadPolicyName, FiscalDocumentAuthorization.ReadPermission);
        AssertPolicy(options, FiscalDocumentAuthorization.VoidPolicyName, FiscalDocumentAuthorization.VoidPermission);

        var forged = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            "synthetic-test-key",
            FiscalDocumentAuthorization.CreatePermission + "," + FiscalDocumentAuthorization.ReadPermission);
        Assert.DoesNotContain(FiscalDocumentAuthorization.ReadPermission, forged.Permissions);
    }

    private static void AssertPolicy(AuthorizationOptions options, string policyName, string permission)
    {
        var policy = options.GetPolicy(policyName);
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements.OfType<ClaimsAuthorizationRequirement>(), requirement =>
            requirement.ClaimType == SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType &&
            requirement.AllowedValues?.Contains(permission, StringComparer.Ordinal) == true);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
    }

    private static string FindSourceFile(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "ExitPass.PosServer.Api", "FiscalDocuments", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException(fileName);
    }
}
