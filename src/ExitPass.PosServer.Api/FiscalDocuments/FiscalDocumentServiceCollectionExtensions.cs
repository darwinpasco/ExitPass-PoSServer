using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using ExitPass.PosServer.Api.ElectronicJournal;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Persistence.Postgres.ElectronicJournal;

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
            services.TryAddScoped<IFiscalZReadingRepository, UnavailableFiscalZReadingRepository>();
            services.TryAddScoped<IBirSalesSummaryRepository, UnavailableBirSalesSummaryRepository>();
            services.TryAddScoped<IElectronicJournalRepository, UnavailableElectronicJournalRepository>();
            services.TryAddScoped<IFiscalDocumentReprintRepository, UnavailableFiscalDocumentReprintRepository>();
            services.TryAddScoped<IAnnexE1Repository, UnavailableAnnexE1Repository>();
        }
        else if (!IsValidNpgsqlConnectionString(connectionString))
        {
            services.TryAddScoped<IFiscalDocumentRepository, InvalidPersistenceConfigurationFiscalDocumentRepository>();
            services.TryAddScoped<IFiscalDocumentReader, InvalidPersistenceConfigurationFiscalDocumentReader>();
            services.TryAddScoped<ISalesInvoiceHeaderProfileRepository, PersistenceNotConfiguredSalesInvoiceHeaderProfileRepository>();
            services.TryAddScoped<IFiscalXReadingRepository, UnavailableFiscalXReadingRepository>();
            services.TryAddScoped<IFiscalZCloseStateRepository, UnavailableFiscalZCloseStateRepository>();
            services.TryAddScoped<IFiscalZReadingRepository, UnavailableFiscalZReadingRepository>();
            services.TryAddScoped<IBirSalesSummaryRepository, UnavailableBirSalesSummaryRepository>();
            services.TryAddScoped<IElectronicJournalRepository, UnavailableElectronicJournalRepository>();
            services.TryAddScoped<IFiscalDocumentReprintRepository, UnavailableFiscalDocumentReprintRepository>();
            services.TryAddScoped<IAnnexE1Repository, UnavailableAnnexE1Repository>();
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
            services.TryAddScoped<IFiscalZReadingRepository, PostgresFiscalZReadingRepository>();
            services.TryAddScoped<IBirSalesSummaryRepository, PostgresBirSalesSummaryRepository>();
            services.TryAddScoped<IElectronicJournalRepository, PostgresElectronicJournalRepository>();
            services.TryAddScoped<IFiscalDocumentReprintRepository, PostgresFiscalDocumentReprintRepository>();
            var annexE1ArtifactRoot = configuration["PosServer:AnnexE1:ArtifactRoot"];
            if (!string.IsNullOrWhiteSpace(annexE1ArtifactRoot) && Path.IsPathFullyQualified(annexE1ArtifactRoot))
            {
                services.TryAddSingleton<IAnnexE1ArtifactStore>(_ => new FileSystemAnnexE1ArtifactStore(annexE1ArtifactRoot));
                services.TryAddScoped<IAnnexE1Repository, PostgresAnnexE1Repository>();
            }
            else
            {
                services.TryAddScoped<IAnnexE1Repository, UnavailableAnnexE1Repository>();
            }
        }

        services.AddScoped<FiscalDocumentCreationService>();
        services.AddScoped<FiscalDocumentVoidService>();
        services.AddScoped<FiscalDocumentReprintService>();
        services.AddScoped<FiscalDocumentReadService>();
        services.AddScoped<DigitalSalesInvoiceRenderService>();
        services.AddScoped<DigitalSalesInvoicePresentationAdapter>();
        services.AddScoped<FiscalXReadingAggregationService>();
        services.AddScoped<FiscalXReadingService>();
        services.AddScoped<FiscalZCloseStateInitializationService>();
        services.AddScoped<FiscalZReadingService>();
        services.AddScoped<BirSalesSummaryService>();
        services.AddSingleton<BirSalesSummaryOutputRenderer>();
        services.AddScoped<ElectronicJournalService>();
        services.AddSingleton<ElectronicJournalExportRenderer>();
        services.AddScoped<AnnexE1Service>();
        services.AddSingleton<AnnexE1CalculationEngine>();
        services.AddSingleton<AnnexE1DeterministicXlsxRenderer>();
        services.AddSingleton<FiscalReportPresentationService>();
        services.AddSingleton<FiscalReportOutputRenderer>();
        services.TryAddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler, FiscalReportOutputAuthorizationResultHandler>();
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
                FiscalDocumentAuthorization.CreatePolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(
                        SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                        FiscalDocumentAuthorization.CreatePermission));
            options.AddPolicy(
                FiscalDocumentAuthorization.ReadPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(
                        SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                        FiscalDocumentAuthorization.ReadPermission));
            options.AddPolicy(
                FiscalDocumentAuthorization.VoidPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(
                        SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                        FiscalDocumentAuthorization.VoidPermission));
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
            options.AddPolicy(
                FiscalZReadingAuthorization.ClosePolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalZReadingAuthorization.ClosePermission));
            options.AddPolicy(
                FiscalZReadingAuthorization.ReadPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalZReadingAuthorization.ReadPermission));
            options.AddPolicy(
                FiscalReportOutputAuthorization.XExportPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalReportOutputAuthorization.XExportPermission));
            options.AddPolicy(
                FiscalReportOutputAuthorization.ZExportPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalReportOutputAuthorization.ZExportPermission));
            options.AddPolicy(
                BirSalesSummaryAuthorization.GeneratePolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, BirSalesSummaryAuthorization.GeneratePermission));
            options.AddPolicy(
                BirSalesSummaryAuthorization.ReadPolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, BirSalesSummaryAuthorization.ReadPermission));
            options.AddPolicy(
                BirSalesSummaryAuthorization.ExportPolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, BirSalesSummaryAuthorization.ExportPermission));
            options.AddPolicy(
                ElectronicJournalAuthorization.ReadPolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, ElectronicJournalAuthorization.ReadPermission));
            options.AddPolicy(
                ElectronicJournalAuthorization.ExportPolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, ElectronicJournalAuthorization.ExportPermission));
            options.AddPolicy(
                ElectronicJournalAuthorization.IntegrityPolicyName,
                policy => policy.AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, ElectronicJournalAuthorization.IntegrityPermission));
            options.AddPolicy(
                FiscalDocumentReprintAuthorization.RecordPolicyName,
                policy => policy
                    .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
                    .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, FiscalDocumentReprintAuthorization.RecordPermission));
            AddAnnexE1Policy(options, AnnexE1Authorization.RecordFactPolicy, AnnexE1Authorization.RecordFactPermission);
            AddAnnexE1Policy(options, AnnexE1Authorization.AttestZeroPolicy, AnnexE1Authorization.AttestZeroPermission);
            AddAnnexE1Policy(options, AnnexE1Authorization.GeneratePolicy, AnnexE1Authorization.GeneratePermission);
            AddAnnexE1Policy(options, AnnexE1Authorization.ReadPolicy, AnnexE1Authorization.ReadPermission);
            AddAnnexE1Policy(options, AnnexE1Authorization.DownloadPolicy, AnnexE1Authorization.DownloadPermission);
            AddAnnexE1Policy(options, AnnexE1Authorization.CorrectPolicy, AnnexE1Authorization.CorrectPermission);
        });

        return services;
    }

    private static void AddAnnexE1Policy(Microsoft.AspNetCore.Authorization.AuthorizationOptions options, string name, string permission) =>
        options.AddPolicy(name, policy => policy
            .AddAuthenticationSchemes(SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme)
            .RequireClaim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission));

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
