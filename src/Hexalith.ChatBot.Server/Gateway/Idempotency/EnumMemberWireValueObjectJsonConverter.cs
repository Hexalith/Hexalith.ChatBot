using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Server.Gateway.Idempotency;

/// <summary>
/// Persists a generated contract object nested in durable state with <see cref="CoarseIdempotencyStateJson.Options"/>,
/// so its generated enums are written as <c>[EnumMember]</c> wire values whatever serializer options the outer store
/// uses; integer tokens are still accepted but interpreted with the current generated numbering (correct only for
/// records written by the same enum generation).
/// </summary>
/// <typeparam name="T">The nested contract type.</typeparam>
/// <remarks>
/// Used on records persisted through the shared read-model store, whose DAPR client must keep the DAPR web defaults
/// required by the EventStore read-model batch seam.
/// </remarks>
internal sealed class EnumMemberWireValueObjectJsonConverter<T> : JsonConverter<T>
    where T : class
{
    /// <inheritdoc/>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<T>(ref reader, CoarseIdempotencyStateJson.Options);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, CoarseIdempotencyStateJson.Options);
}
