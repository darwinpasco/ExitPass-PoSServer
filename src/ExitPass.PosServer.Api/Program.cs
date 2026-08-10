using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Api.ElectronicJournal;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);

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
app.Run();
