using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Observability;
using Hexalith.ChatBot.Server.Tests.Observability;

using Shouldly;

using GeneratedCommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using GeneratedRequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AiActionApprovalGateMetricsTests
{
    private const string Tenant = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    [Fact]
    public async Task EvaluateAsyncShouldRecordApprovalLatencyOnceForTheBoundTenant()
    {
        RecordingChatBotMetrics metrics = new();
        AiActionApprovalGate gate = new(new UnusedPolicyEvaluator(), metrics);

        ChatBotApprovalResult result = await gate.EvaluateAsync(Context(), TestContext.Current.CancellationToken);

        result.ShouldBe(ChatBotApprovalResult.Approved);
        (string operationClass, string tenantId, double milliseconds) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.Approval);
        tenantId.ShouldBe(Tenant);
        milliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task EvaluateAsyncShouldRecordApprovalLatencyEvenWhenTheDecisionThrows()
    {
        // AC2/AC5: the latency is recorded on every completion path via `finally`, so a throwing core evaluation
        // (here a malformed approval-decision payload) still records approval latency for the bound tenant while the
        // exception propagates unchanged — emission never alters the operation's control flow or result.
        RecordingChatBotMetrics metrics = new();
        AiActionApprovalGate gate = new(new UnusedPolicyEvaluator(), metrics);

        // A JSON array cannot bind to the DecideAiActionApproval record → ReadDecisionCommand throws inside the core.
        ChatBotGatewayContext context = Context(nameof(DecideAiActionApproval), "[]");

        await Should.ThrowAsync<JsonException>(
            () => gate.EvaluateAsync(context, TestContext.Current.CancellationToken).AsTask());

        (string operationClass, string tenantId, double milliseconds) = metrics.Latencies.ShouldHaveSingleItem();
        operationClass.ShouldBe(ChatBotOperationClasses.Approval);
        tenantId.ShouldBe(Tenant);
        milliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    private static ChatBotGatewayContext Context()
        => Context(nameof(RecordGovernedNote), "{}");

    private static ChatBotGatewayContext Context(string commandType, string commandJson)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test"));
        ChatBotCommandSubmission submission = new(
            principal,
            new GeneratedCommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType,
                Command = JsonDocument.Parse(commandJson).RootElement.Clone(),
                RequestSchemaVersion = GeneratedRequestSchemaVersion.V1,
            },
            CorrelationId,
            null,
            ChatBotSurfaceOrigin.Ui);

        return new ChatBotGatewayContext(
            submission,
            new ChatBotAuthenticatedActor("actor-alpha", principal),
            new ChatBotTenantBinding(Tenant));
    }

    // A non-AI command resolves to Approved without consulting the policy evaluator, so this stub must never run.
    private sealed class UnusedPolicyEvaluator : IAiActionPolicyEvaluator
    {
        public ValueTask<AiActionPolicyDecision> EvaluateAsync(AiActionPolicyEvaluationRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("The policy evaluator must not be invoked for a non-AI command.");
    }
}
