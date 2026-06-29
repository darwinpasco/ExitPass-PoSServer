using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentServiceCollectionExtensions
{
    public static IServiceCollection AddPosServerFiscalDocumentApi(this IServiceCollection services)
    {
        services.TryAddScoped<IFiscalDocumentRepository, PersistenceNotConfiguredFiscalDocumentRepository>();
        services.AddScoped<FiscalDocumentCreationService>();

        return services;
    }
}
