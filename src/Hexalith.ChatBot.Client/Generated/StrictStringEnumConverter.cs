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
/// Scope: this converter governs the values it is applied to. It reaches scalar enum properties through
/// <see cref="StrictEnumContractResolver"/>'s property converter and enum collection elements through that resolver's
/// item converter. Name matching itself remains <see cref="Newtonsoft.Json.Converters.StringEnumConverter"/>'s — that
/// is, case-insensitive and accepting of the CLR member name as well as the declared <c>[EnumMember]</c> wire value.
/// The strictness claimed here is about integers, not about name casing.
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

        return base.ReadJson(reader, objectType, existingValue, serializer);
    }
}
