using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.UI.Services;

using Shouldly;

using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;
using OperationStatus = Hexalith.ChatBot.Client.Generated.OperationStatus;
using AssociationRoutingStatus = Hexalith.ChatBot.Client.Generated.AssociationRoutingStatus;
using ProjectConversationResponse = Hexalith.ChatBot.Client.Generated.ProjectConversationResponse;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Covers the read-only dashboard UI seam (Story 8.1, AC1/AC2/AC3/AC10): it reaches the spine only through the
/// <c>IChatBotClient</c> façade, declares the <c>ui</c> surface origin, and produces a contract-valid,
/// metadata-only overview covering all six FR67 views plus audit projection lag with bounded-staleness freshness.
/// </summary>
public sealed class OperationalDashboardServiceTests
{
    [Fact]
    public void ServiceShouldDeclareUiSurfaceOriginAtTheBoundary()
    {
        OperationalDashboardService service = new(new StubChatBotClient());
        service.SurfaceOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
    }

    [Fact]
    public async Task GetOverviewShouldRenderAllViewsContractValidMetadataOnlyWithFreshStaleAndExpired()
    {
        OperationalDashboardService service = new(new StubChatBotClient());

        OperationalDashboardOverview overview = await service.GetOverviewAsync(TestContext.Current.CancellationToken);

        overview.Views.Select(static view => view.View).ShouldBe(DashboardObservabilityViews.All, ignoreOrder: false);
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
        overview.Views.ShouldAllBe(view => Enum.IsDefined(view.Health));

        IReadOnlyList<ChatBotFreshnessState> freshness = overview.Views.Select(static view => view.FreshnessState).ToArray();
        freshness.ShouldContain(ChatBotFreshnessState.Fresh);
        freshness.ShouldContain(ChatBotFreshnessState.Stale);
        freshness.ShouldContain(ChatBotFreshnessState.Expired);

        string json = JsonSerializer.Serialize(overview, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("bearer", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("password", Case.Insensitive);
    }

    [Fact]
    public async Task GetOverviewShouldCarryThePublishedSloCatalogWithFailSafeUnknownBurn()
    {
        // Story 8.3 AC3/AC5: the placeholder overview carries the canonical NFR42a catalog so the UI renders the
        // published SLOs; with no server read wired every SLO's burn is the fail-safe Unknown (honest no-data).
        OperationalDashboardService service = new(new StubChatBotClient());

        OperationalDashboardOverview overview = await service.GetOverviewAsync(TestContext.Current.CancellationToken);

        overview.PublishedSlos.ShouldNotBeNull();
        overview.PublishedSlos!.Count.ShouldBe(OperatingBaselineCatalog.Published.Count);
        OperationalDashboardContractValidator.IsValid(overview).ShouldBeTrue();
        overview.PublishedSlos!.ShouldAllBe(slo => slo.BurnState == ErrorBudgetBurnState.Unknown);
        foreach (string required in OperatingBaselineMetrics.Required)
        {
            overview.PublishedSlos!.Select(slo => slo.MetricName).ShouldContain(required);
        }
    }

    // The dashboard read adds no public endpoint (generic transport reused); this stub satisfies the façade
    // dependency without any spine call, since the metadata-only overview is assembled at the UI boundary.
    private sealed class StubChatBotClient : IChatBotClient
    {
        public Task<CommandSubmissionResponse> SubmitAsync(IChatBotCommand command, string? correlationId = null, string? taskId = null, ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationStatus> GetOperationStatusAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(string operationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(string associationId, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(string projectId, string? cursor = null, int pageSize = 25, string? correlationId = null, string? taskId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
