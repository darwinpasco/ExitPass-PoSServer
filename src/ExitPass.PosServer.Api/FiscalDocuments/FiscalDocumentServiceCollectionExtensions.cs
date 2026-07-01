using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;

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
        }
        else if (!IsValidNpgsqlConnectionString(connectionString))
        {
            services.TryAddScoped<IFiscalDocumentRepository, InvalidPersistenceConfigurationFiscalDocumentRepository>();
            services.TryAddScoped<IFiscalDocumentReader, InvalidPersistenceConfigurationFiscalDocumentReader>();
        }
        else
        {
            services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
            services.TryAddScoped<IFiscalDocumentRepository, PostgresFiscalDocumentRepository>();
            services.TryAddScoped<IFiscalDocumentReader, PostgresFiscalDocumentReader>();
        }

        services.AddScoped<FiscalDocumentCreationService>();
        services.AddScoped<FiscalDocumentReadService>();

        return services;
    }

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
