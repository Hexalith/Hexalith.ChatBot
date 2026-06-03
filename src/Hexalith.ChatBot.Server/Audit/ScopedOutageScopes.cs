namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// The closed set of NFR41/NFR58 narrowest-scope axes a dependency outage may degrade (Story 9.13, AC1). A
/// <c>contained</c> verdict requires the observed degradation scope to stay within the expected narrowest scope; an
/// observed scope outside the expected scope is the NFR58 <c>scope_escape</c> breach. Mirrors the closed-set discipline
/// of <see cref="ScopedOutageDependencies"/> / <see cref="ContinuityDrillScenarios"/>: a fixed, bounded token vocabulary
/// with an <see cref="All"/> set and a null-safe <see cref="Contains"/> membership check, so an unknown/unsafe scope
/// token biases to <c>unmeasurable</c> (fail-safe), never a fabricated <c>contained</c>.
/// <para>
/// The literals deliberately avoid the legacy-lifecycle tokens so the scaffold-architecture guard does not flag them and
/// no allowlist entry is needed.
/// </para>
/// </summary>
internal static class ScopedOutageScopes
{
    /// <summary>The whole tenant is the affected scope (the broadest narrowest-scope axis).</summary>
    public const string Tenant = "tenant";

    /// <summary>A single mailbox is the affected scope (the M365/Graph mailbox boundary).</summary>
    public const string Mailbox = "mailbox";

    /// <summary>A single operation is the affected scope.</summary>
    public const string Operation = "operation";

    /// <summary>A single service-client is the affected scope.</summary>
    public const string ServiceClient = "service-client";

    /// <summary>A single command-surface is the affected scope.</summary>
    public const string CommandSurface = "command-surface";

    /// <summary>A single workflow-item is the affected scope (the narrowest axis).</summary>
    public const string WorkflowItem = "workflow-item";

    /// <summary>The closed set of all NFR41/NFR58 narrowest-scope axes.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Tenant,
            Mailbox,
            Operation,
            ServiceClient,
            CommandSurface,
            WorkflowItem,
        };

    /// <summary>Returns <see langword="true"/> only for a known scope token; any other value is unknown (fail-safe).</summary>
    public static bool Contains(string? scope)
        => scope is not null && All.Contains(scope);
}
