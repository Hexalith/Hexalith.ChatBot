using System.Runtime.Serialization;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AssociationCorrectionKind>))]
public enum AssociationCorrectionKind
{
    [EnumMember(Value = "project-reassignment")]
    ProjectReassignment,
}
