using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Gateway.Correlation;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>Checks the OpenAPI request metadata before any command admission work begins.</summary>
/// <remarks>
/// The OpenAPI <c>CommandSubmissionRequest</c> schema is the wire authority (AD-18). The values below restate it for
/// fast pre-admission validation; the conformance drift test binds every one of them to the YAML schema so a change
/// on either side fails before merge.
/// </remarks>
internal static partial class CommandSubmissionContractAdapter
{
    /// <summary>The <c>CommandSubmissionRequest</c> properties; any other root member is rejected.</summary>
    internal static readonly IReadOnlySet<string> RequestProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "commandId", "commandType", "command", "requestSchemaVersion", "actorType", "riskClass", "thresholdBand", "origin",
    };

    /// <summary>The <c>CommandSubmissionRequest</c> required properties, in schema order.</summary>
    internal static readonly IReadOnlyList<string> RequiredRequestProperties =
        ["commandId", "commandType", "command", "requestSchemaVersion"];

    /// <summary>The <c>commandType</c> maximum length.</summary>
    internal const int CommandTypeMaxLength = 160;

    /// <summary>The <c>commandType</c> pattern.</summary>
    internal const string CommandTypePatternText = "^[A-Z][A-Za-z0-9]*$";

    /// <summary>The forbidden <c>commandType</c> suffix (schema <c>not: pattern: "Command$"</c>).</summary>
    internal const string ForbiddenCommandTypeSuffix = "Command";

    /// <summary>The only accepted <c>requestSchemaVersion</c>.</summary>
    internal const string RequestSchemaVersion = "v1";

    /// <summary>The closed <c>ActorType</c> enum.</summary>
    internal static readonly IReadOnlySet<string> ActorTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "human", "ai", "service", "system",
    };

    /// <summary>The closed <c>RiskClass</c> enum.</summary>
    internal static readonly IReadOnlySet<string> RiskClasses = new HashSet<string>(StringComparer.Ordinal)
    {
        "none", "low", "medium", "high", "blocked",
    };

    /// <summary>The closed <c>ThresholdBand</c> enum.</summary>
    internal static readonly IReadOnlySet<string> ThresholdBands = new HashSet<string>(StringComparer.Ordinal)
    {
        "below", "within", "above", "critical",
    };

    /// <summary>The closed <c>SurfaceOrigin</c> enum.</summary>
    internal static readonly IReadOnlySet<string> SurfaceOrigins = new HashSet<string>(StringComparer.Ordinal)
    {
        "ui", "api", "cli", "mcp", "worker", "mailbox", "ai",
    };

    private static readonly NullabilityInfoContext Nullability = new();
    private static readonly Lock NullabilitySync = new();

    private static readonly JsonSerializerOptions CommandJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, Type> KnownCommandTypes = typeof(IChatBotCommand).Assembly.GetTypes()
        .Where(candidate => !candidate.IsAbstract && typeof(IChatBotCommand).IsAssignableFrom(candidate))
        .GroupBy(candidate => candidate.Name, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

    public static async Task<(CommandSubmissionRequest? Request, string? Origin)> ReadAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!string.Equals(
            context.Request.ContentType?.Split(';', 2)[0].Trim(),
            "application/json",
            StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        try
        {
            using JsonDocument document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            // Resolve correlation from a valid caller identity even when another field is malformed.
            if (root.TryGetProperty("commandId", out JsonElement idElement) &&
                idElement.ValueKind == JsonValueKind.String &&
                ChatBotCommandId.TryParse(idElement.GetString(), out ChatBotCommandId parsedId))
            {
                context.ResolveCorrelationContext(parsedId.Value);
            }

            if (root.EnumerateObject().Any(property => !RequestProperties.Contains(property.Name)) ||
                !RequiredString(root, "commandId", out string? commandId) ||
                !ChatBotCommandId.TryParse(commandId, out ChatBotCommandId commandIdentifier) ||
                !RequiredString(root, "commandType", out string? commandType) ||
                commandType!.Length > CommandTypeMaxLength ||
                CommandTypePattern().Match(commandType).Length != commandType.Length ||
                commandType.EndsWith(ForbiddenCommandTypeSuffix, StringComparison.Ordinal) ||
                !RequiredString(root, "requestSchemaVersion", out string? version) ||
                !string.Equals(version, RequestSchemaVersion, StringComparison.Ordinal) ||
                !root.TryGetProperty("command", out JsonElement command) ||
                command.ValueKind != JsonValueKind.Object ||
                !OptionalEnum(root, "actorType", ActorTypes) ||
                !OptionalEnum(root, "riskClass", RiskClasses) ||
                !OptionalEnum(root, "thresholdBand", ThresholdBands) ||
                !OptionalEnum(root, "origin", SurfaceOrigins) ||
                !ValidateKnownCommand(commandType, command))
            {
                return (null, null);
            }

            string? origin = root.TryGetProperty("origin", out JsonElement originElement)
                ? originElement.GetString()
                : null;
            return (new CommandSubmissionRequest
            {
                CommandId = commandIdentifier.Value,
                CommandType = commandType,
                Command = command.Clone(),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            }, origin);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static bool RequiredString(JsonElement root, string name, out string? value)
    {
        value = root.TryGetProperty(name, out JsonElement element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool OptionalEnum(JsonElement root, string name, IReadOnlySet<string> values)
        => !root.TryGetProperty(name, out JsonElement element) ||
            (element.ValueKind == JsonValueKind.String && values.Contains(element.GetString() ?? string.Empty));

    private static bool AllowsNull(ParameterInfo parameter)
    {
        lock (NullabilitySync)
        {
            return Nullability.Create(parameter).ReadState == NullabilityState.Nullable;
        }
    }

    private static bool AllowsNull(PropertyInfo property)
    {
        lock (NullabilitySync)
        {
            return Nullability.Create(property).WriteState == NullabilityState.Nullable;
        }
    }

    private static bool ValidateKnownCommand(string commandType, JsonElement command)
    {
        return !KnownCommandTypes.TryGetValue(commandType, out Type? type) ||
            ValidateValue(command, type, 0, allowNull: false);
    }

    private static bool ValidateValue(JsonElement element, Type type, int depth, bool allowNull)
    {
        if (depth > 12)
        {
            return false;
        }

        Type? nullable = Nullable.GetUnderlyingType(type);
        if (element.ValueKind == JsonValueKind.Null)
        {
            return nullable is not null || allowNull;
        }

        type = nullable ?? type;
        if (type == typeof(string) || type == typeof(DateTimeOffset) || type == typeof(DateTime))
        {
            return element.ValueKind == JsonValueKind.String &&
                (type == typeof(string) || element.TryGetDateTimeOffset(out _));
        }

        if (type.IsEnum)
        {
            if (element.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
            {
                return false;
            }

            try
            {
                object? parsed = element.Deserialize(type, CommandJsonOptions);
                return parsed is not null && Enum.IsDefined(type, parsed);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        if (type == typeof(bool))
        {
            return element.ValueKind is JsonValueKind.True or JsonValueKind.False;
        }

        if (type.IsPrimitive || type == typeof(decimal))
        {
            if (element.ValueKind != JsonValueKind.Number)
            {
                return false;
            }

            try
            {
                return element.Deserialize(type, CommandJsonOptions) is not null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
             type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
             type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];
            return element.ValueKind == JsonValueKind.Object &&
                !HasDuplicateProperties(element, keyType == typeof(string) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase) &&
                element.EnumerateObject().All(item =>
                    ValidateDictionaryKey(item.Name, keyType) &&
                    ValidateValue(item.Value, valueType, depth + 1, allowNull: false));
        }

        if (type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            Type? itemType = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
            return element.ValueKind == JsonValueKind.Array &&
                (itemType is null || element.EnumerateArray().All(item => ValidateValue(item, itemType, depth + 1, allowNull: false)));
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (HasDuplicateProperties(element, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        ConstructorInfo? constructor = type.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).FirstOrDefault();
        if (constructor is null)
        {
            return true;
        }

        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            string propertyName = JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!);
            bool present = element.TryGetProperty(propertyName, out JsonElement member);
            bool required = !parameter.HasDefaultValue &&
                !AllowsNull(parameter);
            if (!present)
            {
                if (required)
                {
                    return false;
                }

                continue;
            }

            if (member.ValueKind == JsonValueKind.Null)
            {
                if (required)
                {
                    return false;
                }

                continue;
            }

            if (!ValidateValue(member, parameter.ParameterType, depth + 1,
                AllowsNull(parameter)))
            {
                return false;
            }
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.SetMethod is null || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            string propertyName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (element.TryGetProperty(propertyName, out JsonElement member) &&
                !ValidateValue(member, property.PropertyType, depth + 1,
                    AllowsNull(property)))
            {
                return false;
            }
        }

        // Web deserialization binds members without regard to case. A differently cased
        // member can otherwise override a value that the exact-name validator checked.
        HashSet<string> canonicalNames = new(StringComparer.Ordinal);
        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            canonicalNames.Add(JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!));
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.SetMethod is not null && property.GetIndexParameters().Length == 0)
            {
                canonicalNames.Add(property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                    JsonNamingPolicy.CamelCase.ConvertName(property.Name));
            }
        }

        foreach (JsonProperty member in element.EnumerateObject())
        {
            if (!canonicalNames.Contains(member.Name) &&
                canonicalNames.Any(name => string.Equals(name, member.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasDuplicateProperties(JsonElement element, StringComparer comparer)
    {
        HashSet<string> names = new(comparer);
        return element.EnumerateObject().Any(property => !names.Add(property.Name));
    }

    private static bool ValidateDictionaryKey(string key, Type type)
    {
        if (type == typeof(string))
        {
            return true;
        }

        return type.IsEnum && type.GetFields(BindingFlags.Public | BindingFlags.Static).Any(field =>
            string.Equals(key, field.Name, StringComparison.Ordinal) ||
            string.Equals(key, field.GetCustomAttribute<EnumMemberAttribute>()?.Value, StringComparison.Ordinal));
    }

    [GeneratedRegex(CommandTypePatternText, RegexOptions.CultureInvariant)]
    private static partial Regex CommandTypePattern();
}
