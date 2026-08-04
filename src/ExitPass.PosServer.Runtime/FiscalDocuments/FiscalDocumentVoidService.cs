using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed class FiscalDocumentVoidService
{
    private static readonly HashSet<string> AllowedReasonCodes = new(StringComparer.Ordinal)
    {
        "customer_request",
        "operator_error",
        "duplicate_issuance",
        "wrong_document_facts",
        "other"
    };

    private static readonly string[] SensitiveReasonTextMarkers =
    [
        "raw_id",
        "id_image",
        "identity_document",
        "evidence_payload",
        "evidence_image",
        "credential",
        "payment_payload",
        "provider_callback",
        "card_number",
        "cvv",
        "token",
        "secret"
    ];

    private readonly IFiscalDocumentRepository repository;

    public FiscalDocumentVoidService(IFiscalDocumentRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<FiscalDocumentVoidResult> VoidAsync(
        FiscalDocumentVoidCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedCommand = Normalize(command);
        var validationFailure = Validate(normalizedCommand);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        try
        {
            var idempotency = FiscalDocumentVoidIdempotencyResolver.Resolve(normalizedCommand);
            var persistenceResult = await repository
                .VoidAsync(normalizedCommand, idempotency, cancellationToken)
                .ConfigureAwait(false);

            return FiscalDocumentVoidResult.Success(
                persistenceResult.Record,
                persistenceResult.Outcome switch
                {
                    FiscalDocumentVoidPersistenceOutcome.NewlyVoided => "newly_voided",
                    FiscalDocumentVoidPersistenceOutcome.Replayed => "idempotent_replay",
                    FiscalDocumentVoidPersistenceOutcome.AlreadyVoided => "already_voided",
                    _ => "rejected"
                });
        }
        catch (FiscalDocumentVoidIdempotencyConflictException ex)
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.IdempotencyConflict,
                ex.Message,
                "conflict");
        }
        catch (FiscalDocumentVoidNotFoundException ex)
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.FiscalDocumentNotFound,
                ex.Message);
        }
        catch (FiscalDocumentVoidInvalidStateException ex)
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.InvalidStateTransition,
                ex.Message,
                "rejected");
        }
        catch (FiscalCloseBoundaryException ex)
        {
            return FiscalDocumentVoidResult.Failure(
                ex.ErrorCode switch
                {
                    FiscalCloseBoundaryErrorCode.ReportingPeriodUnavailable => FiscalDocumentVoidErrorCode.ReportingPeriodUnavailable,
                    FiscalCloseBoundaryErrorCode.ReportingPeriodAssignmentMismatch => FiscalDocumentVoidErrorCode.ReportingPeriodAssignmentMismatch,
                    FiscalCloseBoundaryErrorCode.UnsupportedCrossPeriodMutation or FiscalCloseBoundaryErrorCode.ReportingPeriodClosed =>
                        FiscalDocumentVoidErrorCode.UnsupportedCrossPeriodMutation,
                    FiscalCloseBoundaryErrorCode.LockTimeout => FiscalDocumentVoidErrorCode.FiscalCloseBoundaryLockTimeout,
                    _ => FiscalDocumentVoidErrorCode.FiscalCloseBoundaryRetryableConcurrencyFailure
                },
                ex.Message,
                "rejected");
        }
    }

    private static FiscalDocumentVoidCommand Normalize(FiscalDocumentVoidCommand command) =>
        command with
        {
            IdempotencyKey = NormalizeReference(command.IdempotencyKey) ?? string.Empty,
            ReasonCode = NormalizeReasonCode(command.ReasonCode) ?? string.Empty,
            ReasonText = NormalizeReference(command.ReasonText),
            RequestedByRef = NormalizeReference(command.RequestedByRef) ?? string.Empty,
            CorrelationId = NormalizeReference(command.CorrelationId) ?? string.Empty,
            SourceSystemRef = NormalizeReference(command.SourceSystemRef)
        };

    private static FiscalDocumentVoidResult? Validate(FiscalDocumentVoidCommand command)
    {
        if (command.FiscalDocumentId == Guid.Empty)
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.FiscalDocumentNotFound,
                "Fiscal document id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.MissingIdempotencyKey,
                "Fiscal document void requires an idempotency key.");
        }

        if (string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.MissingReasonCode,
                "Fiscal document void requires a reason code.");
        }

        if (!AllowedReasonCodes.Contains(command.ReasonCode))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.InvalidReasonCode,
                "Fiscal document void reason code is not supported by this runtime slice.");
        }

        if (ContainsSensitiveMarker(command.ReasonText))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.InvalidReasonCode,
                "Fiscal document void reason text must be reference-safe and must not contain raw evidence or credential payload markers.");
        }

        if (string.IsNullOrWhiteSpace(command.RequestedByRef))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.MissingRequestedByRef,
                "Fiscal document void requires requestedByRef.");
        }

        if (string.IsNullOrWhiteSpace(command.CorrelationId))
        {
            return FiscalDocumentVoidResult.Failure(
                FiscalDocumentVoidErrorCode.MissingCorrelationId,
                "Fiscal document void requires correlationId.");
        }

        return null;
    }

    private static string? NormalizeReasonCode(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();

    private static string? NormalizeReference(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(' ', value.Trim().Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ContainsSensitiveMarker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return SensitiveReasonTextMarkers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
