using System.Text.Json;

using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ClaimsTenantBindingStage : ITenantBindingStage
{
    private static readonly string[] TenantClaimTypes = ["eventstore:tenant", "tenant"];
    private static readonly HashSet<string> TenantScopedIdentifierDomains =
        new(StringComparer.Ordinal)
        {
            "chatbot",
            "conversations",
            "folders",
            "operation-status",
            "parties",
            "policy",
            "project-conversation",
            "projects",
            "tenants",
        };

    public ValueTask<ChatBotTenantBindingResult> BindTenantAsync(
        ChatBotCommandSubmission submission,
        ChatBotAuthenticatedActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);

        string[] tenantClaims = TenantClaimTypes
            .SelectMany(type => actor.Principal.FindAll(type))
            .Select(static claim => claim.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tenantClaims.Length != 1 || !IsTenantIdentifierSafe(tenantClaims[0]))
        {
            return ValueTask.FromResult(ChatBotTenantBindingResult.Denied(ChatBotAuthorizationReasonCodes.TenantMissing));
        }

        string boundTenant = tenantClaims[0];
        if (CommandTenantTargets(submission.Request.Command).Any(target => !string.Equals(target, boundTenant, StringComparison.Ordinal)))
        {
            return ValueTask.FromResult(ChatBotTenantBindingResult.Denied(new ChatBotTenantBinding(boundTenant), ChatBotAuthorizationReasonCodes.TenantMismatch));
        }

        return ValueTask.FromResult(ChatBotTenantBindingResult.Bound(new ChatBotTenantBinding(boundTenant)));
    }

    private static bool IsTenantIdentifierSafe(string value)
        => value.Length <= 160 && value.All(static character => !char.IsControl(character) && !char.IsWhiteSpace(character));

    private static IEnumerable<string> CommandTenantTargets(object? command)
    {
        if (command is null)
        {
            yield break;
        }

        if (command is JsonElement element)
        {
            foreach (string value in JsonTenantTargets(element))
            {
                yield return value;
            }

            yield break;
        }

        foreach (System.Reflection.PropertyInfo property in command.GetType().GetProperties())
        {
            if (!property.CanRead || property.PropertyType != typeof(string))
            {
                continue;
            }

            if (property.GetValue(command) is string value && !string.IsNullOrWhiteSpace(value))
            {
                if (IsTenantProperty(property.Name))
                {
                    yield return value;
                }

                if (IsTenantScopedIdentifierProperty(property.Name) && TryReadTenantScopedIdentifierTenant(value, out string? tenantId))
                {
                    yield return tenantId!;
                }
            }
        }
    }

    private static IEnumerable<string> JsonTenantTargets(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                string? value = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value) && IsTenantProperty(property.Name))
                {
                    yield return value;
                }

                if (!string.IsNullOrWhiteSpace(value) &&
                    IsTenantScopedIdentifierProperty(property.Name) &&
                    TryReadTenantScopedIdentifierTenant(value, out string? tenantId))
                {
                    yield return tenantId!;
                }
            }

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (string value in JsonTenantTargets(property.Value))
                {
                    yield return value;
                }
            }
        }
    }

    private static bool IsTenantProperty(string name)
        => string.Equals(name, "tenantId", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("TenantId", StringComparison.Ordinal);

    private static bool IsTenantScopedIdentifierProperty(string name)
        => string.Equals(name, "id", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("Id", StringComparison.Ordinal) ||
            name.EndsWith("Identifier", StringComparison.Ordinal);

    private static bool TryReadTenantScopedIdentifierTenant(string value, out string? tenantId)
    {
        tenantId = null;
        string[] parts = value.Split(':', 3);
        if (parts.Length != 3 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]) || string.IsNullOrWhiteSpace(parts[2]))
        {
            return false;
        }

        if (!IsTenantIdentifierSafe(parts[0]))
        {
            return false;
        }

        // Compound domain identifiers (for example ai-proposal:composer-ai:<id>:<transition>) also appear in
        // ordinary *Id properties. Only interpret the first segment as a tenant when the second segment names one
        // of this host's tenant-scoped bounded contexts; otherwise legitimate governed identifiers would be rejected
        // as cross-tenant targets before project authorization can run.
        if (!TenantScopedIdentifierDomains.Contains(parts[1]))
        {
            return false;
        }

        tenantId = parts[0];
        return true;
    }
}
