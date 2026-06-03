using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite FR74 governance control state for an outbound channel (the governed external-send path through which an
/// approved outbound draft leaves the project boundary — in M0/M1 the M365 mailbox outbound adapter, identified by
/// the safe <c>AdapterRef</c> token <c>adapter:mailbox-outbound</c>). This is a dedicated outbound-channel control
/// plane, distinct from:
/// <list type="bullet">
/// <item>the Epic 6 outbound <em>authority</em> path (<c>SenderAuthorityClass</c>/<c>OutboundSendAuthorityEvaluator</c>/
/// <c>OutboundDraftAuthorityEvaluator</c>), which governs <em>who/what may draft or send</em> — not whether a channel
/// is open;</item>
/// <item>the global, static, all-tenant, code-level <c>ChatBotSpineCommandAllowlist</c> fail-closed boundary
/// (never tenant-mutable);</item>
/// <item>the per-actor / command-capability control planes (<see cref="AiActorControlState"/>/
/// <see cref="ServiceClientControlState"/>/<see cref="CommandCapabilityControlState"/>), which disable an
/// <em>actor</em> or a command <em>type</em>, not an outbound channel.</item>
/// </list>
/// "Disable an outbound channel" marks that channel Disabled for the tenant so its future approved sends fail closed
/// at the outbound send seam (before the external adapter call), while pending drafts/approvals and all existing
/// records stay inspectable and auditable. This state is set only through the security-sensitive two-person
/// submit→approve disable path. Members are append-only for wire/serialization stability (Story 7.25 appends
/// <c>Quarantined</c>; do not reorder).
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<OutboundChannelControlState>))]
public enum OutboundChannelControlState
{
    /// <summary>The outbound channel is open; approved sends through it dispatch to the external adapter normally.</summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>The outbound channel is disabled; future approved sends through it fail closed at the send seam (no external dispatch) while existing records stay inspectable and auditable.</summary>
    [EnumMember(Value = "disabled")]
    Disabled,
}
