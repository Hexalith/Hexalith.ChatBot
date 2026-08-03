using System.Collections.Immutable;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Overrides NSwag's per-property enum converter with the strict generated-client wire contract.
/// <para>
/// Ownership: this type is hand-maintained beside NSwag output under <c>Generated/</c>. NSwag regenerates only
/// <c>HexalithChatBotClient.g.cs</c> (see <c>nswag.json</c>); do not delete this file in a Generated wipe.
/// </para>
/// </summary>
internal sealed class StrictEnumContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        Type? propertyType = property.PropertyType;
        if (propertyType is null)
        {
            return property;
        }

        if (IsEnumType(propertyType))
        {
            property.Converter = new StrictStringEnumConverter();
            return property;
        }

        // Collections need the ITEM converter, not the property converter. NSwag emits
        // `ItemConverterType = typeof(StringEnumConverter)` — the permissive converter, AllowIntegerValues = true — on
        // every collection-of-enum property, and Newtonsoft resolves a collection's elements through
        // `containerProperty.ItemConverter` BEFORE consulting `settings.Converters`. Setting only `property.Converter`
        // therefore left every enum array accepting integer ordinals while its scalar siblings rejected them.
        // A dictionary's IEnumerable<> element type is KeyValuePair<,>, never the enum, so a
        // Dictionary<string, TEnum> fell through this check and kept NSwag's permissive ItemConverterType — it still
        // accepted integer ordinals while its scalar and array siblings rejected them. Latent today (the generated
        // client emits no such property), but nothing guarded the next regeneration.
        Type? dictionaryValueType = DictionaryValueType(propertyType);
        if (dictionaryValueType is not null && IsEnumType(dictionaryValueType))
        {
            property.ItemConverter = new StrictStringEnumConverter();
            return property;
        }

        Type? elementType = EnumerableElementType(propertyType);
        if (elementType is not null && IsEnumType(elementType))
        {
            property.ItemConverter = new StrictStringEnumConverter();
        }

        return property;
    }

    private static Type? DictionaryValueType(Type type)
    {
        if (TryDictionaryValueType(type, out Type? direct))
        {
            return direct;
        }

        foreach (Type candidate in type.GetInterfaces())
        {
            if (TryDictionaryValueType(candidate, out Type? fromInterface))
            {
                return fromInterface;
            }
        }

        return null;
    }

    private static bool TryDictionaryValueType(Type type, out Type? valueType)
    {
        valueType = null;
        if (!type.IsGenericType)
        {
            return false;
        }

        Type definition = type.GetGenericTypeDefinition();
        if (definition == typeof(IDictionary<,>) ||
            definition == typeof(IReadOnlyDictionary<,>) ||
            definition == typeof(IImmutableDictionary<,>))
        {
            valueType = type.GetGenericArguments()[1];
            return true;
        }

        return false;
    }

    private static bool IsEnumType(Type type)
        => type.IsEnum || Nullable.GetUnderlyingType(type)?.IsEnum is true;

    private static Type? EnumerableElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        foreach (Type candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return candidate.GetGenericArguments()[0];
            }
        }

        return null;
    }
}
