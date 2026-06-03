using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// Resolves a degraded/failed dependency to the <b>narrowest</b> identified scope (NFR41). Given the candidate
/// scope tokens in precedence order — workflow-item (narrowest) &lt; operation &lt; command-surface &lt;
/// service-client &lt; project &lt; mailbox &lt; tenant (broadest) — it returns the first non-empty <b>safe</b>
/// token as a <c>(DependencyScopeKind, "{scopeKind}:{token}")</c> pair. When no scope token is present it returns
/// <see cref="DependencyScopeKind.Unknown"/> with <c>scope:unknown</c> — fail-closed, never a fabricated broader
/// scope. Pure: no clock, no IO, deterministic given inputs.
/// </summary>
internal static class DependencyScopeResolver
{
    /// <summary>The fail-closed scope returned when no scope token is present.</summary>
    public const string UnknownScope = "scope:unknown";

    public static (DependencyScopeKind Kind, string Scope) Resolve(
        string? workflowItemRef,
        string? operationRef,
        string? commandSurfaceRef,
        string? serviceClientRef,
        string? projectRef,
        string? mailboxRef,
        string? tenantRef)
    {
        if (SafeToken(workflowItemRef) is { } workflowItem)
        {
            return (DependencyScopeKind.WorkflowItem, Compose(DependencyScopeKinds.WorkflowItem, workflowItem));
        }

        if (SafeToken(operationRef) is { } operation)
        {
            return (DependencyScopeKind.Operation, Compose(DependencyScopeKinds.Operation, operation));
        }

        if (SafeToken(commandSurfaceRef) is { } commandSurface)
        {
            return (DependencyScopeKind.CommandSurface, Compose(DependencyScopeKinds.CommandSurface, commandSurface));
        }

        if (SafeToken(serviceClientRef) is { } serviceClient)
        {
            return (DependencyScopeKind.ServiceClient, Compose(DependencyScopeKinds.ServiceClient, serviceClient));
        }

        if (SafeToken(projectRef) is { } project)
        {
            return (DependencyScopeKind.Project, Compose(DependencyScopeKinds.Project, project));
        }

        if (SafeToken(mailboxRef) is { } mailbox)
        {
            return (DependencyScopeKind.Mailbox, Compose(DependencyScopeKinds.Mailbox, mailbox));
        }

        if (SafeToken(tenantRef) is { } tenant)
        {
            return (DependencyScopeKind.Tenant, Compose(DependencyScopeKinds.Tenant, tenant));
        }

        return (DependencyScopeKind.Unknown, UnknownScope);
    }

    // Produces "{kindToken}:{value}"; if the caller already supplied a kind-namespaced token (e.g. "mailbox:ops"),
    // it is kept as-is rather than double-prefixed, so the composite scope stays a single clean safe token.
    private static string Compose(string kindToken, string value)
        => value.StartsWith(kindToken + ":", StringComparison.Ordinal)
            ? value
            : kindToken + ":" + value;

    private static string? SafeToken(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|')
                ? value
                : null;

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
