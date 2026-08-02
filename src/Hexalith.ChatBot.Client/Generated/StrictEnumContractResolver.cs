using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Hexalith.ChatBot.Client.Generated;

/// <summary>Overrides NSwag's per-property enum converter with the strict generated-client wire contract.</summary>
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
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
        {
            return type.GetGenericArguments()[1];
        }

        foreach (Type candidate in type.GetInterfaces())
        {
            if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                return candidate.GetGenericArguments()[1];
            }
        }

        return null;
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
