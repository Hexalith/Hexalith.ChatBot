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
        Type? elementType = EnumerableElementType(propertyType);
        if (elementType is not null && IsEnumType(elementType))
        {
            property.ItemConverter = new StrictStringEnumConverter();
        }

        return property;
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
