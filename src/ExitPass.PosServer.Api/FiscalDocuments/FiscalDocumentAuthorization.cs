namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentAuthorization
{
    public const string CreatePolicyName = "FiscalDocumentCreate";
    public const string ReadPolicyName = "FiscalDocumentRead";
    public const string VoidPolicyName = "FiscalDocumentVoid";

    public const string CreatePermission = "fiscal_document.create";
    public const string ReadPermission = "fiscal_document.read";
    public const string VoidPermission = "fiscal_document.void";
}
