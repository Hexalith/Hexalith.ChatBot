using System.Text.Json;

using Dapr.Workflow;
using Dapr.Workflow.Abstractions;
using Dapr.Workflow.Worker;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class CorrectionPropagationWorkflowBridgeTests
{
    [Fact]
    public async Task RegisteredResolverMustExecuteAtMostFiveTimesAndExposeTerminalEscalationWithOriginalCause()
    {
        RegisteredCorrectionWorkflowContext context = new() { AlwaysFailResolution = true, AutoAdvanceTimers = true };
        InMemoryOperationStatusStore store = new();
        const string operationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", OperationId = operationId, CorrelationId = correlationId,
            AcceptedAt = context.UtcNow, LifecycleState = LifecycleState.Correcting,
        };
        await store.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, context.UtcNow), TestContext.Current.CancellationToken);
        ServiceCollection services = new();
        services.AddLogging();
        services.AddChatBotCorrectionPropagationWorkflow();
        services.AddSingleton<ISystemClock>(context);
        services.AddSingleton<IMemoriesCaseResolver>(context);
        services.AddSingleton<IOperationStatusStore>(store);
        services.AddSingleton<ICorrectionPropagationWorkflowStatusSink, OperationStatusWorkflowStatusSink>();
        using ServiceProvider provider = services.BuildServiceProvider();
        context.Services = provider;
        context.Factory = provider.GetRequiredService<IWorkflowsFactory>();
        (provider.GetService<WorkflowRuntimeOptions>() ?? provider.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value).ApplyRegistrations(context.Factory);
        context.Factory.TryCreateWorkflow(new TaskIdentifier(nameof(CorrectionPropagationWorkflow)), provider, out var workflow, out Exception? failure).ShouldBeTrue(failure?.Message);
        CorrectionPropagationRequest request = new("tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV", accepted.CommandId,
            "correction-1", context.InstanceId, "project-001", "project-002", 3, correlationId,
            context.UtcNow, context.UtcNow.AddMinutes(10), OperationId: operationId);
        await Should.ThrowAsync<InvalidOperationException>(() => workflow!.RunAsync(context, request));
        context.ResolverRetryOptions.ShouldBeNull();
        context.ResolutionCalls.ShouldBe(5);
        context.Progress!.Status.ShouldBe(CorrectionPropagationWorkflowStatuses.Failed);
        context.Progress.RetryCount.ShouldBe(5);
        context.Progress.StoresCompleted.ShouldBe(0);
        JsonElement status = await StatusAsync(store, context, operationId, correlationId);
        status.GetProperty("maxAttempts").GetInt32().ShouldBe(5);
        status.GetProperty("retryCount").GetInt32().ShouldBe(context.ResolutionCalls);
        status.GetProperty("completionStatus").GetString().ShouldBe("failed");
        status.GetProperty("reasonCode").GetString().ShouldBe(ChatBotMessageCodes.AssociationCorrectionPropagationFailed);
        status.GetProperty("failureReasonCode").GetString().ShouldBe(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable);
        status.GetProperty("workflowLastFailureCode").GetString().ShouldBe(CorrectionPropagationWorkflowFailureCodes.CaseResolutionUnavailable);
        status.GetProperty("safeNextActions").EnumerateArray().Single().GetString().ShouldBe("escalate");
        status.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        status.TryGetProperty("nextRetryAt", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task RegisteredPendingStoreWorkflowShouldSuppressOldScheduleWhenActiveClearAndLaterPublicationFail()
    {
        RegisteredCorrectionWorkflowContext context = new()
        {
            PendingStoreCycles = true, FailActivePublication = true, FailLaterScheduledPublication = true,
        };
        InMemoryOperationStatusStore statusStore = new();
        const string operationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", OperationId = operationId, CorrelationId = correlationId,
            AcceptedAt = context.UtcNow, LifecycleState = LifecycleState.Correcting,
        };
        await statusStore.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, context.UtcNow), TestContext.Current.CancellationToken);
        ServiceCollection services = new();
        services.AddLogging();
        services.AddChatBotCorrectionPropagationWorkflow();
        services.AddSingleton<ISystemClock>(context);
        services.AddSingleton<IMemoriesCaseResolver>(context);
        services.AddSingleton<IOperationStatusStore>(statusStore);
        services.AddSingleton<ICorrectionPropagationWorkflowStatusSink, OperationStatusWorkflowStatusSink>();
        using ServiceProvider provider = services.BuildServiceProvider();
        context.Services = provider;
        context.Factory = provider.GetRequiredService<IWorkflowsFactory>();
        (provider.GetService<WorkflowRuntimeOptions>() ?? provider.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value)
            .ApplyRegistrations(context.Factory);
        context.Factory.TryCreateWorkflow(new TaskIdentifier(nameof(CorrectionPropagationWorkflow)), provider, out var workflow, out Exception? failure)
            .ShouldBeTrue(failure?.Message);
        CorrectionPropagationRequest request = new(
            "tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV", accepted.CommandId,
            "correction-1", context.InstanceId, "project-001", "project-002", 3, correlationId,
            context.UtcNow, context.UtcNow.AddMinutes(10), CorrectedCaseId: "case-corrected", OperationId: operationId);
        Task<object?> running = workflow!.RunAsync(context, request);
        await context.ScheduledStatus.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        DateTimeOffset firstDue = context.TimerDueAt!.Value;
        context.UtcNow = firstDue;
        (await StatusAsync(statusStore, context, operationId, correlationId)).GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        context.Timer.TrySetResult();
        await context.SecondSchedule.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.TimerDueAt.ShouldBe(firstDue.AddSeconds(30));
        context.Progress!.Status.ShouldBe(CorrectionPropagationWorkflowStatuses.Retrying);
        context.Progress.RetryCount.ShouldBe(0);
        context.Progress.RetryDueAt.ShouldBe(context.TimerDueAt);
        OperationStatusRecord projected = (await statusStore.TryGetAsync("tenant-alpha", operationId, TestContext.Current.CancellationToken))!;
        projected.NextRetryAt.ShouldBe(firstDue);
        projected.WorkflowRetryCount.ShouldBe(0);
        JsonElement stale = await StatusAsync(statusStore, context, operationId, correlationId);
        stale.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        stale.TryGetProperty("nextRetryAt", out _).ShouldBeFalse();
        context.UtcNow = context.TimerDueAt.Value;
        context.Timer.TrySetResult();
        await context.ThirdStore.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.StoreCompletion.TrySetResult();
        _ = await running.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.Progress!.Status.ShouldBe(CorrectionPropagationWorkflowStatuses.Completed);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task RegisteredWorkflowShouldUseSharedUtcAndAdvanceTimersDespiteStatusPublicationFailures(bool failScheduled, bool failActive)
    {
        RegisteredCorrectionWorkflowContext context = new() { FailScheduledPublication = failScheduled, FailActivePublication = failActive };
        InMemoryOperationStatusStore statusStore = new();
        const string operationId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
        const string correlationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        CommandSubmissionResponse accepted = new()
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", OperationId = operationId, CorrelationId = correlationId,
            AcceptedAt = context.UtcNow, LifecycleState = LifecycleState.Correcting,
        };
        await statusStore.UpsertAsync(OperationStatusRecord.Accepted("tenant-alpha", accepted, false, context.UtcNow), TestContext.Current.CancellationToken);
        ServiceCollection services = new();
        services.AddLogging();
        services.AddChatBotCorrectionPropagationWorkflow();
        services.AddSingleton<ISystemClock>(context);
        services.AddSingleton<IMemoriesCaseResolver>(context);
        services.AddSingleton<IOperationStatusStore>(statusStore);
        services.AddSingleton<ICorrectionPropagationWorkflowStatusSink, OperationStatusWorkflowStatusSink>();
        using ServiceProvider provider = services.BuildServiceProvider();
        context.Services = provider;
        context.Factory = provider.GetRequiredService<IWorkflowsFactory>();
        WorkflowRuntimeOptions options = provider.GetService<WorkflowRuntimeOptions>()
            ?? provider.GetRequiredService<IOptions<WorkflowRuntimeOptions>>().Value;
        options.ApplyRegistrations(context.Factory);
        context.Factory.TryCreateWorkflow(new TaskIdentifier(nameof(CorrectionPropagationWorkflow)), provider, out var workflow, out Exception? activationFailure)
            .ShouldBeTrue(activationFailure?.Message);
        CorrectionPropagationRequest request = new(
            "tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV", accepted.CommandId,
            "correction-1", context.InstanceId, "project-001", "project-002", 3, correlationId,
            context.UtcNow, context.UtcNow.AddMinutes(10), OperationId: operationId);
        Task<object?> running = workflow!.RunAsync(context, request);
        await context.ScheduledStatus.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.TimerDueAt.ShouldBe(context.UtcNow.AddSeconds(30));
        context.Progress!.StoresCompleted.ShouldBe(0);
        context.Progress.RetryCount.ShouldBe(1);
        OperationStatusRecord before = (await statusStore.TryGetAsync("tenant-alpha", operationId, TestContext.Current.CancellationToken))!;
        if (!failScheduled)
        {
            before.NextRetryAt.ShouldBe(context.TimerDueAt);
            (await StatusAsync(statusStore, context, operationId, correlationId)).GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
            context.UtcNow = context.TimerDueAt!.Value;
            (await StatusAsync(statusStore, context, operationId, correlationId)).GetProperty("retryEligible").GetBoolean().ShouldBeTrue();
        }
        else
        {
            before.NextRetryAt.ShouldBeNull();
            context.UtcNow = context.TimerDueAt!.Value;
        }

        context.Timer.TrySetResult();
        await context.ActiveAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.ResolutionCalls.ShouldBe(2);
        context.Progress!.Status.ShouldBe(CorrectionPropagationWorkflowStatuses.Started);
        JsonElement active = await StatusAsync(statusStore, context, operationId, correlationId);
        active.GetProperty("retryEligible").GetBoolean().ShouldBeFalse();
        active.TryGetProperty("nextRetryAt", out _).ShouldBeFalse();
        if (failActive)
        {
            (await statusStore.TryGetAsync("tenant-alpha", operationId, TestContext.Current.CancellationToken))!.NextRetryAt.ShouldNotBeNull();
        }
        context.ResolvedCase.TrySetResult("case-corrected");
        _ = await running.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        context.Progress!.Status.ShouldBe(CorrectionPropagationWorkflowStatuses.Completed);
    }

    private static async Task<JsonElement> StatusAsync(InMemoryOperationStatusStore store, RegisteredCorrectionWorkflowContext context,
        string operationId, string correlationId)
    {
        QueryEnvelope query = new("tenant-alpha", "chatbot", operationId, ChatBotReadQueryTypes.OperationStatus,
            JsonSerializer.SerializeToUtf8Bytes(new OperationStatusQuery(operationId, null), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            correlationId, "actor-alpha");
        QueryResult result = await new OperationStatusQueryHandler(store, context, context).ExecuteAsync(query, TestContext.Current.CancellationToken).ConfigureAwait(false);
        result.Success.ShouldBeTrue();
        return result.GetPayload();
    }
}
