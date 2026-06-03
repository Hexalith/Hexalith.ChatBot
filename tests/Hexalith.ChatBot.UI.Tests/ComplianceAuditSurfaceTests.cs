using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;

using Shouldly;

using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;
using IChatBotCommand = Hexalith.ChatBot.Contracts.Commands.IChatBotCommand;
using RequestComplianceEscalation = Hexalith.ChatBot.Contracts.Commands.RequestComplianceEscalation;
using RequestComplianceInvestigation = Hexalith.ChatBot.Contracts.Commands.RequestComplianceInvestigation;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Story 9.3 (S9): the compliance audit investigation surface service reads metadata-only timeline rows through the
/// client, dispatches the read/escalate-only escalation/investigation commands with an opaque target, and never leaks
/// raw content; the page composition matches the binding DOM contract and stays read/escalate-only.
/// </summary>
public sealed class ComplianceAuditSurfaceTests
{
    [Fact]
    public async Task ServiceShouldReadTimelineRowsAsMetadataOnlySafeTokens()
    {
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        ComplianceAuditTimelineModel timeline = await service.SearchAsync(DefaultQuery(), TestContext.Current.CancellationToken);

        ComplianceAuditRowModel row = timeline.Rows.ShouldHaveSingleItem();
        row.AuditRecordRef.ShouldBe("audit-record-001");
        row.Command.ShouldBe("SubmitRetentionConfigurationChange");
        row.Redaction.ShouldBe("restricted");
        row.SafeNextAction.ShouldBe("request-access");
        row.TimestampZ.ShouldBe("2026-06-02 04:00:00Z");

        string json = JsonSerializer.Serialize(timeline, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("mailbox subject", Case.Insensitive);
        json.ShouldNotContain("provider payload", Case.Insensitive);
    }

    [Fact]
    public async Task ServiceShouldDispatchEscalationWithOpaqueTargetFromUiOrigin()
    {
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        ComplianceCommandResult result = await service.RequestEscalationAsync(
            "audit-record-001",
            "investigation-s9",
            "project-opaque-ref",
            TestContext.Current.CancellationToken);

        client.SubmittedOrigin.ShouldBe(ChatBotSurfaceOrigin.Ui);
        RequestComplianceEscalation command = client.SubmittedCommand.ShouldBeOfType<RequestComplianceEscalation>();
        command.EscalationTargetRef.ShouldBe("project-opaque-ref");
        command.AuditRecordRef.ShouldBe("audit-record-001");
        result.CommandId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAC");
    }

    [Fact]
    public async Task ServiceShouldDispatchInvestigationIntent()
    {
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        await service.TriggerInvestigationAsync("investigation-s9", "audit-query-s9", ["audit-filter-time"], TestContext.Current.CancellationToken);

        client.SubmittedCommand.ShouldBeOfType<RequestComplianceInvestigation>().QueryRef.ShouldBe("audit-query-s9");
    }

    [Fact]
    public async Task ServiceShouldTranslateEveryFr56DimensionOntoTheOutboundQueryWithATimeBaseline()
    {
        // FR56: each populated filter dimension on the surface form must reach the wire query as its canonical key, the
        // always-on `time` baseline keeps the query schema-valid, and a non-positive limit falls back to 100.
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        ComplianceAuditQueryModel query = new(
            Tenant: "tenant-alpha",
            Actor: "actor-alpha",
            Command: "SubmitRetentionConfigurationChange",
            Resource: "audit-record-001",
            Decision: "allow",
            Reason: "pre_commit_gate",
            Correlation: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            MessageId: "intake-007",
            Surface: "ui",
            FromUtc: new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ToUtc: new DateTimeOffset(2026, 6, 3, 0, 0, 0, TimeSpan.Zero),
            Limit: 0);

        await service.SearchAsync(query, TestContext.Current.CancellationToken);

        ComplianceAuditQuery submitted = client.SubmittedQuery.ShouldNotBeNull();
        Dictionary<string, string> byKey = submitted.Filters.ToDictionary(static f => f.FilterKey, static f => f.ValueRef, StringComparer.Ordinal);
        byKey["time"].ShouldBe("all");
        byKey["tenant"].ShouldBe("tenant-alpha");
        byKey["actor"].ShouldBe("actor-alpha");
        byKey["command"].ShouldBe("SubmitRetentionConfigurationChange");
        byKey["resource"].ShouldBe("audit-record-001");
        byKey["decision"].ShouldBe("allow");
        byKey["reason"].ShouldBe("pre_commit_gate");
        byKey["correlation"].ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        byKey["message-id"].ShouldBe("intake-007");
        byKey["surface"].ShouldBe("ui");
        submitted.Limit.ShouldBe(100);
        submitted.Filters.Select(static f => f.FilterKey).ShouldBeUnique();
    }

    [Fact]
    public async Task ServiceShouldOmitBlankDimensionsAndStillKeepTheTimeBaseline()
    {
        // An empty form must not emit blank filters (which would fail the safe-token gate) yet must remain valid.
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        await service.SearchAsync(DefaultQuery(), TestContext.Current.CancellationToken);

        ComplianceAuditQuery submitted = client.SubmittedQuery.ShouldNotBeNull();
        submitted.Filters.ShouldHaveSingleItem().FilterKey.ShouldBe("time");
        submitted.Limit.ShouldBe(100);
    }

    [Fact]
    public async Task ServiceShouldMapRedactedDetailWithoutLeakingHiddenResource()
    {
        FakeComplianceClient client = new();
        ComplianceAuditService service = new(client);

        ComplianceAuditDetailModel detail = await service.GetDetailAsync("audit-record-001", TestContext.Current.CancellationToken);

        detail.Redaction.ShouldBe("escalation-required");
        detail.SafeNextAction.ShouldBe("request-access");
        detail.VisibleMetadataRefs.ShouldBeEmpty();
    }

    [Fact]
    public void SurfacePageShouldMatchBindingDomContractAndStayReadEscalateOnly()
    {
        string page = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Pages/ComplianceAuditInvestigation.razor");

        page.ShouldContain("@page \"/compliance-audit-investigation\"");
        page.ShouldContain("id=\"compliance-audit-title\"");
        page.ShouldContain("data-chatbot-surface=\"audit-investigation-s9\"");
        page.ShouldContain("aria-labelledby=\"compliance-timeline-title\"");

        // Timeline list + article rows with redaction/escalation data attributes and safe-token definition list.
        page.ShouldContain("ComplianceAuditTimelineLabel");
        page.ShouldContain("data-redaction-state=\"@row.Redaction\"");
        page.ShouldContain("data-escalation-state=\"@row.Escalation\"");
        page.ShouldContain("actor:@row.Actor");
        page.ShouldContain("safe-next-action:@row.SafeNextAction");

        // Escalation + investigation affordances; opaque escalation target; no workflow mutation on the operate control.
        page.ShouldContain("aria-describedby=\"compliance-escalation-reason\"");
        page.ShouldContain("ComplianceAuditEscalationButton");
        page.ShouldContain("ComplianceAuditInvestigationButton");
        page.ShouldContain("RequestEscalationAsync");
        page.ShouldContain("TriggerInvestigationAsync");
        page.ShouldContain("project-opaque-ref");

        // Read/escalate-only proof: the operate-style control is inert with an explainable reason.
        page.ShouldContain("aria-disabled=\"true\"");
        page.ShouldContain("aria-describedby=\"compliance-operate-denied\"");
        page.ShouldContain("ComplianceAuditRetryButton");

        // Phone fallback keeps the read-only summary and escalation reachable.
        page.ShouldContain("compliance-phone-fallback");
        page.ShouldContain("ComplianceAuditPhoneSummary");
        page.ShouldContain("ComplianceAuditPhoneGuidance");

        // Every visible string flows through the localizer (no free-form English literals for the surface labels).
        page.ShouldContain("UiText[ChatBotUiTextKey.ComplianceAuditPageTitle]");
        page.ShouldNotContain(">Request compliance access<");
    }

    private static ComplianceAuditQueryModel DefaultQuery()
        => new(null, null, null, null, null, null, null, null, null,
            new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero),
            100);

    private static string ReadProjectFile(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull();
        return File.ReadAllText(Path.Combine(directory.FullName, relativePath));
    }

    private sealed class FakeComplianceClient : IChatBotClient
    {
        public IChatBotCommand? SubmittedCommand { get; private set; }

        public ChatBotSurfaceOrigin SubmittedOrigin { get; private set; }

        public ComplianceAuditQuery? SubmittedQuery { get; private set; }

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            SubmittedCommand = command;
            SubmittedOrigin = origin;
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAC",
                CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                LifecycleState = LifecycleState.Proposed,
                AcceptedAt = new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            });
        }

        public Task<ComplianceAuditSearchView> SearchComplianceAuditRecordsAsync(
            ComplianceAuditQuery query,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQuery = query;
            return Task.FromResult(new ComplianceAuditSearchView(
                "audit-query-s9",
                [
                    new ComplianceAuditRowView(
                        "audit-record-001",
                        "admin-alpha",
                        "human",
                        "SubmitRetentionConfigurationChange",
                        "audit-record-001",
                        "allow",
                        "pre_commit_gate",
                        "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                        new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
                        "policy-snapshot-admin-v1",
                        "restricted",
                        "not-requested",
                        "request-access"),
                ],
                "sha256:1",
                new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero),
                "01ARZ3NDEKTSV4RRFFQ69G5FAW"));
        }

        public Task<ComplianceAuditDetailView> GetComplianceAuditDetailAsync(
            string auditRecordRef,
            string? correlationId = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(ComplianceAuditDetailView.Restricted);

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
