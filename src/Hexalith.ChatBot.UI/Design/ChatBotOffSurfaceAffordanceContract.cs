namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Redaction-safe contract for copy, export, read-aloud, handoff, audit, and evidence artifacts.
/// </summary>
/// <param name="Kind">Off-surface affordance kind.</param>
/// <param name="VisualText">Text shown on the visual surface.</param>
/// <param name="OffSurfaceText">Text prepared for clipboard, export, read-aloud, or handoff.</param>
/// <param name="RedactionState">Redaction state inherited from the visual surface.</param>
/// <param name="AccessibleName">Safe accessible name.</param>
/// <param name="AccessibleDescription">Safe accessible description.</param>
/// <param name="DisabledReason">Reachable disabled or restricted-use reason.</param>
/// <param name="EscalationGuidance">Guidance for full-detail escalation.</param>
/// <param name="RedactionNotice">Screen-reader-equivalent redaction notice.</param>
/// <param name="RestrictedSourceTextMarkers">Known raw source markers that must never appear off-surface.</param>
public sealed record ChatBotOffSurfaceAffordanceContract(
    ChatBotOffSurfaceAffordanceKind Kind,
    string VisualText,
    string OffSurfaceText,
    ChatBotOffSurfaceRedactionState RedactionState,
    string AccessibleName,
    string AccessibleDescription,
    string DisabledReason,
    string EscalationGuidance,
    string RedactionNotice,
    IReadOnlyList<string> RestrictedSourceTextMarkers)
{
    /// <summary>Gets a value indicating whether full source detail may be opened from this artifact.</summary>
    public bool CanOpenSourceDetail => RedactionState is ChatBotOffSurfaceRedactionState.Available;

    /// <summary>Gets a value indicating whether the state requires escalation to obtain full detail.</summary>
    public bool RequiresEscalationForFullDetail
        => RedactionState is ChatBotOffSurfaceRedactionState.Redacted or ChatBotOffSurfaceRedactionState.Unauthorized;

    /// <summary>Gets a value indicating whether any known restricted source text appears in artifact text.</summary>
    public bool ContainsRestrictedSourceText
        => RestrictedSourceTextMarkers is not null
            && RestrictedSourceTextMarkers
                .Where(static marker => !string.IsNullOrWhiteSpace(marker))
                .Any(marker =>
                    ContainsOrdinalIgnoreCase(VisualText, marker)
                    || ContainsOrdinalIgnoreCase(OffSurfaceText, marker)
                    || ContainsOrdinalIgnoreCase(AccessibleName, marker)
                    || ContainsOrdinalIgnoreCase(AccessibleDescription, marker)
                    || ContainsOrdinalIgnoreCase(DisabledReason, marker)
                    || ContainsOrdinalIgnoreCase(EscalationGuidance, marker)
                    || ContainsOrdinalIgnoreCase(RedactionNotice, marker));

    /// <summary>Gets a value indicating whether the off-surface artifact carries the visual display payload.</summary>
    public bool UsesVisualPayloadOffSurface
        => !string.IsNullOrWhiteSpace(VisualText)
            && ContainsOrdinalIgnoreCase(OffSurfaceText, VisualText);

    /// <summary>Gets a value indicating whether this artifact can be used by off-surface affordances.</summary>
    public bool IsSafeForOffSurfaceUse
        => !ContainsRestrictedSourceText
            && !string.IsNullOrWhiteSpace(VisualText)
            && !string.IsNullOrWhiteSpace(OffSurfaceText)
            && !string.IsNullOrWhiteSpace(AccessibleName)
            && !string.IsNullOrWhiteSpace(AccessibleDescription)
            && !string.IsNullOrWhiteSpace(DisabledReason)
            && !string.IsNullOrWhiteSpace(EscalationGuidance)
            && UsesVisualPayloadOffSurface
            && HasRequiredRedactionMessage;

    /// <summary>Gets a value indicating whether required artifact metadata is complete.</summary>
    public bool IsComplete => IsSafeForOffSurfaceUse;

    private bool HasRequiredRedactionMessage
        => !RequiresEscalationForFullDetail
            || (!string.IsNullOrWhiteSpace(RedactionNotice)
                && ContainsOrdinalIgnoreCase(OffSurfaceText, RedactionNotice)
                && ContainsOrdinalIgnoreCase(AccessibleDescription, RedactionNotice));

    private static bool ContainsOrdinalIgnoreCase(string? value, string marker)
        => value?.Contains(marker, StringComparison.OrdinalIgnoreCase) == true;
}
