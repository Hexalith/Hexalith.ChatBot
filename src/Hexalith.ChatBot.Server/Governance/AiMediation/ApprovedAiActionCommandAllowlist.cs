namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed class ApprovedAiActionCommandAllowlist : IApprovedAiActionCommandAllowlist
{
    // M0 floor (Epic 4): exactly the single approval-required project command. Frozen — v1 work must never
    // mutate this set, so an already-pinned M0 tenant is unaffected.
    private static readonly HashSet<string> M0Commands =
        new(StringComparer.Ordinal)
        {
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
        };

    // v1 (M1): the AI-invocable subset of the governed-command catalog minus tenant `disallowed-for-AI`. v1
    // ADDS breadth over M0 (the read-only low-risk assistance command) without relaxing the version-gated check.
    // Membership is checked in (code + metadata) and only changes behind a new version constant (AC3).
    private static readonly HashSet<string> V1Commands =
        new(StringComparer.Ordinal)
        {
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
            AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
        };

    // The default version for an un-pinned tenant remains the M0 floor (fail-closed; never widen first).
    // Per-tenant promotion to v1 is the existing security-sensitive `allowlist.version-pin` two-person knob.
    public string CurrentVersion => AiActionCommandMetadataProvider.M0AllowlistVersion;

    public bool IsAllowed(string? commandName, string? allowlistVersion)
        => !string.IsNullOrWhiteSpace(commandName) &&
            ResolveSet(allowlistVersion) is { } members &&
            members.Contains(commandName);

    /// <summary>
    /// Resolves the exact, immutable command set for a requested allowlist version. An unknown version resolves
    /// to an empty set (fail-closed). Used by metadata-completeness coverage to assert every v1 member has
    /// non-null metadata.
    /// </summary>
    public static IReadOnlyCollection<string> ResolveMembers(string? allowlistVersion)
        => ResolveSet(allowlistVersion) ?? [];

    private static HashSet<string>? ResolveSet(string? allowlistVersion)
        => string.Equals(allowlistVersion, AiActionCommandMetadataProvider.M0AllowlistVersion, StringComparison.Ordinal)
            ? M0Commands
            : string.Equals(allowlistVersion, AiActionCommandMetadataProvider.V1AllowlistVersion, StringComparison.Ordinal)
                ? V1Commands
                : null;
}
