using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Finite FR74 governance control state for a service client. Distinct from the Epic 5
/// <see cref="Identities.ServiceClientGrant.IsRevoked"/>/<see cref="Identities.ServiceClientGrant.ExpiresAt"/>
/// grant-lifecycle flags, which are Keycloak-claims-sourced (<c>ClaimsServiceClientGrantResolver</c>) and
/// externally owned: this state is set only through the security-sensitive two-person submit→approve disable
/// path, never the grant-lifecycle path. Shaped so Story 7.16 can add <c>Quarantined</c> and 7.17 (rate-limit)
/// can reuse the per-(subject × action) control pattern.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<ServiceClientControlState>))]
public enum ServiceClientControlState
{
    /// <summary>The service client's commands and queries are admitted normally.</summary>
    [EnumMember(Value = "active")]
    Active,

    /// <summary>The service client is disabled; future commands and queries fail closed while existing records stay auditable.</summary>
    [EnumMember(Value = "disabled")]
    Disabled,
}
