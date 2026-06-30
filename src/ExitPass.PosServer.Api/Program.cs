using ExitPass.PosServer.Api.FiscalDocuments;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPosServerFiscalDocumentApi(builder.Configuration);

var app = builder.Build();
app.MapFiscalDocumentEndpoints();
app.Run();
