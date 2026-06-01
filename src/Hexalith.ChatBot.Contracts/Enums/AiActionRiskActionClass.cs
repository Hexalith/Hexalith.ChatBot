using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AiActionRiskActionClass>))]
public enum AiActionRiskActionClass
{
    [EnumMember(Value = "modifies-state")]
    ModifiesState,

    [EnumMember(Value = "exposes-files")]
    ExposesFiles,

    [EnumMember(Value = "sends-external")]
    SendsExternal,

    [EnumMember(Value = "creates-tasks")]
    CreatesTasks,

    [EnumMember(Value = "invokes-tools")]
    InvokesTools,

    [EnumMember(Value = "acts-on-behalf")]
    ActsOnBehalf,
}
