using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class AiActionCommandMetadataProvider
{
    public const string AppendConversationMessageCommandName = "Project.AppendConversationMessage";
    public const string ExecuteLowRiskAssistanceCommandName = "ChatBot.ExecuteLowRiskAssistance";
    public const string M0AllowlistVersion = "ai-action-command-allowlist.m0";
    public const string V1AllowlistVersion = "ai-action-command-allowlist.v1";

    public static AiActionCommandMetadata? TryGet(string commandName)
        => string.Equals(commandName, AppendConversationMessageCommandName, StringComparison.Ordinal)
            ? new AiActionCommandMetadata(
                AppendConversationMessageCommandName,
                [AiActionRiskActionClass.ModifiesState],
                "project-conversation",
                "approval-required",
                M0AllowlistVersion,
                AiActionRiskClass.ApprovalRequired,
                true,
                AiActionAuthorityClass.DelegatedProjectContributor,
                AiActionIdempotencyContract.CommandExecution)
            : string.Equals(commandName, ExecuteLowRiskAssistanceCommandName, StringComparison.Ordinal)
                ? new AiActionCommandMetadata(
                    ExecuteLowRiskAssistanceCommandName,
                    [],
                    "read-only",
                    "low-risk",
                    M0AllowlistVersion,
                    AiActionRiskClass.LowRisk,
                    true,
                    AiActionAuthorityClass.ReadOnlyAssistant,
                    AiActionIdempotencyContract.CommandExecution)
                : null;
}

/// <summary>
/// The finite authority class an AI actor must hold to invoke a governed command. Modelled as a closed token
/// set (mirroring <see cref="Hexalith.ChatBot.Contracts.Enums.SenderAuthorityClass"/>) rather than a free
/// string so allowlist v1 metadata cannot smuggle an unbounded authority claim. Server-internal: not a wire
/// contract.
/// </summary>
internal enum AiActionAuthorityClass
{
    /// <summary>Read-only assistance that mutates no durable state (e.g. summarise visible context).</summary>
    ReadOnlyAssistant,

    /// <summary>Acts under the requester's delegated project authority to mutate project-scoped state.</summary>
    DelegatedProjectContributor,
}

/// <summary>How a duplicate command-execution within the idempotency window is resolved. Closed token set.</summary>
internal enum AiActionIdempotencyResolution
{
    /// <summary>Return the prior outcome; do not re-execute (addendum §Idempotency Keys).</summary>
    ReturnPriorOutcome,
}

/// <summary>
/// The per-command idempotency contract for AI-action command execution (addendum §Idempotency Keys). The key
/// template, window, and duplicate-resolution behaviour are fixed safe tokens — never tenant-supplied.
/// </summary>
/// <param name="KeyTemplate">The idempotency-key composition for command execution.</param>
/// <param name="WindowSeconds">The dedup window in whole seconds (integer, never a float).</param>
/// <param name="OnDuplicate">The resolution applied to a duplicate within the window.</param>
internal sealed record AiActionIdempotencyContract(
    string KeyTemplate,
    int WindowSeconds,
    AiActionIdempotencyResolution OnDuplicate)
{
    /// <summary>
    /// Command-execution idempotency contract: <c>tenant_id + command_name + command_input_hash + requester_id</c>,
    /// 60-second window, "return prior outcome; do not re-execute" (addendum §Idempotency Keys).
    /// </summary>
    public static AiActionIdempotencyContract CommandExecution { get; } = new(
        "tenant_id+command_name+command_input_hash+requester_id",
        60,
        AiActionIdempotencyResolution.ReturnPriorOutcome);
}

internal sealed record AiActionCommandMetadata(
    string CommandName,
    IReadOnlyList<AiActionRiskActionClass> ActionClasses,
    string EffectSurface,
    string TenantPolicyClassification,
    string CommandAllowlistVersion,
    AiActionRiskClass CommandDefaultRisk,
    bool Supported,
    AiActionAuthorityClass RequiredAuthorityClass,
    AiActionIdempotencyContract IdempotencyContract);
