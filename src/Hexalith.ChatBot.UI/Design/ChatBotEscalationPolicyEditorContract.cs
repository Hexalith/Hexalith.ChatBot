namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single bounded escalation-matrix row: the <c>(state-class × scope)</c> key plus the bounded age threshold and
/// the declared severity, escalation-target role, and channel tokens. All values are declared enum tokens or bounded
/// integers, never recipient PII.
/// </summary>
public sealed record ChatBotEscalationPolicyMatrixRow(
    string StateClass,
    string Scope,
    int AgeThresholdSeconds,
    string SeverityThreshold,
    string EscalationTargetRole,
    string EscalationChannel);

/// <summary>
/// Contract bundle for the escalation-policy matrix editor (Story 7.7). Mirrors
/// <see cref="ChatBotNotificationRoutingEditorContract"/>: a bounded <c>(state-class × scope)</c> grid with a numeric
/// (bounded) age-threshold input plus severity/target-role/channel selectors drawn from declared enums, validation
/// summary, reason-code entry, a governed submit with old→new diff, and a phone fallback. State classes are
/// restricted to the five escalatable classes (<c>retry</c> is excluded).
/// </summary>
/// <param name="Validation">Validation summary and field association contract.</param>
/// <param name="Recovery">Save and recovery contract.</param>
/// <param name="SmallScreenFallback">Phone-limited fallback contract.</param>
/// <param name="DisabledSaveAction">Disabled submit action contract.</param>
/// <param name="FocusReturn">Focus-return contract for the diff/confirmation panel.</param>
/// <param name="EscalationMatrix">The bounded escalation-matrix rows rendered as labelled grid rows.</param>
/// <param name="StateClassTokens">Declared escalatable state-class tokens available to the row selectors.</param>
/// <param name="ScopeTokens">Declared scope tokens available to the row selectors.</param>
/// <param name="SeverityTokens">Declared severity tokens available to the severity selector.</param>
/// <param name="EscalationTargetRoleTokens">Declared target-role tokens available to the role selector.</param>
/// <param name="ChannelTokens">Declared channel tokens available to the channel selector.</param>
/// <param name="ShownEscalationMetadata">Safe snapshot metadata rows shown by the editor.</param>
/// <param name="RestrictedMarkers">Markers that must not appear in safe UI copy.</param>
public sealed record ChatBotEscalationPolicyEditorContract(
    ChatBotValidationErrorContract Validation,
    ChatBotRecoveryPatternContract Recovery,
    ChatBotSmallScreenFallbackContract SmallScreenFallback,
    ChatBotDisabledActionContract DisabledSaveAction,
    ChatBotFocusReturnContract FocusReturn,
    IReadOnlyList<ChatBotEscalationPolicyMatrixRow> EscalationMatrix,
    IReadOnlyList<string> StateClassTokens,
    IReadOnlyList<string> ScopeTokens,
    IReadOnlyList<string> SeverityTokens,
    IReadOnlyList<string> EscalationTargetRoleTokens,
    IReadOnlyList<string> ChannelTokens,
    IReadOnlyList<string> ShownEscalationMetadata,
    IReadOnlyList<string> RestrictedMarkers)
{
    /// <summary>Gets a value indicating whether the escalation editor contract is complete and metadata-only.</summary>
    public bool IsComplete
        => Validation.IsComplete
            && Recovery.IsComplete
            && SmallScreenFallback.IsComplete
            && DisabledSaveAction.IsComplete
            && FocusReturn.IsComplete
            && EscalationMatrix is { Count: > 0 }
            && EscalationMatrix.All(static row =>
                !string.IsNullOrWhiteSpace(row.StateClass)
                && !string.IsNullOrWhiteSpace(row.Scope)
                && !string.IsNullOrWhiteSpace(row.SeverityThreshold)
                && !string.IsNullOrWhiteSpace(row.EscalationTargetRole)
                && !string.IsNullOrWhiteSpace(row.EscalationChannel))
            && StateClassTokens is { Count: > 0 }
            && ScopeTokens is { Count: > 0 }
            && SeverityTokens is { Count: > 0 }
            && EscalationTargetRoleTokens is { Count: > 0 }
            && ChannelTokens is { Count: > 0 }
            && ShownEscalationMetadata is { Count: > 0 }
            && ShownEscalationMetadata.All(static value => !string.IsNullOrWhiteSpace(value))
            && EscalationMatrix.All(SelectorsBounded)
            && !ContainsRestrictedText;

    /// <summary>Gets a value indicating whether every matrix row draws its values from the declared token sets and bounded ranges.</summary>
    public bool SelectorsAreBounded => EscalationMatrix.All(SelectorsBounded);

    /// <summary>Gets a value indicating whether restricted markers leak into visible metadata.</summary>
    public bool ContainsRestrictedText
        => RestrictedMarkers is not null
            && RestrictedMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker =>
                    ShownEscalationMetadata.Any(value => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    || EscalationMatrix.Any(row =>
                        row.StateClass.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.Scope.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.SeverityThreshold.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.EscalationTargetRole.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.EscalationChannel.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    private bool SelectorsBounded(ChatBotEscalationPolicyMatrixRow row)
        => StateClassTokens.Contains(row.StateClass, StringComparer.Ordinal)
            && ScopeTokens.Contains(row.Scope, StringComparer.Ordinal)
            && SeverityTokens.Contains(row.SeverityThreshold, StringComparer.Ordinal)
            && EscalationTargetRoleTokens.Contains(row.EscalationTargetRole, StringComparer.Ordinal)
            && ChannelTokens.Contains(row.EscalationChannel, StringComparer.Ordinal)
            && row.AgeThresholdSeconds >= 0;

    /// <summary>Creates the default escalation editor contract used by design and bUnit tests.</summary>
    /// <returns>A complete escalation-policy editor contract.</returns>
    public static ChatBotEscalationPolicyEditorContract CreateDefault()
    {
        var fields = new Dictionary<string, string>
        {
            ["escalation-state-class"] = "escalation-state-class-message",
            ["escalation-age-threshold"] = "escalation-age-threshold-message",
            ["escalation-change-reason"] = "escalation-change-reason-message",
        };

        // State classes are restricted to the five escalatable classes; `retry` is deliberately excluded.
        string[] stateClassTokens = ["review-needed", "approval-pending", "failure", "degraded", "quarantine"];
        string[] scopeTokens = ["see-only", "operate", "policy", "mailbox", "compliance", "audit-obligation"];
        string[] severityTokens = ["low", "medium", "high"];
        string[] targetRoleTokens = ["tenant-admin", "mailbox-admin", "policy-admin", "compliance-admin", "operations-admin"];
        string[] channelTokens = ["in-app", "email", "webhook", "operator-alert"];

        ChatBotEscalationPolicyMatrixRow[] matrix =
        [
            new("review-needed", "see-only", 86400, "high", "operations-admin", "in-app"),
            new("approval-pending", "policy", 43200, "medium", "policy-admin", "email"),
            new("failure", "operate", 3600, "high", "operations-admin", "operator-alert"),
            new("degraded", "operate", 7200, "medium", "operations-admin", "operator-alert"),
            new("quarantine", "compliance", 1800, "high", "compliance-admin", "email"),
        ];

        return new(
            new ChatBotValidationErrorContract(
                "escalation-policy-validation-summary",
                "Escalation policy validation summary",
                "escalation-policy-validation-summary",
                fields.Keys.ToArray(),
                fields,
                "Review the validation summary before saving the escalation policy."),
            ChatBotRecoveryPatternContract.ForTenantConfiguration(
                "stale_data",
                "escalation-policy-validation-summary",
                "before-fields",
                fields,
                ChatBotSaveConflictCause.StaleData,
                "Review the validation summary before saving the escalation policy.",
                ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]),
            ChatBotSmallScreenFallbackContract.CreatePhoneLimited(
                "Escalation policy summary is available on phone.",
                "active-snapshot",
                ["review-summary", "submit-escalation-change"],
                "Open escalation policy editor",
                "Use a larger screen for dense escalation edits.",
                "escalation-policy-draft-preserved",
                "Dense escalation controls are unavailable on this screen size; summary and safe submit action remain reachable."),
            ChatBotDisabledActionContract.CreateGovernedAction(
                "Save escalation policy",
                "escalation-policy-save-disabled-reason",
                "A valid reason and policy authority are required before saving."),
            ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel),
            matrix,
            stateClassTokens,
            scopeTokens,
            severityTokens,
            targetRoleTokens,
            channelTokens,
            ["schema-version", "snapshot-id", "source-version", "escalation-fingerprint", "safe-conflict-cause"],
            ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]);
    }
}
