using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>The canonical execution lifecycle, independent of projection.</summary>
[JsonConverter(typeof(JsonEnumMemberStringConverter<CommandExecutionState>))]
public enum CommandExecutionState
{
    /// <summary>The Received state.</summary>
    [EnumMember(Value = "Received")]
    Received,
    /// <summary>The Admitted state.</summary>
    [EnumMember(Value = "Admitted")]
    Admitted,
    /// <summary>The Committed state.</summary>
    [EnumMember(Value = "Committed")]
    Committed,
    /// <summary>The Rejected state.</summary>
    [EnumMember(Value = "Rejected")]
    Rejected,
    /// <summary>The Failed state.</summary>
    [EnumMember(Value = "Failed")]
    Failed,
}
