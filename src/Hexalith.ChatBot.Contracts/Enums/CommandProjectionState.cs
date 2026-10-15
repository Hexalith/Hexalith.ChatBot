using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>The canonical projection lifecycle, independent of execution.</summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<CommandProjectionState>))]
public enum CommandProjectionState
{
    /// <summary>The ProjectionPending state.</summary>
    [EnumMember(Value = "ProjectionPending")]
    ProjectionPending,
    /// <summary>The Projected state.</summary>
    [EnumMember(Value = "Projected")]
    Projected,
    /// <summary>The ProjectionLagging state.</summary>
    [EnumMember(Value = "ProjectionLagging")]
    ProjectionLagging,
}
