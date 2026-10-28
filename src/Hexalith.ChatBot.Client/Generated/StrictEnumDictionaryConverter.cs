using System.Collections.Immutable;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Applies the strict named-enum policy to dictionary member names and delegates values back to the serializer.
/// </summary>
/// <typeparam name="TKey">The enum key type.</typeparam>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
internal sealed class StrictEnumDictionaryConverter<TKey, TValue> : JsonConverter
    where TKey : notnull
{
    private static readonly StrictStringEnumConverter KeyConverter = new();

    /// <summary>
    /// Initializes the converter selected by the strict contract resolver.
    /// </summary>
    public StrictEnumDictionaryConverter()
    {
    }

    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
        => typeof(IEnumerable<KeyValuePair<TKey, TValue>>).IsAssignableFrom(objectType);

    /// <inheritdoc />
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if ((reader.TokenType is JsonToken.None or JsonToken.Comment) && !ReadContent(reader))
        {
            throw new JsonSerializationException("Unexpected end of an enum-keyed dictionary.");
        }

        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonToken.StartObject)
        {
            throw new JsonSerializationException("An enum-keyed dictionary must be a JSON object.");
        }

        IDictionary<TKey, TValue> entries = existingValue is IDictionary<TKey, TValue> { IsReadOnly: false } existing
            ? existing
            : new Dictionary<TKey, TValue>();
        while (ReadContent(reader))
        {
            if (reader.TokenType == JsonToken.EndObject)
            {
                return Materialize(objectType, entries);
            }

            if (reader.TokenType != JsonToken.PropertyName || reader.Value is not string name)
            {
                throw new JsonSerializationException("An enum dictionary key must be a named wire value.");
            }

            TKey key = ReadKey(reader, name, serializer);
            if (!ReadContent(reader))
            {
                break;
            }

            entries[key] = serializer.Deserialize<TValue>(reader)!;
        }

        throw new JsonSerializationException("Unexpected end of an enum-keyed dictionary.");
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        foreach (KeyValuePair<TKey, TValue> entry in (IEnumerable<KeyValuePair<TKey, TValue>>)value)
        {
            using JTokenWriter keyWriter = new();
            KeyConverter.WriteJson(keyWriter, entry.Key, serializer);
            string name = keyWriter.Token?.Value<string>()
                ?? throw new JsonSerializationException("An enum dictionary key must be a named wire value.");
            writer.WritePropertyName(name);
            serializer.Serialize(writer, entry.Value, typeof(TValue));
        }

        writer.WriteEndObject();
    }

    private static TKey ReadKey(JsonReader reader, string name, JsonSerializer serializer)
    {
        using JsonReader keyReader = new JValue(name).CreateReader();
        keyReader.Read();
        try
        {
            return (TKey)KeyConverter.ReadJson(keyReader, typeof(TKey), null, serializer)!;
        }
        catch (JsonException exception)
        {
            // The isolated key reader must not replace the response location with an empty path.
            IJsonLineInfo? lineInfo = reader as IJsonLineInfo;
            throw new JsonSerializationException(
                "An enum dictionary key must be a named wire value.", reader.Path,
                lineInfo?.LineNumber ?? 0, lineInfo?.LinePosition ?? 0, exception);
        }
    }

    private static bool ReadContent(JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonToken.Comment)
            {
                return true;
            }
        }

        return false;
    }

    private static object Materialize(Type objectType, IDictionary<TKey, TValue> entries)
    {
        if (objectType.IsInstanceOfType(entries))
        {
            return entries;
        }

        if (typeof(IImmutableDictionary<TKey, TValue>).IsAssignableFrom(objectType))
        {
            ImmutableDictionary<TKey, TValue> immutable = entries.ToImmutableDictionary();
            if (objectType.IsInstanceOfType(immutable))
            {
                return immutable;
            }

            ImmutableSortedDictionary<TKey, TValue> sorted = entries.ToImmutableSortedDictionary();
            if (objectType.IsInstanceOfType(sorted))
            {
                return sorted;
            }
        }

        // Preserve the usual concrete and read-only dictionary shapes, without changing a shared
        // contract or temporarily removing this converter while recursive values are deserialized.
        ConstructorInfo? constructor = objectType.GetConstructor([typeof(IDictionary<TKey, TValue>)])
            ?? objectType.GetConstructor([typeof(IEnumerable<KeyValuePair<TKey, TValue>>)]);
        if (constructor is not null)
        {
            return constructor.Invoke([entries]);
        }

        if (!objectType.IsAbstract && objectType.GetConstructor(Type.EmptyTypes) is not null &&
            Activator.CreateInstance(objectType) is IDictionary<TKey, TValue> { IsReadOnly: false } mutable)
        {
            foreach (KeyValuePair<TKey, TValue> entry in entries)
            {
                mutable[entry.Key] = entry.Value;
            }

            return mutable;
        }

        throw new JsonSerializationException($"Cannot create enum-keyed dictionary type {objectType}.");
    }
}
