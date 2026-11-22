using System.Text.Json;

using Dapr.Client;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.AspNetCore.DataProtection;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

public sealed class NativeReviewQueueTests
{
    // Set this to a disposable Dapr sidecar with ChatBot's read-model component.
    // Every run uses its own tenant and reads back through a new sidecar client.
    [Theory]
    [InlineData(OperationalQueueFamily.FailedAttachment)]
    [InlineData(OperationalQueueFamily.FailedIngestion)]
    public async Task RestartedReadersRetainOutOfOrderWorkflowHistoryAndIsolateScopes(OperationalQueueFamily family)
    {
        string? endpoint = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_TEST_DAPR_GRPC_ENDPOINT");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(endpoint), "Set HEXALITH_CHATBOT_TEST_DAPR_GRPC_ENDPOINT to an isolated test sidecar.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        CancellationToken token = timeout.Token;
        string tenant = "queue-test-" + Guid.NewGuid().ToString("N");
        DateTimeOffset entered = DateTimeOffset.UtcNow;
        string[] projects = ["project-a", "project-b"];
        using (DaprClient writer = new DaprClientBuilder().UseGrpcEndpoint(endpoint!).Build())
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.AddEventStoreDomainService(typeof(GovernedOperationAggregate).Assembly);
            builder.Services.AddChatBotCommandGateway();
            builder.Services.AddChatBotDaprStateStores();
            builder.Services.AddSingleton<IReadModelStore>(new DaprReadModelStore(writer));
            using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            var handler = ActivatorUtilities.CreateInstance<ChatBotDomainProjectionHandler>(scope.ServiceProvider);
            foreach (string project in projects)
            {
                var failed = new RecordWorkflowReviewTransition(family, "same-subject", project, 1, 0, "failed", "owner_failed");
                var retry = failed with { Attempt = 1, State = "retrying", Reason = "retry_started" };
                var recovery = retry with { State = "recovered", Reason = "owner_confirmed" };
                GovernedOperationState state = new();
                ProjectionEventDto[] events = [Event(failed, 1), Event(retry, 2), Event(recovery, 3)];
                foreach (int index in new[] { 2, 0, 1, 0 })
                {
                    _ = handler.Project(new ProjectionRequest(tenant, "chatbot", failed.StreamId(tenant), [events[index]]));
                }

                ProjectionEventDto Event(RecordWorkflowReviewTransition command, int sequence)
                {
                    string messageId = Guid.NewGuid().ToString("N");
                    var envelope = new CommandEnvelope(messageId, tenant, "chatbot", command.StreamId(tenant),
                        nameof(RecordWorkflowReviewTransition), JsonSerializer.SerializeToUtf8Bytes(command),
                        "queue-test", null, WorkflowReviewCommandWriter.Actor, null);
                    var fact = GovernedOperationAggregate.Handle(command, state, envelope).Events
                        .ShouldHaveSingleItem().ShouldBeOfType<WorkflowReviewTransitionRecorded>();
                    state.Apply(fact);
                    return new(typeof(WorkflowReviewTransitionRecorded).FullName!, JsonSerializer.SerializeToUtf8Bytes(fact, JsonSerializerOptions.Web),
                        "json", sequence, entered.AddSeconds(sequence), "queue-test", messageId);
                }
            }
        }

        using DaprClient reader = new DaprClientBuilder().UseGrpcEndpoint(endpoint!).Build();
        var queues = new ReviewQueueStore(new DaprReadModelStore(reader));
        var subjects = await queues.ReadFamilyAsync(tenant, family, token);
        subjects.Select(subject => subject.Current.IdentityProject).Order().ShouldBe(projects);
        foreach (ReviewQueueSubject subject in subjects)
        {
            subject.Current.State.ShouldBe("recovered");
            subject.Current.Terminal.ShouldBeTrue();
            subject.Current.At.ShouldBe(entered.AddSeconds(3));
            subject.History.Select(fact => fact.Version).ShouldBe([1L, 2L, 3L]);
            (await queues.ReadAsync("other-" + tenant, subject.Current.ItemRef, token)).ShouldBeNull();
            // Read the stored JSON independently of the queue fold and its family index.
            var raw = await reader.GetStateAsync<JsonElement>(ChatBotReadModelStoreNames.StateStoreName,
                ReviewQueueStore.SubjectKey(tenant, subject.Current.ItemRef), cancellationToken: token);
            raw.GetProperty("current").GetProperty("terminal").GetBoolean().ShouldBeTrue();
            raw.GetProperty("history").GetArrayLength().ShouldBe(3);
        }

        (await queues.ReadFamilyAsync(tenant, OperationalQueueFamily.RetryableOperation, token)).Count.ShouldBe(2);

        // The controlled gateway distinguishes receipt from commit. Its committed prefix lives in the
        // disposable sidecar. A restarted reader and real activity consume independent reads of that prefix.
        var owner = new NativeOwnerFixture(reader);
        using var services = new ServiceCollection().AddSingleton<IAuthoritativeEventStreamReader>(owner).BuildServiceProvider();
        var commandWriter = new WorkflowReviewCommandWriter(owner, new DataProtectionChatBotAdmissionMarker(new EphemeralDataProtectionProvider()),
            services.GetRequiredService<IServiceScopeFactory>());
        string phase = family == OperationalQueueFamily.FailedAttachment ? "subject" : "resolve";
        var target = new WorkflowReviewTarget("association", "intake", phase);
        var failedCommand = new RecordWorkflowReviewTransition(family, "subject", "project-a", 7, 0, "failed", "owner_failed", target);
        const string correlation = "retry-correlation";
        await Should.ThrowAsync<InvalidOperationException>(() => commandWriter.RecordAsync(tenant, correlation, failedCommand, token));
        (await owner.ReadAsync(new(tenant, "chatbot", failedCommand.StreamId(tenant)), token)).Stream!.Events.ShouldBeEmpty();
        owner.AllowCommit = true;
        await commandWriter.RecordAsync(tenant, correlation, failedCommand, token);
        var failedEvent = (await owner.ReadAsync(new(tenant, "chatbot", failedCommand.StreamId(tenant)), token)).Stream!.Events.Single();
        var source = new ReviewQueueTransition(tenant, failedCommand.StreamId(tenant), failedEvent.SequenceNumber, failedEvent.MessageId,
            correlation, OperationalQueueFamily.RetryableOperation, failedCommand.StreamId(tenant), "project-a", ["project-a"],
            "failed", false, "owner_failed", "operations-admin", "retry", entered, ProducerFamily: family, RetryTarget: target, Generation: 7);
        var intent = new ReviewQueueRetryRequest("request", failedEvent.MessageId, source.Version,
            OperationalQueueFamilies.ToWireValue(family), "retry-request", 1, entered, correlation, "actor");
        await commandWriter.RecordAsync(tenant, correlation, failedCommand with { Attempt = 1, State = "retrying" }, token);
        using var restarted = new DaprClientBuilder().UseGrpcEndpoint(endpoint!).Build();
        var recovered = failedCommand with { Attempt = 1, State = "recovered", Reason = "confirmed" };
        var adapter = new RetryOwnerAdapter(() => commandWriter.RecordAsync(tenant, correlation, recovered, token));
        var activity = new RejoinReviewOperationActivity(adapter, adapter, new NativeOwnerFixture(restarted));
        (await activity.RunAsync(null!, new(source, intent))).ShouldBeTrue();
        (await activity.RunAsync(null!, new(source, intent))).ShouldBeFalse();
        adapter.Calls.ShouldBe(1);
        var persisted = await restarted.GetStateAsync<StreamReadEvent[]>(ChatBotReadModelStoreNames.StateStoreName, NativeOwnerFixture.Key(tenant, source.StreamId), cancellationToken: token);
        persisted.Length.ShouldBe(3);
        JsonSerializer.Deserialize<WorkflowReviewTransitionRecorded>(persisted[^1].Payload)!.Transition.State.ShouldBe("recovered");
    }

    private sealed class RetryOwnerAdapter(Func<Task> confirm) : IIngestionBindingCoordinator, IAttachmentCaptureCoordinator
    {
        public int Calls { get; private set; }
        public bool IsReady => true;
        public async ValueTask StartAsync(IngestionBindingRequest request, CancellationToken cancellationToken)
        {
            request.RetryPhase.ShouldBe("resolve");
            request.RetryAttempt.ShouldBe(1);
            Calls++;
            await confirm().ConfigureAwait(false);
        }
        public async Task<AttachmentCaptureCoordinatorResult> CaptureAsync(AttachmentCaptureCoordinatorRequest request, CancellationToken cancellationToken = default)
        {
            request.SubjectId.ShouldBe("subject");
            request.SourceVersion.ShouldBe(7);
            Calls++;
            await confirm().ConfigureAwait(false);
            return new(1, 1, 0, request.SubjectId, request.SourceVersion);
        }
    }

    private sealed class NativeOwnerFixture(DaprClient stateStore) : IAuthoritativeEventStreamReader, IEventStoreGatewayClient
    {
        public bool AllowCommit { get; set; }
        internal static string Key(string tenant, string stream) => $"{tenant}-owner-fixture-{stream}";

        public async Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
        {
            var events = await stateStore.GetStateAsync<StreamReadEvent[]>(ChatBotReadModelStoreNames.StateStoreName,
                Key(identity.TenantId, identity.AggregateId), cancellationToken: cancellationToken).ConfigureAwait(false) ?? [];
            return new(new(identity, events.Length, DateTimeOffset.UtcNow, events, "native-fixture"), null);
        }

        public async Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        {
            if (AllowCommit)
            {
                var identity = new AggregateIdentity(request.Tenant, request.Domain, request.AggregateId);
                var prefix = (await ReadAsync(identity, cancellationToken).ConfigureAwait(false)).Stream!;
                var state = new GovernedOperationState();
                foreach (var previous in prefix.Events) { state.Apply(JsonSerializer.Deserialize<WorkflowReviewTransitionRecorded>(previous.Payload)!); }
                var command = request.Payload.Deserialize<RecordWorkflowReviewTransition>()!;
                var envelope = new CommandEnvelope(request.MessageId, request.Tenant, request.Domain, request.AggregateId, request.CommandType,
                    JsonSerializer.SerializeToUtf8Bytes(command), request.CorrelationId!, null, WorkflowReviewCommandWriter.Actor, request.Extensions);
                var result = GovernedOperationAggregate.Handle(command, state, envelope);
                var recorded = result.Events.ShouldHaveSingleItem().ShouldBeOfType<WorkflowReviewTransitionRecorded>();
                StreamReadEvent next = new(prefix.Head + 1, typeof(WorkflowReviewTransitionRecorded).FullName!, JsonSerializer.SerializeToUtf8Bytes(recorded),
                    "json", 1, request.MessageId, request.CorrelationId, null, DateTimeOffset.UtcNow, WorkflowReviewCommandWriter.Actor);
                await stateStore.SaveStateAsync<StreamReadEvent[]>(ChatBotReadModelStoreNames.StateStoreName, Key(request.Tenant, request.AggregateId),
                    [.. prefix.Events, next], cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            return new(request.CorrelationId!, MessageId: request.MessageId);
        }

        public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
