using Hexalith.ChatBot.Tests.TrustedAuthority;
using System.Text.Json;

using Dapr.DurableTask.Protobuf;
using Dapr.Workflow.Registration;

using Google.Protobuf.WellKnownTypes;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class CorrectionPropagationProgressReadTests
{
    [Theory]
    [InlineData("before", false)]
    [InlineData("due", true)]
    [InlineData("active", false)]
    [InlineData("unavailable", false)]
    [InlineData("completed", false)]
    [InlineData("failed", false)]
    [InlineData("terminated", false)]
    [InlineData("canceled", false)]
    [InlineData("pending", false)]
    [InlineData("suspended", false)]
    [InlineData("stale-due", false)]
    [InlineData("identity", false)]
    [InlineData("count", false)]
    [InlineData("instance", false)]
    [InlineData("correlation", false)]
    [InlineData("absent-runtime", false)]
    [InlineData("unavailable-runtime", false)]
    [InlineData("non-authoritative-runtime", false)]
    [InlineData("malformed", false)]
    public async Task ProductionProgressReaderShouldBindStatusEligibilityToRunningCurrentSchedule(string scenario, bool eligible)
    {
        const string operationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        RegisteredCorrectionWorkflowContext clock = new();
        DateTimeOffset due = clock.UtcNow.AddSeconds(30);
        CorrectionPropagationWorkflowProgress progress = new(
            scenario == "active" ? CorrectionPropagationWorkflowStatuses.Started : CorrectionPropagationWorkflowStatuses.Retrying,
            clock.InstanceId, "tenant-alpha", "correction-1", 3, 0,
            CorrectionPropagationWorkflowFailureCodes.StoreUnavailable, correlationId,
            RetryCount: scenario == "count" ? 2 : 1, RetryDueAt: scenario == "stale-due" ? due.AddSeconds(30) : due);
        if (scenario == "identity")
        {
            progress = progress with { TenantId = "tenant-other" };
        }
        if (scenario == "instance") { progress = progress with { WorkflowInstanceId = "wf-other" }; }
        if (scenario == "correlation") { progress = progress with { CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAZ" }; }
        using WorkflowStateTransport transport = new()
        {
            Unavailable = scenario == "unavailable",
            Response = new GetInstanceResponse
            {
                Exists = true,
                WorkflowState = new Dapr.DurableTask.Protobuf.WorkflowState
                {
                    InstanceId = clock.InstanceId,
                    Name = nameof(CorrectionPropagationWorkflow),
                    CreatedTimestamp = Timestamp.FromDateTimeOffset(clock.UtcNow),
                    LastUpdatedTimestamp = Timestamp.FromDateTimeOffset(clock.UtcNow),
                    CustomStatus = scenario == "malformed" ? "{invalid" : JsonSerializer.Serialize(progress),
                    WorkflowStatus = scenario switch
                    {
                        "completed" => OrchestrationStatus.Completed,
                        "failed" => OrchestrationStatus.Failed,
                        "terminated" => OrchestrationStatus.Terminated,
                        "canceled" => OrchestrationStatus.Canceled,
                        "pending" => OrchestrationStatus.Pending,
                        "suspended" => OrchestrationStatus.Suspended,
                        _ => OrchestrationStatus.Running,
                    },
                },
            },
        };
        var builder = new DaprWorkflowClientBuilder();
        builder.UseGrpcEndpoint("http://workflow-controlled");
        builder.UseHttpClientFactory(transport);
        using var client = builder.Build();
        DaprCorrectionPropagationWorkflowRuntime runtime = new(client, clock);
        var actual = await runtime.ReadProgressAsync(clock.InstanceId, TestContext.Current.CancellationToken);
        if (scenario is "completed" or "failed" or "terminated" or "canceled" or "pending" or "suspended" or "unavailable" or "malformed")
        {
            actual.ShouldBeNull();
        }
        else
        {
            actual.ShouldBe(progress);
        }
        transport.RequestedPath.ShouldNotBeNull().ShouldEndWith("/GetInstance");

        InMemoryOperationStatusStore store = new();
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", OperationId = operationId, CorrelationId = correlationId,
            AcceptedAt = clock.UtcNow, LifecycleState = LifecycleState.Correcting,
        };
        OperationStatusRecord record = OperationStatusRecord.Accepted("tenant-alpha", accepted, false, clock.UtcNow) with
        {
            NextRetryAt = due, RetryCount = 1, MaxAttempts = 5, WorkflowRetryCount = 1,
            WorkflowInstanceId = clock.InstanceId, WorkflowStatus = CorrectionPropagationWorkflowStatuses.Retrying,
        };
        await store.UpsertAsync(record, TestContext.Current.CancellationToken);
        clock.UtcNow = scenario == "before" ? due.AddTicks(-1) : due;
        QueryEnvelope query = new("tenant-alpha", "chatbot", operationId, ChatBotReadQueryTypes.OperationStatus,
            JsonSerializer.SerializeToUtf8Bytes(new OperationStatusQuery(operationId, null), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            correlationId, "actor-alpha");
        ICorrectionPropagationWorkflowRuntime? selectedRuntime = scenario switch
        {
            "absent-runtime" => null,
            "unavailable-runtime" => new UnavailableCorrectionPropagationWorkflowRuntime(clock),
            "non-authoritative-runtime" => new AvailableNonAuthoritativeWorkflowRuntime(),
            _ => runtime,
        };
        QueryResult result = await new OperationStatusQueryHandler(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, new SyntheticOwnerAuthorityProvider(clock)), store, clock, selectedRuntime)
            .ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.Success.ShouldBeTrue();
        JsonElement status = result.GetPayload();
        status.GetProperty("retryEligible").GetBoolean().ShouldBe(eligible);
        status.TryGetProperty("nextRetryAt", out _).ShouldBe(scenario is "before" or "due");
        (await store.TryGetAsync("tenant-alpha", operationId, TestContext.Current.CancellationToken)).ShouldBe(record);
    }
}
