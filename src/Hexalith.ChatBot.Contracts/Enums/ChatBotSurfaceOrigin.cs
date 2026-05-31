using System.Runtime.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Closed set of adapter surfaces that may originate a governed command. Declared by the adapter at
/// the boundary as provenance (FR85 / S7) — not a server-inferred security control. M0 exercises
/// <see cref="Ui"/> and <see cref="Api"/>; the remaining members are reserved for later surfaces.
/// <see cref="Api"/> (the zero value) is the safe default for an absent or unknown declaration.
/// </summary>
public enum ChatBotSurfaceOrigin
{
    [EnumMember(Value = "api")]
    Api,

    [EnumMember(Value = "ui")]
    Ui,

    [EnumMember(Value = "cli")]
    Cli,

    [EnumMember(Value = "mcp")]
    Mcp,

    [EnumMember(Value = "worker")]
    Worker,

    [EnumMember(Value = "mailbox")]
    Mailbox,

    [EnumMember(Value = "ai")]
    Ai,
}
