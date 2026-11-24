using System.Reflection;
using System.Runtime.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Rejects integer enum wire values on read, and writes named values only.
/// <para>
/// Ownership: hand-maintained beside NSwag output under <c>Generated/</c>. NSwag regenerates only
/// <c>HexalithChatBotClient.g.cs</c>; do not delete this file in a Generated wipe.
/// </para>
/// <para>
/// Scope: this converter governs scalar and recursive enum contracts, generated property/item overrides, and
/// enum dictionary names through <see cref="StrictEnumContractResolver"/>. Name matching itself remains
/// <see cref="Newtonsoft.Json.Converters.StringEnumConverter"/>'s — that
/// is, case-insensitive and accepting of the CLR member name as well as the declared <c>[EnumMember]</c> wire value.
/// Integer values and compound names on enums without <see cref="FlagsAttribute"/> are refused.
/// </para>
/// </summary>
internal sealed class StrictStringEnumConverter : StringEnumConverter
{
    public StrictStringEnumConverter()
    {
        AllowIntegerValues = false;
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            throw new JsonSerializationException("Integer enum wire values are not accepted.");
        }

        // The base converter maps an empty string to null for a nullable enum, so a malformed `"actorType": ""` read
        // as absent rather than as an error — the caller then saw a missing value it could not distinguish from one
        // the server never sent.
        if (reader.TokenType == JsonToken.String && reader.Value is string { Length: 0 })
        {
            throw new JsonSerializationException("Empty enum wire values are not accepted.");
        }

        Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
        if (reader.TokenType == JsonToken.String && reader.Value is string name && name.Contains(',') &&
            !enumType.IsDefined(typeof(FlagsAttribute), inherit: false) &&
            !IsDeclaredAlias(enumType, name))
        {
            throw new JsonSerializationException("Compound names are only accepted on flags enums.");
        }

        return base.ReadJson(reader, objectType, existingValue, serializer);
    }

    private static bool IsDeclaredAlias(Type enumType, string name)
        => enumType.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(static field => field.GetCustomAttribute<EnumMemberAttribute>()?.Value)
            .Any(alias => string.Equals(alias, name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(alias, name.Trim(), StringComparison.OrdinalIgnoreCase));
}
