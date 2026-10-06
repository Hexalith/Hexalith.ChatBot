using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>
/// Persists an enum as its stable <see cref="EnumMemberAttribute"/> wire value (or member name) and still accepts
/// integer tokens, which it interprets with the current generated numbering.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
/// <remarks>
/// The generated client enums carry Newtonsoft <c>[EnumMember]</c> wire values that System.Text.Json ignores, so
/// without this converter durable state stores bare ordinals that silently change meaning whenever generation inserts
/// or reorders a member. A value that is not a declared member is written as its number so it still round-trips.
/// An integer is decoded with the current generated numbering, which is correct only for records written by the same
/// enum generation: records persisted before a renumbering (such as this story's insertion of ten codes at the front
/// of <c>ChatBotMessageCode</c>) decode to different members. That is accepted while the product is pre-release.
/// </remarks>
internal sealed class EnumMemberWireValueJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly FrozenDictionary<TEnum, string> TokensByValue = BuildTokensByValue();
    private static readonly FrozenDictionary<string, TEnum> ValuesByToken = BuildValuesByToken();
    private static readonly bool IsUnsigned = Enum.GetUnderlyingType(typeof(TEnum)) is var underlying
        && (underlying == typeof(byte) || underlying == typeof(ushort) || underlying == typeof(uint) || underlying == typeof(ulong));

    /// <inheritdoc/>
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => FromToken(reader.GetString()),
            JsonTokenType.Number => FromNumber(ref reader),
            _ => throw new JsonException($"Expected a string or integer token for {typeof(TEnum).Name}."),
        };

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (TokensByValue.TryGetValue(value, out string? token))
        {
            writer.WriteStringValue(token);
            return;
        }

        if (IsUnsigned)
        {
            writer.WriteNumberValue(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
        }
        else
        {
            writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public override TEnum ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? name = reader.GetString();
        return name is not null && ValuesByToken.TryGetValue(name, out TEnum value)
            ? value
            : FromIntegerText(name);
    }

    /// <inheritdoc/>
    public override void WriteAsPropertyName(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WritePropertyName(TokensByValue.TryGetValue(value, out string? token)
            ? token
            : IsUnsigned
                ? Convert.ToUInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                : Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
    }

    private static TEnum FromToken(string? token)
        => token is not null && ValuesByToken.TryGetValue(token, out TEnum value)
            ? value
            : throw new JsonException($"Unknown {typeof(TEnum).Name} value '{token}'.");

    private static TEnum FromNumber(ref Utf8JsonReader reader)
    {
        if (IsUnsigned)
        {
            return reader.TryGetUInt64(out ulong unsignedValue)
                ? (TEnum)Enum.ToObject(typeof(TEnum), unsignedValue)
                : throw new JsonException($"Invalid {typeof(TEnum).Name} ordinal.");
        }

        return reader.TryGetInt64(out long signedValue)
            ? (TEnum)Enum.ToObject(typeof(TEnum), signedValue)
            : throw new JsonException($"Invalid {typeof(TEnum).Name} ordinal.");
    }

    private static TEnum FromIntegerText(string? text)
    {
        if (IsUnsigned && ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out ulong unsignedValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), unsignedValue);
        }

        if (!IsUnsigned && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long signedValue))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), signedValue);
        }

        throw new JsonException($"Unknown {typeof(TEnum).Name} value '{text}'.");
    }

    private static FrozenDictionary<TEnum, string> BuildTokensByValue()
    {
        Dictionary<TEnum, string> tokens = [];
        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            // The first declared alias of a value owns its token, matching enum declaration order.
            _ = tokens.TryAdd((TEnum)field.GetValue(null)!, field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name);
        }

        return tokens.ToFrozenDictionary();
    }

    private static FrozenDictionary<string, TEnum> BuildValuesByToken()
    {
        Dictionary<string, TEnum> values = new(StringComparer.Ordinal);
        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            TEnum value = (TEnum)field.GetValue(null)!;
            _ = values.TryAdd(field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name, value);
            _ = values.TryAdd(field.Name, value);
        }

        return values.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
