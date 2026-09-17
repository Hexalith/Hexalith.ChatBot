using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Queries;

[JsonConverter(typeof(JsonEnumMemberStringConverter<LowRiskAiAssistanceKind>))]
public enum LowRiskAiAssistanceKind
{
    [EnumMember(Value = "summarize-visible-context")]
    SummarizeVisibleContext,

    [EnumMember(Value = "explain-visible-evidence")]
    ExplainVisibleEvidence,
}
