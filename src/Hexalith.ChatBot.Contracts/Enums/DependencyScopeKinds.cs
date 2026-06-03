namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Stable wire tokens and ordered listing for <see cref="DependencyScopeKind"/>, mirroring the
/// <see cref="OperationalQueueFamilies"/> convention. <see cref="All"/> is ordered narrowest to broadest, the same
/// precedence the resolver applies when selecting the narrowest present scope.
/// </summary>
public static class DependencyScopeKinds
{
    public const string Tenant = "tenant";
    public const string Mailbox = "mailbox";
    public const string Project = "project";
    public const string Operation = "operation";
    public const string ServiceClient = "service-client";
    public const string WorkflowItem = "workflow-item";
    public const string CommandSurface = "command-surface";
    public const string Unknown = "unknown";

    /// <summary>The scope kinds ordered narrowest (workflow-item) to broadest (tenant); excludes <see cref="DependencyScopeKind.Unknown"/>.</summary>
    public static IReadOnlyList<DependencyScopeKind> All { get; } =
    [
        DependencyScopeKind.WorkflowItem,
        DependencyScopeKind.Operation,
        DependencyScopeKind.CommandSurface,
        DependencyScopeKind.ServiceClient,
        DependencyScopeKind.Project,
        DependencyScopeKind.Mailbox,
        DependencyScopeKind.Tenant,
    ];

    public static string ToWireValue(DependencyScopeKind kind)
        => kind switch
        {
            DependencyScopeKind.Tenant => Tenant,
            DependencyScopeKind.Mailbox => Mailbox,
            DependencyScopeKind.Project => Project,
            DependencyScopeKind.Operation => Operation,
            DependencyScopeKind.ServiceClient => ServiceClient,
            DependencyScopeKind.WorkflowItem => WorkflowItem,
            DependencyScopeKind.CommandSurface => CommandSurface,
            DependencyScopeKind.Unknown => Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported dependency scope kind."),
        };
}
