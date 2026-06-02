using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite FR74 governance control state for a command capability (a first-party governed command <em>type</em>
/// that a tenant may submit through the command gateway). This is a dedicated command-capability control plane,
/// distinct from:
/// <list type="bullet">
/// <item>the global, static, all-tenant, code-level <c>ChatBotSpineCommandAllowlist</c> fail-closed boundary
/// (never tenant-mutable);</item>
/// <item>the Epic 5 per-grant <c>ServiceClientGrant.AllowedCommandNames</c> scope (covers service/AI actors
/// only, never human submissions);</item>
/// <item>the per-actor control planes (<see cref="AiActorControlState"/>/<see cref="ServiceClientControlState"/>),
/// which disable an <em>actor</em>, not a command type.</item>
/// </list>
/// "Disable a command capability" marks that command type Disabled for the tenant so every actor's future
/// submission of that type fails closed at the actor-agnostic admission seam, while existing records stay
/// auditable. This state is set only through the security-sensitive two-person submit→approve disable path.
/// Members are append-only for wire/serialization stability (Story 7.22 appends <c>Quarantined</c>; do not
/// reorder).
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<CommandCapabilityControlState>))]
public enum CommandCapabilityControlState
{
    /// <summary>The command capability is admitted normally for the tenant.</summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>The command capability is disabled; future submissions of that command type fail closed while existing records stay auditable.</summary>
    [EnumMember(Value = "disabled")]
    Disabled,

    /// <summary>The command capability is quarantined for review; future submissions of that command type fail closed (contained for review) while existing records stay auditable.</summary>
    [EnumMember(Value = "quarantined")]
    Quarantined,
}
