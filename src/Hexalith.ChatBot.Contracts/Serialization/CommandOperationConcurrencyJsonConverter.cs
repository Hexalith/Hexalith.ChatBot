using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.ChatBot.Contracts.Serialization;

/// <summary>Preserves immutable revision guards without defaulting a missing revision to zero.</summary>
internal sealed class CommandOperationConcurrencyJsonConverter : JsonConverter<CommandOperationConcurrency>
{
    /// <inheritdoc/>
    public override bool HandleNull => true;

    /// <inheritdoc/>
    public override CommandOperationConcurrency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A revision guard object is required.");
        }
        long? expectedRevision = null;
        string? decisionSlotId = null;
        bool decisionSlotSeen = false;
        string revisionName = Name(nameof(CommandOperationConcurrency.ExpectedRevision), options);
        string slotName = Name(nameof(CommandOperationConcurrency.DecisionSlotId), options);
        StringComparison comparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (property.Name.Equals(revisionName, comparison))
            {
                if (expectedRevision is not null || property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt64(out long revision) || revision < 0)
                {
                    throw new JsonException("The expected revision must be a nonnegative integer.");
                }
                expectedRevision = revision;
            }
            else if (property.Name.Equals(slotName, comparison))
            {
                if (decisionSlotSeen || property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                {
                    throw new JsonException("The decision slot must occur once and be a string or null.");
                }
                decisionSlotSeen = true;
                decisionSlotId = property.Value.ValueKind == JsonValueKind.Null ? null : property.Value.GetString();
            }
            else
            {
                throw new JsonException("Unknown revision guard member.");
            }
        }
        if (expectedRevision is null)
        {
            throw new JsonException("The expected revision is required.");
        }
        try
        {
            return new(expectedRevision, decisionSlotId);
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Invalid revision guard metadata.", exception);
        }
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, CommandOperationConcurrency value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartObject();
        writer.WriteNumber(Name(nameof(CommandOperationConcurrency.ExpectedRevision), options), value.ExpectedRevision);
        if (value.DecisionSlotId is not null)
        {
            writer.WriteString(Name(nameof(CommandOperationConcurrency.DecisionSlotId), options), value.DecisionSlotId);
        }
        writer.WriteEndObject();
    }

    private static string Name(string name, JsonSerializerOptions options) => options.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
