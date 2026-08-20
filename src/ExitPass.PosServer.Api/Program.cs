using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Api.ElectronicJournal;
using ExitPass.PosServer.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);
builder.Services
    .AddHealthChecks()
    .AddCheck<PosServerDatabaseHealthCheck>("pos_database", tags: ["ready"]);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapFiscalDocumentEndpoints();
app.MapSalesInvoiceHeaderProfileAdminEndpoints();
app.MapFiscalXReadingEndpoints();
app.MapFiscalZCloseStateInitializationEndpoints();
app.MapFiscalZReadingEndpoints();
app.MapFiscalReportOutputEndpoints();
app.MapBirSalesSummaryEndpoints();
app.MapElectronicJournalEndpoints();
app.MapAnnexE1Endpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
app.Run();

public partial class Program;
