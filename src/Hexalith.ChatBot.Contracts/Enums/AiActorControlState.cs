using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite FR74 governance control state for an AI actor. This is a dedicated AI-actor control plane, distinct
/// from the Story 7.15 <see cref="ServiceClientControlState"/> plane and from the Epic 5
/// <see cref="Identities.ServiceClientGrant.IsRevoked"/>/<see cref="Identities.ServiceClientGrant.ExpiresAt"/>
/// grant-lifecycle flags, which are Keycloak-claims-sourced (<c>ClaimsServiceClientGrantResolver</c>) and
/// externally owned — even though an AI actor shares the <c>ServiceClientId</c> identifier space and the
/// <c>ServiceClientGrantValidator</c> seam with service clients (the two are distinguished only by the
/// <c>actor_type</c> claim). This state is set only through the security-sensitive two-person submit→approve
/// disable path, never the grant-lifecycle path. Shaped so Story 7.19 can append <c>Quarantined</c>.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<AiActorControlState>))]
public enum AiActorControlState
{
    /// <summary>The AI actor's proposals and commands are admitted normally.</summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>The AI actor is disabled; future proposals and commands fail closed while existing records stay auditable.</summary>
    [EnumMember(Value = "disabled")]
    Disabled,
}
