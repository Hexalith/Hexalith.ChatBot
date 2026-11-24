using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Serialization;

/// <summary>
/// Serializes enum values using their stable <see cref="EnumMemberAttribute.Value"/> wire tokens.
/// </summary>
/// <typeparam name="TEnum">The enum type.</typeparam>
public sealed class JsonEnumMemberStringConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly IReadOnlyDictionary<TEnum, string> ValuesByEnum = BuildValuesByEnum();
    private static readonly IReadOnlyDictionary<string, TEnum> ValuesByToken = BuildValuesByToken();
    private static readonly IReadOnlyDictionary<string, TEnum> ValuesByWireToken = ValuesByEnum.ToDictionary(
        pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token for {typeof(TEnum).Name}.");
        }

        return ParseToken(reader.GetString(), ValuesByToken);
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);

        writer.WriteStringValue(WireToken(value));
    }

    /// <inheritdoc />
    public override TEnum ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);
        return ParseToken(reader.GetString(), ValuesByWireToken);
    }

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);
        writer.WritePropertyName(WireToken(value));
    }

    private static TEnum ParseToken(string? token, IReadOnlyDictionary<string, TEnum> tokens)
        => token is not null && tokens.TryGetValue(token, out TEnum value)
            ? value
            : throw new JsonException($"Unknown {typeof(TEnum).Name} value '{token}'.");

    private static string WireToken(TEnum value)
        => ValuesByEnum.TryGetValue(value, out string? token)
            ? token
            : throw new JsonException($"Unknown {typeof(TEnum).Name} value '{value}'.");

    private static IReadOnlyDictionary<TEnum, string> BuildValuesByEnum()
        => typeof(TEnum)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(static field => new
            {
                Value = (TEnum)field.GetValue(null)!,
                Token = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name,
            })
            .ToDictionary(static item => item.Value, static item => item.Token, EqualityComparer<TEnum>.Default);

    private static IReadOnlyDictionary<string, TEnum> BuildValuesByToken()
    {
        Dictionary<string, TEnum> values = new(StringComparer.Ordinal);
        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            TEnum value = (TEnum)field.GetValue(null)!;
            values[field.Name] = value;
            values[field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name] = value;
        }

        return values;
    }
}
