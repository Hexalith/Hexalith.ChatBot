namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single bounded routing-matrix row: the <c>(state-class × scope)</c> key plus the declared recipient role and
/// channel tokens. All values are declared enum tokens, never recipient PII.
/// </summary>
public sealed record ChatBotNotificationRoutingMatrixRow(
    string StateClass,
    string Scope,
    string RecipientRole,
    string Channel);

/// <summary>
/// Contract bundle for the notification routing matrix editor (Story 7.6). Mirrors the Tenant Configuration S5
/// editor surface: a bounded <c>(state-class × scope)</c> grid with role/channel selectors drawn from declared
/// enums, validation summary, reason-code entry, a governed submit with old→new diff, and a phone fallback.
/// </summary>
/// <param name="Validation">Validation summary and field association contract.</param>
/// <param name="Recovery">Save and recovery contract.</param>
/// <param name="SmallScreenFallback">Phone-limited fallback contract.</param>
/// <param name="DisabledSaveAction">Disabled submit action contract.</param>
/// <param name="FocusReturn">Focus-return contract for the diff/confirmation panel.</param>
/// <param name="RoutingMatrix">The bounded routing-matrix rows rendered as labelled grid rows.</param>
/// <param name="StateClassTokens">Declared state-class tokens available to the row selectors.</param>
/// <param name="ScopeTokens">Declared scope tokens available to the row selectors.</param>
/// <param name="RecipientRoleTokens">Declared recipient-role tokens available to the role selector.</param>
/// <param name="ChannelTokens">Declared channel tokens available to the channel selector.</param>
/// <param name="ShownRoutingMetadata">Safe snapshot metadata rows shown by the editor.</param>
/// <param name="RestrictedMarkers">Markers that must not appear in safe UI copy.</param>
public sealed record ChatBotNotificationRoutingEditorContract(
    ChatBotValidationErrorContract Validation,
    ChatBotRecoveryPatternContract Recovery,
    ChatBotSmallScreenFallbackContract SmallScreenFallback,
    ChatBotDisabledActionContract DisabledSaveAction,
    ChatBotFocusReturnContract FocusReturn,
    IReadOnlyList<ChatBotNotificationRoutingMatrixRow> RoutingMatrix,
    IReadOnlyList<string> StateClassTokens,
    IReadOnlyList<string> ScopeTokens,
    IReadOnlyList<string> RecipientRoleTokens,
    IReadOnlyList<string> ChannelTokens,
    IReadOnlyList<string> ShownRoutingMetadata,
    IReadOnlyList<string> RestrictedMarkers)
{
    /// <summary>Gets a value indicating whether the routing editor contract is complete and metadata-only.</summary>
    public bool IsComplete
        => Validation.IsComplete
            && Recovery.IsComplete
            && SmallScreenFallback.IsComplete
            && DisabledSaveAction.IsComplete
            && FocusReturn.IsComplete
            && RoutingMatrix is { Count: > 0 }
            && RoutingMatrix.All(static row =>
                !string.IsNullOrWhiteSpace(row.StateClass)
                && !string.IsNullOrWhiteSpace(row.Scope)
                && !string.IsNullOrWhiteSpace(row.RecipientRole)
                && !string.IsNullOrWhiteSpace(row.Channel))
            && StateClassTokens is { Count: > 0 }
            && ScopeTokens is { Count: > 0 }
            && RecipientRoleTokens is { Count: > 0 }
            && ChannelTokens is { Count: > 0 }
            && ShownRoutingMetadata is { Count: > 0 }
            && ShownRoutingMetadata.All(static value => !string.IsNullOrWhiteSpace(value))
            && RoutingMatrix.All(SelectorsBounded)
            && !ContainsRestrictedText;

    /// <summary>Gets a value indicating whether every matrix row draws its values from the declared token sets.</summary>
    public bool SelectorsAreBounded => RoutingMatrix.All(SelectorsBounded);

    /// <summary>Gets a value indicating whether restricted markers leak into visible metadata.</summary>
    public bool ContainsRestrictedText
        => RestrictedMarkers is not null
            && RestrictedMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker =>
                    ShownRoutingMetadata.Any(value => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    || RoutingMatrix.Any(row =>
                        row.StateClass.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.Scope.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.RecipientRole.Contains(marker, StringComparison.OrdinalIgnoreCase)
                        || row.Channel.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    private bool SelectorsBounded(ChatBotNotificationRoutingMatrixRow row)
        => StateClassTokens.Contains(row.StateClass, StringComparer.Ordinal)
            && ScopeTokens.Contains(row.Scope, StringComparer.Ordinal)
            && RecipientRoleTokens.Contains(row.RecipientRole, StringComparer.Ordinal)
            && ChannelTokens.Contains(row.Channel, StringComparer.Ordinal);

    /// <summary>Creates the default routing editor contract used by design and bUnit tests.</summary>
    /// <returns>A complete notification routing editor contract.</returns>
    public static ChatBotNotificationRoutingEditorContract CreateDefault()
    {
        var fields = new Dictionary<string, string>
        {
            ["routing-state-class"] = "routing-state-class-message",
            ["routing-channel"] = "routing-channel-message",
            ["routing-change-reason"] = "routing-change-reason-message",
        };

        string[] stateClassTokens = ["review-needed", "approval-pending", "failure", "degraded", "quarantine", "retry"];
        string[] scopeTokens = ["see-only", "operate", "policy", "mailbox", "compliance", "audit-obligation"];
        string[] recipientRoleTokens = ["tenant-admin", "mailbox-admin", "policy-admin", "compliance-admin", "operations-admin"];
        string[] channelTokens = ["in-app", "email", "webhook", "operator-alert"];

        ChatBotNotificationRoutingMatrixRow[] matrix =
        [
            new("review-needed", "see-only", "operations-admin", "in-app"),
            new("approval-pending", "policy", "policy-admin", "email"),
            new("failure", "operate", "operations-admin", "operator-alert"),
            new("degraded", "operate", "operations-admin", "operator-alert"),
            new("quarantine", "compliance", "compliance-admin", "email"),
            new("retry", "operate", "operations-admin", "in-app"),
        ];

        return new(
            new ChatBotValidationErrorContract(
                "notification-routing-validation-summary",
                "Notification routing validation summary",
                "notification-routing-validation-summary",
                fields.Keys.ToArray(),
                fields,
                "Review the validation summary before saving the routing map."),
            ChatBotRecoveryPatternContract.ForTenantConfiguration(
                "stale_data",
                "notification-routing-validation-summary",
                "before-fields",
                fields,
                ChatBotSaveConflictCause.StaleData,
                "Review the validation summary before saving the routing map.",
                ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]),
            ChatBotSmallScreenFallbackContract.CreatePhoneLimited(
                "Notification routing summary is available on phone.",
                "active-snapshot",
                ["review-summary", "submit-routing-change"],
                "Open notification routing editor",
                "Use a larger screen for dense routing edits.",
                "notification-routing-draft-preserved",
                "Dense routing controls are unavailable on this screen size; summary and safe submit action remain reachable."),
            ChatBotDisabledActionContract.CreateGovernedAction(
                "Save notification routing",
                "notification-routing-save-disabled-reason",
                "A valid reason and policy authority are required before saving."),
            ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel),
            matrix,
            stateClassTokens,
            scopeTokens,
            recipientRoleTokens,
            channelTokens,
            ["schema-version", "snapshot-id", "source-version", "routing-fingerprint", "safe-conflict-cause"],
            ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]);
    }
}
