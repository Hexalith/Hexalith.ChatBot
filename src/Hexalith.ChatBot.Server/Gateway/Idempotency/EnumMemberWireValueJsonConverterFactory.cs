using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>
/// Creates an <see cref="EnumMemberWireValueJsonConverter{TEnum}"/> for every enum type; System.Text.Json wraps it for
/// nullable enums automatically.
/// </summary>
internal sealed class EnumMemberWireValueJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return (JsonConverter?)Activator.CreateInstance(
            typeof(EnumMemberWireValueJsonConverter<>).MakeGenericType(typeToConvert));
    }
}
