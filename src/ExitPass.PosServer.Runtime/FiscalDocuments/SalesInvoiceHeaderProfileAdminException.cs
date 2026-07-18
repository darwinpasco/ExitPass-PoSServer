namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class SalesInvoiceHeaderProfileAdminException : Exception
{
    public SalesInvoiceHeaderProfileAdminException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
