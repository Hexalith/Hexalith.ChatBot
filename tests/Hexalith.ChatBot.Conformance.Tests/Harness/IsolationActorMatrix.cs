using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// One actor persona exercised by the cross-tenant isolation harness. The persona is a TEST-HARNESS concept,
/// not a production identity: its <see cref="Origin"/> is the only thing the gateway actually observes, and its
/// <see cref="RoleMetadataClaims"/> are metadata-only labels that prove even a self-declared "admin" or
/// "service" actor cannot cross tenant scope. M0 has no RBAC for these categories; the point is to prove no
/// declared actor class crosses the tenant boundary, not to model permissions. Production enums (ActorType) are
/// intentionally NOT widened for these labels.
/// </summary>
/// <param name="Label">The stable persona label (used in diagnostics and the non-vacuity matrix).</param>
/// <param name="Origin">The declared surface origin this persona presents at the boundary.</param>
/// <param name="AdapterPosture">A short description of the surface/adapter posture this persona stands in for.</param>
/// <param name="RoleMetadataClaims">Metadata-only extra claims (never a second tenant claim, never RBAC).</param>
internal sealed record IsolationActorPersona(
    string Label,
    ChatBotSurfaceOrigin Origin,
    string AdapterPosture,
    IReadOnlyList<KeyValuePair<string, string>> RoleMetadataClaims)
{
    /// <summary>The wire token the persona declares for its surface origin.</summary>
    public string DeclaredOrigin => ChatBotSurfaceOrigins.ToWireValue(Origin);
}

/// <summary>
/// The nine required actor personas for the cross-tenant isolation harness (FR/epic acceptance): human user,
/// tenant admin, project admin/owner, service client, CLI client, MCP client, background worker, M365 event,
/// and AI actor. Each maps to a stable surface origin or adapter posture. The matrix is the authority the
/// non-vacuity test asserts against: exactly nine personas, each with at least one negative case.
/// </summary>
internal static class IsolationActorMatrix
{
    /// <summary>Persona label: an authenticated human on the UI surface.</summary>
    public const string HumanUser = "human-user";

    /// <summary>Persona label: a tenant admin (role metadata only) on the UI surface.</summary>
    public const string TenantAdmin = "tenant-admin";

    /// <summary>Persona label: a project admin/owner (role metadata only) on the UI surface.</summary>
    public const string ProjectAdminOwner = "project-admin-owner";

    /// <summary>Persona label: a service principal on the API surface.</summary>
    public const string ServiceClient = "service-client";

    /// <summary>Persona label: a CLI adapter shim.</summary>
    public const string CliClient = "cli-client";

    /// <summary>Persona label: an MCP adapter shim.</summary>
    public const string McpClient = "mcp-client";

    /// <summary>Persona label: a background worker shim.</summary>
    public const string BackgroundWorker = "background-worker";

    /// <summary>Persona label: an M365 mailbox/event shim.</summary>
    public const string M365Event = "m365-event";

    /// <summary>Persona label: an AI actor shim.</summary>
    public const string AiActor = "ai-actor";

    /// <summary>The nine required actor personas, in stable order.</summary>
    public static IReadOnlyList<IsolationActorPersona> Personas { get; } =
    [
        new(HumanUser, ChatBotSurfaceOrigin.Ui, "ui-human", []),
        new(TenantAdmin, ChatBotSurfaceOrigin.Ui, "ui-tenant-admin", [new("role", "tenant-admin")]),
        new(ProjectAdminOwner, ChatBotSurfaceOrigin.Ui, "ui-project-owner", [new("role", "project-owner")]),
        new(ServiceClient, ChatBotSurfaceOrigin.Api, "api-service-client", [new("actor_posture", "service")]),
        new(CliClient, ChatBotSurfaceOrigin.Cli, "cli-adapter", []),
        new(McpClient, ChatBotSurfaceOrigin.Mcp, "mcp-adapter", []),
        new(BackgroundWorker, ChatBotSurfaceOrigin.Worker, "worker-adapter", [new("actor_posture", "worker")]),
        new(M365Event, ChatBotSurfaceOrigin.Mailbox, "mailbox-event", [new("actor_posture", "m365-event")]),
        new(AiActor, ChatBotSurfaceOrigin.Ai, "ai-adapter", [new("actor_posture", "ai")]),
    ];

    /// <summary>The nine required persona labels, in stable order (for the non-vacuity assertion).</summary>
    public static IReadOnlyList<string> RequiredPersonaLabels { get; } =
    [
        HumanUser,
        TenantAdmin,
        ProjectAdminOwner,
        ServiceClient,
        CliClient,
        McpClient,
        BackgroundWorker,
        M365Event,
        AiActor,
    ];
}
