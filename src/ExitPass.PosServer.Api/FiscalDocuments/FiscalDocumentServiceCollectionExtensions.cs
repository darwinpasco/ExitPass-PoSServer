using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentServiceCollectionExtensions
{
    public static IServiceCollection AddPosServerFiscalDocumentApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("PosServer") ??
            configuration["POSSERVER_DB_URL"] ??
            configuration["PosServer:Database:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.TryAddScoped<IFiscalDocumentRepository, PersistenceNotConfiguredFiscalDocumentRepository>();
            services.TryAddScoped<IFiscalDocumentReader, PersistenceNotConfiguredFiscalDocumentReader>();
            services.TryAddScoped<ISalesInvoiceHeaderProfileRepository, PersistenceNotConfiguredSalesInvoiceHeaderProfileRepository>();
            services.TryAddScoped<IFiscalXReadingRepository, UnavailableFiscalXReadingRepository>();
            services.TryAddScoped<IFiscalZCloseStateRepository, UnavailableFiscalZCloseStateRepository>();
        }
        else if (!IsValidNpgsqlConnectionString(connectionString))
        {
            services.TryAddScoped<IFiscalDocumentRepository, InvalidPersistenceConfigurationFiscalDocumentRepository>();
            services.TryAddScoped<IFiscalDocumentReader, InvalidPersistenceConfigurationFiscalDocumentReader>();
            services.TryAddScoped<ISalesInvoiceHeaderProfileRepository, PersistenceNotConfiguredSalesInvoiceHeaderProfileRepository>();
            services.TryAddScoped<IFiscalXReadingRepository, UnavailableFiscalXReadingRepository>();
            services.TryAddScoped<IFiscalZCloseStateRepository, UnavailableFiscalZCloseStateRepository>();
        }
        else
        {
            services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            services.TryAddScoped<IFiscalDocumentRepository>(provider =>
                new PostgresFiscalDocumentRepository(
                    provider.GetRequiredService<NpgsqlDataSource>(),
                    IsSalesInvoiceHeaderProfileRequired(configuration)));
            services.TryAddScoped<IFiscalDocumentReader, PostgresFiscalDocumentReader>();
            services.TryAddScoped<ISalesInvoiceHeaderProfileRepository, PostgresSalesInvoiceHeaderProfileRepository>();
            services.TryAddScoped<IFiscalXReadingRepository, PostgresFiscalXReadingRepository>();
            services.TryAddScoped<IFiscalZCloseStateRepository, PostgresFiscalZCloseStateRepository>();
        }

        services.AddScoped<FiscalDocumentCreationService>();
        services.AddScoped<FiscalDocumentVoidService>();
        services.AddScoped<FiscalDocumentReadService>();
        services.AddScoped<DigitalSalesInvoiceRenderService>();
        services.AddScoped<DigitalSalesInvoicePresentationAdapter>();
        services.AddScoped<FiscalXReadingAggregationService>();
        services.AddScoped<FiscalXReadingService>();
        services.AddScoped<FiscalZCloseStateInitializationService>();
        services.AddScoped(provider => new SalesInvoiceHeaderProfileAdminService(
            provider.GetRequiredService<ISalesInvoiceHeaderProfileRepository>(),
            IsSalesInvoiceHeaderProfileRequired(configuration)));

        services
            .AddAuthentication(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
            .AddScheme<PosServerAdminApiKeyAuthenticationOptions, PosServerAdminApiKeyAuthenticationHandler>(
                SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme,
                _ => { });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                SalesInvoiceHeaderProfileAdminAuthorization.PolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(
                        SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                        SalesInvoiceHeaderProfileAdminAuthorization.RequiredPermission));
            options.AddPolicy(
                FiscalXReadingAuthorization.GeneratePolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalXReadingAuthorization.GeneratePermission));
            options.AddPolicy(
                FiscalXReadingAuthorization.ReadPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalXReadingAuthorization.ReadPermission));
            options.AddPolicy(
                FiscalZCloseStateInitializationAuthorization.PolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(
                        SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                        FiscalZCloseStateInitializationAuthorization.Permission));
        });

        return services;
    }

    private static bool IsSalesInvoiceHeaderProfileRequired(IConfiguration configuration) =>
        string.Equals(
            configuration["POS_REQUIRE_COMPLETE_SALES_INVOICE_HEADER_PROFILE"] ??
            configuration["PosServer:FiscalDocuments:RequireCompleteSalesInvoiceHeaderProfile"],
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsValidNpgsqlConnectionString(string connectionString)
    {
        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) &&
            (uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        try
        {
            _ = new NpgsqlConnectionStringBuilder(connectionString);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
