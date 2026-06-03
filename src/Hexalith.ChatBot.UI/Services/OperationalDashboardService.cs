using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.UI.Services;

/// <summary>
/// The UI's single seam onto the read-only operational dashboard. It declares the
/// <see cref="ChatBotSurfaceOrigin.Ui"/> surface origin at the <see cref="IChatBotClient"/> façade — the only
/// spine the UI is allowed to reach. Story 8.1 adds <b>no</b> public dashboard read endpoint (AC9 — the generic
/// command/query transport spine is reused, mirroring Story 7.5's operational-queue surface), and no dashboard
/// read method exists on <see cref="IChatBotClient"/> yet, so the M0/M1 overview is assembled at the UI boundary
/// as a fail-safe placeholder: every view reports <see cref="ChatBotHealthStatus.Unknown"/> (never a fabricated
/// health) until the server-side <c>OperationalDashboardProjector</c> is wired behind a read endpoint. The
/// injected client is the seam that read will flow through once it exists. The overview is metadata-only by
/// construction — it never carries project/evidence/file/mailbox/audit detail; restricted per-item detail stays
/// behind the existing authorized hydration path, surfaced as a safe request-access/disabled state.
/// </summary>
public sealed class OperationalDashboardService(IChatBotClient client)
{
    private const string SchemaVersion = "chatbot.operational-dashboard.v1";
    private const string DefaultOwnerRole = "operations-admin";

    private readonly IChatBotClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <summary>The surface origin this service declares at the spine boundary.</summary>
    public ChatBotSurfaceOrigin SurfaceOrigin => ChatBotSurfaceOrigin.Ui;

    /// <summary>
    /// Reads the metadata-only operational health overview. Bounded-staleness freshness is computed per view from
    /// the snapshot instant against the NFR6 window, so a re-query (timed poll or manual refresh) within the window
    /// refreshes the freshness state honestly.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The metadata-only operational dashboard overview.</returns>
    public Task<OperationalDashboardOverview> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string correlationId = ChatBotIdentity.NewUlid();

        // M0/M1 fail-safe placeholder: with no dashboard read endpoint wired onto the IChatBotClient façade, every
        // view reports Unknown rather than fabricating Healthy/Degraded/Failed health an operator could act on. The
        // snapshot timestamps still vary so the bounded-staleness fresh/stale/expired classification is exercised
        // honestly; depth/age stay 0 because no real source has been read.
        OperationalDashboardView[] views =
        [
            QueueView(DashboardObservabilityView.MailboxProcessing, ChatBotHealthStatus.Unknown, depth: 0, ageSeconds: 0, now.AddMinutes(-1), now),
            QueueView(DashboardObservabilityView.FailedAssociations, ChatBotHealthStatus.Unknown, depth: 0, ageSeconds: 0, now.AddMinutes(-2), now),
            QueueView(DashboardObservabilityView.ApprovalQueues, ChatBotHealthStatus.Unknown, depth: 0, ageSeconds: 0, now.AddMinutes(-8), now),
            QueueView(DashboardObservabilityView.DuplicateHandling, ChatBotHealthStatus.Unknown, depth: 0, ageSeconds: 0, now.AddSeconds(-30), now),
            AggregateView(DashboardObservabilityView.AiActionOutcomes, ChatBotHealthStatus.Unknown, depth: 0, ageSeconds: 0, now.AddMinutes(-9), now, lagIndicator: null),
            AggregateView(DashboardObservabilityView.AuditProjectionLag, ChatBotHealthStatus.Unknown, depth: null, ageSeconds: 0, now.AddMinutes(-20), now, lagIndicator: "unknown"),
        ];

        DateTimeOffset overallFreshness = views.Min(static view => view.FreshnessTimestampUtc);

        OperationalDashboardOverview overview = new(
            views,
            overallFreshness,
            OperationalDashboardFreshnessPolicy.Classify(overallFreshness, now),
            SchemaVersion,
            correlationId);

        return Task.FromResult(overview);
    }

    private static OperationalDashboardView QueueView(
        DashboardObservabilityView view,
        ChatBotHealthStatus health,
        int depth,
        int ageSeconds,
        DateTimeOffset snapshotUtc,
        DateTimeOffset now)
        => new(
            view,
            health,
            Depth: depth,
            OldestItemAgeSeconds: ageSeconds,
            OwnerRole: DefaultOwnerRole,
            FreshnessTimestampUtc: snapshotUtc,
            FreshnessState: OperationalDashboardFreshnessPolicy.Classify(snapshotUtc, now),
            // Per-item detail requires per-project authority; the dashboard offers a safe request-access state.
            DetailLinkState: OperationalDashboardContractValidator.DetailRequestAccess,
            DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.InsufficientAuthority]);

    private static OperationalDashboardView AggregateView(
        DashboardObservabilityView view,
        ChatBotHealthStatus health,
        int? depth,
        int ageSeconds,
        DateTimeOffset snapshotUtc,
        DateTimeOffset now,
        string? lagIndicator)
        => new(
            view,
            health,
            Depth: depth,
            OldestItemAgeSeconds: ageSeconds,
            OwnerRole: DefaultOwnerRole,
            FreshnessTimestampUtc: snapshotUtc,
            FreshnessState: OperationalDashboardFreshnessPolicy.Classify(snapshotUtc, now),
            DetailLinkState: OperationalDashboardContractValidator.DetailDisabled,
            DisabledDetailReasonCodes: [ChatBotDisabledActionReasons.StateNotPermitted],
            LagIndicator: lagIndicator);
}
