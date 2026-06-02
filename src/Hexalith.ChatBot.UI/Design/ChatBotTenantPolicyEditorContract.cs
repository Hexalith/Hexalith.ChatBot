namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Contract bundle for the Tenant Configuration S5 tenant-policy editor.
/// </summary>
/// <param name="Validation">Validation summary and field association contract.</param>
/// <param name="Recovery">Save and recovery contract.</param>
/// <param name="SmallScreenFallback">Phone-limited fallback contract.</param>
/// <param name="DisabledSaveAction">Disabled save action contract.</param>
/// <param name="FocusReturn">Focus-return contract for approval or conflict panels.</param>
/// <param name="ShownPolicyMetadata">Safe metadata rows shown by the editor.</param>
/// <param name="RestrictedMarkers">Markers that must not appear in safe UI copy.</param>
public sealed record ChatBotTenantPolicyEditorContract(
    ChatBotValidationErrorContract Validation,
    ChatBotRecoveryPatternContract Recovery,
    ChatBotSmallScreenFallbackContract SmallScreenFallback,
    ChatBotDisabledActionContract DisabledSaveAction,
    ChatBotFocusReturnContract FocusReturn,
    IReadOnlyList<string> ShownPolicyMetadata,
    IReadOnlyList<string> RestrictedMarkers)
{
    /// <summary>Gets a value indicating whether the S5 editor contract is complete and metadata-only.</summary>
    public bool IsComplete
        => Validation.IsComplete
            && Recovery.IsComplete
            && SmallScreenFallback.IsComplete
            && DisabledSaveAction.IsComplete
            && FocusReturn.IsComplete
            && ShownPolicyMetadata is { Count: > 0 }
            && ShownPolicyMetadata.All(static value => !string.IsNullOrWhiteSpace(value))
            && !ContainsRestrictedText;

    /// <summary>Gets a value indicating whether restricted markers leak into visible metadata.</summary>
    public bool ContainsRestrictedText
        => RestrictedMarkers is not null
            && RestrictedMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker => ShownPolicyMetadata.Any(value => value.Contains(marker, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Creates the default S5 editor contract used by design and bUnit tests.</summary>
    /// <returns>A complete tenant-policy editor contract.</returns>
    public static ChatBotTenantPolicyEditorContract CreateDefault()
    {
        var fields = new Dictionary<string, string>
        {
            ["association-t-high"] = "association-t-high-message",
            ["ai-action-low-risk-allowed"] = "ai-action-low-risk-allowed-message",
            ["policy-change-reason"] = "policy-change-reason-message",
        };
        return new(
            new ChatBotValidationErrorContract(
                "tenant-policy-validation-summary",
                "Tenant policy validation summary",
                "tenant-policy-validation-summary",
                fields.Keys.ToArray(),
                fields,
                "Review the validation summary before saving the tenant policy."),
            ChatBotRecoveryPatternContract.ForTenantConfiguration(
                "stale_data",
                "tenant-policy-validation-summary",
                "before-fields",
                fields,
                ChatBotSaveConflictCause.StaleData,
                "Review the validation summary before saving the tenant policy.",
                ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]),
            ChatBotSmallScreenFallbackContract.CreatePhoneLimited(
                "Tenant policy summary is available on phone.",
                "pending-approval",
                ["review-summary", "approve-pending-policy"],
                "Open tenant policy editor",
                "Use a larger screen for dense policy editing.",
                "tenant-policy-draft-preserved",
                "Dense policy controls are unavailable on this screen size; summary and safe approval actions remain reachable."),
            ChatBotDisabledActionContract.CreateGovernedAction(
                "Save tenant policy",
                "tenant-policy-save-disabled-reason",
                "A valid reason and policy authority are required before saving."),
            ChatBotFocusReturnContract.ForOverlay(ChatBotOverlayKind.ReviewPanel),
            ["schema-version", "snapshot-id", "pending-approval", "changed-knobs", "safe-conflict-cause"],
            ["project name", "mailbox body", "provider payload", "raw claim", "token", "secret"]);
    }
}
