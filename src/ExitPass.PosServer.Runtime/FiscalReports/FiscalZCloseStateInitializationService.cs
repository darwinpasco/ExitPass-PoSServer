using System.Text.RegularExpressions;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed partial class FiscalZCloseStateInitializationService
{
    private readonly IFiscalZCloseStateRepository repository;

    public FiscalZCloseStateInitializationService(IFiscalZCloseStateRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<FiscalZCloseStateInitializationResult> InitializeAsync(
        InitializeFiscalZCloseStateCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(command);
        var failure = Validate(normalized);
        return failure is null
            ? repository.InitializeAsync(normalized, FiscalZCloseStateSemanticRequestHasher.Compute(normalized), cancellationToken)
            : Task.FromResult(failure);
    }

    private static InitializeFiscalZCloseStateCommand Normalize(InitializeFiscalZCloseStateCommand command) => command with
    {
        OperationReference = NormalizeReference(command.OperationReference),
        CurrencyCode = NormalizeReference(command.CurrencyCode).ToUpperInvariant(),
        Provenance = NormalizeReference(command.Provenance).ToLowerInvariant(),
        ApprovalReference = NormalizeReference(command.ApprovalReference),
        ActorReference = NormalizeReference(command.ActorReference),
        ServiceIdentityReference = NormalizeReference(command.ServiceIdentityReference),
        CorrelationReference = NormalizeReference(command.CorrelationReference)
    };

    private static FiscalZCloseStateInitializationResult? Validate(InitializeFiscalZCloseStateCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.OperationReference) ||
            command.SitePosServerId == Guid.Empty ||
            command.FiscalIdentityId == Guid.Empty ||
            !CurrencyPattern().IsMatch(command.CurrencyCode) ||
            string.IsNullOrWhiteSpace(command.ApprovalReference) ||
            string.IsNullOrWhiteSpace(command.ActorReference) ||
            string.IsNullOrWhiteSpace(command.ServiceIdentityReference) ||
            string.IsNullOrWhiteSpace(command.CorrelationReference) ||
            command.ResetCounterValue < 0 ||
            command.ZCounterValue < 0 ||
            command.GrandTotalAmountMinorUnits < 0)
        {
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.InvalidRequest,
                "Fiscal Z close state initialization request is invalid.");
        }

        if (command.Provenance is not FiscalZCloseStateContract.ApprovedNewScopeZero and not FiscalZCloseStateContract.VerifiedLegacyImport)
        {
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.InvalidProvenance,
                "Fiscal Z close state initialization provenance is unsupported.");
        }

        if (command.Provenance == FiscalZCloseStateContract.ApprovedNewScopeZero &&
            (command.ResetCounterValue != 0 || command.ZCounterValue != 0 || command.GrandTotalAmountMinorUnits != 0))
        {
            return FiscalZCloseStateInitializationResult.Failure(
                FiscalZCloseStateInitializationErrorCode.InvalidRequest,
                "Approved new fiscal scope initialization requires zero state values.");
        }

        return null;
    }

    private static string NormalizeReference(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Trim().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyPattern();
}
