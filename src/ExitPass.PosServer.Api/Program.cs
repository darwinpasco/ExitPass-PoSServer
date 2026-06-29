using ExitPass.PosServer.Api.FiscalDocuments;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPosServerFiscalDocumentApi();

var app = builder.Build();
app.MapFiscalDocumentEndpoints();
app.Run();
