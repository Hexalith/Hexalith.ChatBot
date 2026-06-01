using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using ContractAssociationCandidate = Hexalith.ChatBot.Contracts.Commands.AssociationCandidate;
using ContractAssociationConfidenceInput = Hexalith.ChatBot.Contracts.Commands.AssociationConfidenceInput;
using ContractAssociationDecisionKind = Hexalith.ChatBot.Contracts.Enums.AssociationDecisionKind;
using ContractAssociationEvidenceReference = Hexalith.ChatBot.Contracts.Commands.AssociationEvidenceReference;
using ContractAssociationReasonCode = Hexalith.ChatBot.Contracts.Enums.AssociationReasonCode;
using ContractAssociationScoringOutcome = Hexalith.ChatBot.Contracts.Enums.AssociationScoringOutcome;
using ContractAssociationSignalClass = Hexalith.ChatBot.Contracts.Enums.AssociationSignalClass;
using ContractAssociationThresholdBand = Hexalith.ChatBot.Contracts.Enums.AssociationThresholdBand;
using ContractLifecycleState = Hexalith.ChatBot.Contracts.Enums.LifecycleState;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// Boots the REAL server (<c>WebApplicationFactory&lt;Program&gt;</c>) for the HTTP read-surface isolation tests
/// (AC3/AC5). The durable read stores are seeded directly via IVT with a foreign (<c>tenant-beta</c>) record and
/// an own (<c>tenant-alpha</c>) record for each surface, so a bound caller can be proven to get a safe denial on
/// the foreign record while the foreign record demonstrably exists (read as its owning tenant via the
/// <c>X-Test-Tenant</c> override). The seeded singletons override the app's defaults (last registration wins),
/// matching the Story 1.9/1.11 Server.Tests pattern; no production seam is made public.
/// </summary>
internal static class IsolationHttpHost
{
    /// <summary>Header token that asks the test principal shim to omit tenant claims.</summary>
    public const string MissingTenantContext = "__missing_tenant__";

    /// <summary>Header token that asks the test principal shim to emit two distinct tenant claims.</summary>
    public const string AmbiguousTenantContext = "__ambiguous_tenant__";

    /// <summary>Header token that asks the test principal shim to emit a safe but obsolete tenant claim.</summary>
    public const string StaleTenantContext = "__stale_tenant__";

    /// <summary>Header token that asks the test principal shim to emit an unsafe tenant claim.</summary>
    public const string UnsafeTenantContext = "__unsafe_tenant__";

    private static readonly DateTimeOffset SeedTime = new(2026, 5, 31, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Creates a factory whose read stores are seeded with the corpus's foreign and own records.</summary>
    /// <returns>A configured <see cref="WebApplicationFactory{TEntryPoint}"/>.</returns>
    public static WebApplicationFactory<Program> CreateSeeded()
    {
        InMemoryGovernedOperationProjectionStore projectionStore = new();
        InMemoryOperationStatusStore statusStore = new();
        InMemoryProjectConversationProjectionStore conversationStore = new();
        InMemoryAssociationProjectionStore associationStore = new();
        SeedProjection(projectionStore);
        SeedStatus(statusStore);
        SeedProjectConversation(conversationStore);
        SeedAssociationRouting(associationStore);

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new IsolationPrincipalStartupFilter(CrossTenantLeakageCorpus.BoundTenant));
                services.AddSingleton<IGovernedOperationProjectionStore>(projectionStore);
                services.AddSingleton<IOperationStatusStore>(statusStore);
                services.AddSingleton<IProjectConversationProjectionStore>(conversationStore);
                services.AddSingleton<IAssociationProjectionStore>(associationStore);
                services.AddSingleton<IAuditHistoryReader>(new EmptyAuditHistoryReader());
            }));
    }

    /// <summary>Builds a governed-operation projection read request for a note, with an optional tenant override.</summary>
    /// <param name="noteId">The note id to read.</param>
    /// <param name="tenantId">The tenant to present (via <c>X-Test-Tenant</c>), or null for the default.</param>
    /// <returns>The request message.</returns>
    public static HttpRequestMessage GovernedOperationRequest(string noteId, string? tenantId = null)
        => Get($"/api/v1/governed-operations/{noteId}", tenantId);

    /// <summary>Builds an operation-status read request, with an optional tenant override.</summary>
    /// <param name="operationId">The operation id to read.</param>
    /// <param name="tenantId">The tenant to present (via <c>X-Test-Tenant</c>), or null for the default.</param>
    /// <returns>The request message.</returns>
    public static HttpRequestMessage OperationStatusRequest(string operationId, string? tenantId = null)
        => Get($"/api/v1/operations/{operationId}", tenantId);

    /// <summary>Builds an operation audit-history read request, with an optional tenant override.</summary>
    /// <param name="operationId">The operation id to read.</param>
    /// <param name="tenantId">The tenant to present (via <c>X-Test-Tenant</c>), or null for the default.</param>
    /// <returns>The request message.</returns>
    public static HttpRequestMessage AuditHistoryRequest(string operationId, string? tenantId = null)
        => Get($"/api/v1/operations/{operationId}/audit-history", tenantId);

    public static HttpRequestMessage ProjectConversationRequest(string projectId, string? tenantId = null)
        => Get($"/api/v1/projects/{projectId}/conversation", tenantId);

    public static HttpRequestMessage AssociationRoutingStatusRequest(string associationId, string? tenantId = null)
        => Get($"/api/v1/associations/{associationId}/routing-status", tenantId);

    private static HttpRequestMessage Get(string route, string? tenantId)
    {
        HttpRequestMessage request = new(HttpMethod.Get, route);
        request.Headers.Add("X-Correlation-Id", CrossTenantIsolationHarness.CorrelationId);
        request.Headers.Add("X-Hexalith-Task-Id", CrossTenantIsolationHarness.TaskId);
        if (tenantId is not null)
        {
            request.Headers.Add("X-Test-Tenant", tenantId);
        }

        return request;
    }

    private static void SeedProjection(InMemoryGovernedOperationProjectionStore store)
    {
        store.SaveAsync(View(CrossTenantLeakageCorpus.ForeignTenant, CrossTenantLeakageCorpus.ForeignNoteId)).GetAwaiter().GetResult();
        store.SaveAsync(View(CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.OwnNoteId)).GetAwaiter().GetResult();
    }

    private static void SeedStatus(InMemoryOperationStatusStore store)
    {
        store.UpsertAsync(Status(CrossTenantLeakageCorpus.ForeignTenant, CrossTenantLeakageCorpus.ForeignOperationId), CancellationToken.None).AsTask().GetAwaiter().GetResult();
        store.UpsertAsync(Status(CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.OwnOperationId), CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    private static void SeedProjectConversation(InMemoryProjectConversationProjectionStore store)
    {
        store.UpsertAsync(ConversationItem(CrossTenantLeakageCorpus.ForeignTenant, "foreign-project", CrossTenantLeakageCorpus.ForeignOperationId), CancellationToken.None).GetAwaiter().GetResult();
        store.UpsertAsync(ConversationItem(CrossTenantLeakageCorpus.BoundTenant, "own-project", CrossTenantLeakageCorpus.OwnOperationId), CancellationToken.None).GetAwaiter().GetResult();
    }

    private static void SeedAssociationRouting(InMemoryAssociationProjectionStore store)
    {
        store.SaveAsync(Association(CrossTenantLeakageCorpus.ForeignTenant, CrossTenantLeakageCorpus.ForeignOperationId, "foreign-project")).GetAwaiter().GetResult();
        store.SaveAsync(Association(CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.OwnOperationId, "own-project")).GetAwaiter().GetResult();
    }

    private static GovernedOperationView View(string tenantId, string noteId)
        => new(
            tenantId,
            noteId,
            GovernedOperationView.CurrentSchemaVersion,
            GovernedOperationView.GovernedCommandProvenance,
            GovernedOperationView.CurrentDerivationKernelVersion,
            GovernedOperationView.MetadataOnlyRedactionState,
            GovernedOperationView.GovernedOperationalRetentionClass,
            SourceVersion: 1,
            SeedTime,
            SeedTime);

    private static OperationStatusRecord Status(string tenantId, string operationId)
        => new(
            tenantId,
            operationId,
            operationId,
            CrossTenantIsolationHarness.CorrelationId,
            LifecycleState.Proposed,
            0,
            OperationStatusRecord.AcceptedProjectionPending,
            OperationStatusRecord.AuditCommitted,
            [ChatBotMessageNextActions.None],
            null,
            SeedTime,
            SeedTime);

    private static ProjectConversationItemView ConversationItem(string tenantId, string projectId, string itemId)
        => new(
            tenantId,
            projectId,
            projectId == "own-project" ? "Own Project" : "Foreign Project",
            itemId,
            $"{itemId}-intake",
            Hexalith.ChatBot.Contracts.Enums.ProjectConversationItemKind.EmailDerived,
            Hexalith.ChatBot.Contracts.Enums.ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            SeedTime,
            Hexalith.ChatBot.Contracts.Enums.LifecycleState.Associated,
            Hexalith.ChatBot.Contracts.Enums.AssociationThresholdBand.Auto,
            0.9,
            itemId,
            "controlled-mailbox-001",
            $"provider-{itemId}",
            $"internet-{itemId}",
            "conversation-001",
            null,
            SeedTime,
            null,
            null,
            "UTC",
            "Microsoft 365 mailbox",
            AssociationCandidateView.MailboxSourceProvenance,
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            1,
            CrossTenantIsolationHarness.CorrelationId);

    private static AssociationCandidateView Association(string tenantId, string associationId, string projectId)
    {
        ContractAssociationEvidenceReference evidence = new(
            "mailbox:intake:project-token",
            "evidence-sha256-project-token",
            "project-identifier",
            "explicit-project-identifier",
            "mailbox:metadata",
            "available",
            "metadata_only",
            "fresh",
            0.71);
        ContractAssociationCandidate candidate = new(
            projectId,
            null,
            0.91,
            1,
            [ContractAssociationReasonCode.ExplicitProjectIdentifierMatched],
            [evidence],
            [
                new ContractAssociationConfidenceInput(
                    ContractAssociationSignalClass.ExplicitProjectIdentifier,
                    ContractAssociationReasonCode.ExplicitProjectIdentifierMatched,
                    0.71,
                    evidence.EvidenceReference,
                    evidence.EvidenceFingerprint),
            ],
            false);

        return new AssociationCandidateView(
            tenantId,
            associationId,
            $"{associationId}-intake",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            projectId,
            null,
            ContractLifecycleState.Associated,
            ContractAssociationScoringOutcome.AutoAssociated,
            ContractAssociationThresholdBand.Auto,
            0.91,
            [candidate],
            [],
            "association-thresholds.m0.default.v1",
            AssociationCandidateView.CurrentSchemaVersion,
            AssociationCandidateView.MailboxSourceProvenance,
            "association-deterministic.kernel.m0.v1",
            "metadata_only",
            "collaboration_input",
            1,
            CrossTenantIsolationHarness.CorrelationId,
            SeedTime,
            SeedTime,
            ContractAssociationDecisionKind.Associate,
            "actor-safe",
            "human",
            SeedTime.AddMinutes(1),
            DecisionNoteRedactionState: "redacted",
            SurfaceOrigin: "ui",
            PolicySnapshotVersion: "association-thresholds.m0.default.v1",
            SafeNextAction: ChatBotMessageNextActions.None);
    }

    private sealed class EmptyAuditHistoryReader : IAuditHistoryReader
    {
        public IReadOnlyList<AuditEnvelope> GetPostCommitEnvelopes(string tenantId, string commandId) => [];
    }

    private sealed class IsolationPrincipalStartupFilter(string tenantId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (context, continuation) =>
                {
                    string effectiveTenantId =
                        context.Request.Headers.TryGetValue("X-Test-Tenant", out Microsoft.Extensions.Primitives.StringValues values) && values.Count == 1
                            ? values[0]!
                            : tenantId;
                    Claim[] claims = effectiveTenantId switch
                    {
                        MissingTenantContext =>
                            [new("sub", CrossTenantIsolationHarness.BoundActorId)],
                        AmbiguousTenantContext =>
                        [
                            new("sub", CrossTenantIsolationHarness.BoundActorId),
                            new("eventstore:tenant", CrossTenantLeakageCorpus.BoundTenant),
                            new("eventstore:tenant", CrossTenantLeakageCorpus.ForeignTenant),
                        ],
                        StaleTenantContext =>
                        [
                            new("sub", CrossTenantIsolationHarness.BoundActorId),
                            new("eventstore:tenant", $"{CrossTenantLeakageCorpus.BoundTenant}-stale"),
                        ],
                        UnsafeTenantContext =>
                        [
                            new("sub", CrossTenantIsolationHarness.BoundActorId),
                            new("eventstore:tenant", "unsafe tenant value"),
                        ],
                        _ =>
                        [
                            new("sub", CrossTenantIsolationHarness.BoundActorId),
                            new("eventstore:tenant", effectiveTenantId),
                        ],
                    };
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }
}
