namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentReprintService(IFiscalDocumentReprintRepository repository)
{
    private static readonly HashSet<string> AllowedReasons = new(StringComparer.Ordinal)
    {
        "operator_request", "customer_request", "audit_request", "damaged_original"
    };

    public async Task<FiscalDocumentReprintResult> RecordAsync(
        FiscalDocumentReprintCommand command,
        CancellationToken cancellationToken = default)
    {
        if (ContainsLineBreak(command.OperationKey) || ContainsLineBreak(command.ActorReference) ||
            ContainsLineBreak(command.ServiceIdentityReference) || ContainsLineBreak(command.CorrelationReference))
            return Invalid("Fiscal reprint operation and attribution references are required and must be safe.");

        var normalized = command with
        {
            CurrencyCode = Normalize(command.CurrencyCode).ToUpperInvariant(),
            OperationKey = Normalize(command.OperationKey),
            ReasonCode = Normalize(command.ReasonCode).ToLowerInvariant(),
            ActorReference = Normalize(command.ActorReference),
            ServiceIdentityReference = Normalize(command.ServiceIdentityReference),
            CorrelationReference = Normalize(command.CorrelationReference)
        };
        var validation = Validate(normalized);
        if (validation is not null) return validation;

        return await repository.RecordAsync(
            normalized,
            FiscalDocumentReprintSemanticRequestHasher.Hash(normalized),
            cancellationToken).ConfigureAwait(false);
    }

    private static FiscalDocumentReprintResult? Validate(FiscalDocumentReprintCommand command)
    {
        if (command.FiscalDocumentId == Guid.Empty || command.SitePosServerId == Guid.Empty || command.FiscalIdentityId == Guid.Empty)
            return Invalid("The fiscal document and exact fiscal scope are required.");
        if (command.CurrencyCode.Length != 3 || command.CurrencyCode.Any(character => character is < 'A' or > 'Z'))
            return Invalid("A governed three-letter currency is required.");
        if (!AllowedReasons.Contains(command.ReasonCode))
            return Invalid("The fiscal reprint reason is unsupported.");
        if (InvalidReference(command.OperationKey) || InvalidReference(command.ActorReference) ||
            InvalidReference(command.ServiceIdentityReference) || InvalidReference(command.CorrelationReference))
            return Invalid("Fiscal reprint operation and attribution references are required and must be safe.");
        return null;
    }

    private static FiscalDocumentReprintResult Invalid(string message) =>
        new(FiscalDocumentReprintOutcome.InvalidRequest, SafeMessage: message);

    private static bool InvalidReference(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Any(character => character is '\r' or '\n');

    private static bool ContainsLineBreak(string? value) =>
        value?.Any(character => character is '\r' or '\n') == true;

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
}
