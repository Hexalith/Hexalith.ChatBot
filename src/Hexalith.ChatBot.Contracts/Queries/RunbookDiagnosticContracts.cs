using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// The deterministic NFR44 completeness report for a sampled set of <see cref="OperationalQueueDiagnostics"/>:
/// how many were sampled, how many render a complete runbook diagnostic, and the workflow-item refs of the ones
/// that do not (the defects). It mechanically encodes the NFR44 observable — "each of N sampled items renders a
/// complete diagnostic; any missing field is a defect".
/// </summary>
/// <param name="Sampled">The number of diagnostics evaluated.</param>
/// <param name="Complete">The number with no missing/placeholder field.</param>
/// <param name="DefectWorkflowItemRefs">The workflow-item refs of the incomplete diagnostics, in input order.</param>
public sealed record RunbookDiagnosticCompletenessReport(
    int Sampled,
    int Complete,
    IReadOnlyList<string> DefectWorkflowItemRefs);

/// <summary>
/// Validates that an <see cref="OperationalQueueDiagnostics"/> is runbook-real (NFR44): every required field is a
/// safe token that is neither empty, the fail-closed <c>unknown</c> placeholder, nor a legacy stub prefix
/// (<c>correlation:</c>, <c>tenant:current</c>, <c>last-transition:</c>); <see cref="OperationalQueueDiagnostics.LastTransition"/>
/// parses into all three <c>from</c>/<c>actor</c>/<c>at</c> components (none <c>unknown</c>, a real epoch);
/// <see cref="OperationalQueueDiagnostics.RetryCount"/> is non-negative; and
/// <see cref="OperationalQueueDiagnostics.FailureReason"/> is null or a FR77 catalog reason code. A stale stub is a
/// real defect, surfaced — not hidden.
/// </summary>
public static class RunbookDiagnosticCompletenessValidator
{
    /// <summary>The fail-closed placeholder emitted for a genuinely-absent diagnostic component; always a defect.</summary>
    public const string UnknownPlaceholder = "unknown";

    /// <summary>
    /// Returns the names of the fields that are missing, placeholder, or otherwise not runbook-real. An empty list
    /// means the diagnostic is complete.
    /// </summary>
    public static IReadOnlyList<string> Validate(OperationalQueueDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        List<string> defects = [];

        if (!IsRealToken(diagnostics.CorrelationId))
        {
            defects.Add(nameof(diagnostics.CorrelationId));
        }

        if (!IsRealToken(diagnostics.TenantRef))
        {
            defects.Add(nameof(diagnostics.TenantRef));
        }

        if (!IsRealToken(diagnostics.WorkflowItemRef))
        {
            defects.Add(nameof(diagnostics.WorkflowItemRef));
        }

        if (!IsRealToken(diagnostics.CurrentState))
        {
            defects.Add(nameof(diagnostics.CurrentState));
        }

        if (!IsRealToken(diagnostics.NextSafeAction))
        {
            defects.Add(nameof(diagnostics.NextSafeAction));
        }

        // MailboxRef is optional (a non-mailbox item legitimately omits it); when present it must still be runbook-real.
        if (diagnostics.MailboxRef is not null && !IsRealToken(diagnostics.MailboxRef))
        {
            defects.Add(nameof(diagnostics.MailboxRef));
        }

        if (!IsCompleteTransition(diagnostics.LastTransition))
        {
            defects.Add(nameof(diagnostics.LastTransition));
        }

        if (diagnostics.RetryCount < 0)
        {
            defects.Add(nameof(diagnostics.RetryCount));
        }

        // A non-failed item carries a null FailureReason (allowed); a present one must be a FR77 catalog code.
        if (diagnostics.FailureReason is not null &&
            (!IsSafeToken(diagnostics.FailureReason) || !ChatBotMessageCodes.All.Contains(diagnostics.FailureReason)))
        {
            defects.Add(nameof(diagnostics.FailureReason));
        }

        return defects;
    }

    public static bool IsComplete(OperationalQueueDiagnostics diagnostics)
        => Validate(diagnostics).Count == 0;

    /// <summary>
    /// Evaluates an already-selected sample (no RNG here — the caller supplies the NFR44 weekly random sample) and
    /// returns the deterministic completeness report.
    /// </summary>
    public static RunbookDiagnosticCompletenessReport EvaluateSample(IReadOnlyList<OperationalQueueDiagnostics> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        int complete = 0;
        List<string> defects = [];
        foreach (OperationalQueueDiagnostics item in diagnostics)
        {
            if (IsComplete(item))
            {
                complete++;
            }
            else
            {
                defects.Add(IsSafeToken(item.WorkflowItemRef) && !string.IsNullOrWhiteSpace(item.WorkflowItemRef)
                    ? item.WorkflowItemRef
                    : UnknownPlaceholder);
            }
        }

        return new RunbookDiagnosticCompletenessReport(diagnostics.Count, complete, defects);
    }

    private static bool IsCompleteTransition(string? lastTransition)
    {
        if (!IsRequiredSafeToken(lastTransition) || IsLegacyStub(lastTransition))
        {
            return false;
        }

        string[] parts = lastTransition!.Split('|');
        if (parts.Length != 3 ||
            !parts[0].StartsWith("from:", StringComparison.Ordinal) ||
            !parts[1].StartsWith("actor:", StringComparison.Ordinal) ||
            !parts[2].StartsWith("at:", StringComparison.Ordinal))
        {
            return false;
        }

        string fromState = parts[0]["from:".Length..];
        string actor = parts[1]["actor:".Length..];
        string at = parts[2]["at:".Length..];

        return IsRealComponent(fromState) &&
            IsRealComponent(actor) &&
            long.TryParse(at, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long unixSeconds) &&
            unixSeconds > 0;
    }

    private static bool IsRealComponent(string value)
        => !string.IsNullOrWhiteSpace(value) && !string.Equals(value, UnknownPlaceholder, StringComparison.Ordinal);

    private static bool IsRealToken(string? value)
        => IsRequiredSafeToken(value) &&
            !string.Equals(value, UnknownPlaceholder, StringComparison.Ordinal) &&
            !IsLegacyStub(value);

    private static bool IsLegacyStub(string? value)
        => value is not null &&
            (value.StartsWith("correlation:", StringComparison.Ordinal) ||
                value.StartsWith("tenant:current", StringComparison.Ordinal) ||
                value.StartsWith("last-transition:", StringComparison.Ordinal));

    private static bool IsRequiredSafeToken(string? value)
        => !string.IsNullOrWhiteSpace(value) && IsSafeToken(value);

    private static bool IsSafeToken(string? value)
        => value is null ||
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|');

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
