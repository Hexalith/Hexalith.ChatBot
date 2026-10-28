using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Models a generated enum carrying a permissive converter attribute.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
internal enum PermissiveWireEnum
{
    [EnumMember(Value = "primary-state")]
    Primary,

    [EnumMember(Value = "secondary-state")]
    Secondary,
}
