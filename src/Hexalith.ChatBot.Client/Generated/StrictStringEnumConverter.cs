using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Rejects integer enum wire values on read, and writes named values only.
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

        return base.ReadJson(reader, objectType, existingValue, serializer);
    }
}
