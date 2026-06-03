using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// The NFR41 dependency-isolation scope kinds, narrowest (<see cref="WorkflowItem"/>) to broadest
/// (<see cref="Tenant"/>), plus the fail-closed <see cref="Unknown"/> used when no scope token is present. A
/// degraded/failed dependency is isolated to the narrowest identified scope so the blast radius is contained;
/// <see cref="Unknown"/> is never a fabricated broader scope.
/// </summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<DependencyScopeKind>))]
public enum DependencyScopeKind
{
    [EnumMember(Value = "tenant")]
    Tenant,

    [EnumMember(Value = "mailbox")]
    Mailbox,

    [EnumMember(Value = "project")]
    Project,

    [EnumMember(Value = "operation")]
    Operation,

    [EnumMember(Value = "service-client")]
    ServiceClient,

    [EnumMember(Value = "workflow-item")]
    WorkflowItem,

    [EnumMember(Value = "command-surface")]
    CommandSurface,

    [EnumMember(Value = "unknown")]
    Unknown,
}
