using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle.Workflows;

public sealed class IngestionBindingWorkflowRunnerTests
{
    [Fact]
    public async Task RunAsync_WaitsForEveryCanonicalUnitThenFinalizesProviderOrderOnce()
    {
        IngestionBindingRequest request = Request();
        RecordingSteps steps = new(Context(request));

        IngestionBindingWorkflowResult result = await IngestionBindingWorkflowRunner.RunAsync(request, steps);

        result.Status.ShouldBe("completed");
        result.PriorCaseId.ShouldBe("case-prior");
        result.MemoryUnitCount.ShouldBe(3);
        steps.Started.Select(static source => (source.RecordKind, source.Ordinal)).ShouldBe(
        [
            (IngestionBindingRecordKind.Message, 0),
            (IngestionBindingRecordKind.Attachment, 1),
            (IngestionBindingRecordKind.Attachment, 2),
        ]);
        steps.Delays.ShouldBe(3);
        IngestionBindingFinalizeInput finalized = steps.Finalized.ShouldHaveSingleItem();
        finalized.CompletedSources.Select(static source => source.MemoryUnitId).ShouldBe(
            ["unit-Message-0", "unit-Attachment-1", "unit-Attachment-2"]);
        steps.Events.Last().ShouldBe("finalize");
    }

    [Fact]
    public async Task RunAsync_RejectsDuplicateAttachmentIdentityBeforeStartingIngestion()
    {
        IngestionBindingRequest request = Request();
        IngestionBindingResolvedContext context = Context(request) with
        {
            Source = Context(request).Source with
            {
                Attachments =
                [
                    new("attachment-1", 1, "application/pdf"),
                    new("attachment-1", 2, "application/pdf"),
                ],
            },
        };
        RecordingSteps steps = new(context);

        await Should.ThrowAsync<InvalidOperationException>(() => IngestionBindingWorkflowRunner.RunAsync(request, steps));

        steps.Started.ShouldBeEmpty();
        steps.Finalized.ShouldBeEmpty();
    }

    [Fact]
    public async Task RunAsync_DurablyRetriesTransientAuthorityContentStatusAndFinalizeFailures()
    {
        IngestionBindingRequest request = Request();
        RecordingSteps steps = new(Context(request))
        {
            ResolveFailuresRemaining = 1,
            StartFailuresRemaining = 1,
            StatusFailuresRemaining = 1,
            FinalizeFailuresRemaining = 1,
        };

        IngestionBindingWorkflowResult result = await IngestionBindingWorkflowRunner.RunAsync(request, steps);

        result.Status.ShouldBe("completed");
        steps.ResolveFailuresRemaining.ShouldBe(0);
        steps.StartFailuresRemaining.ShouldBe(0);
        steps.StatusFailuresRemaining.ShouldBe(0);
        steps.FinalizeFailuresRemaining.ShouldBe(0);
        steps.Finalized.Count.ShouldBe(1);
        steps.Delays.ShouldBe(7);
    }

    [Fact]
    public async Task RunAsync_WaitsForTheExactAcceptedSourceVersionBeforeIngestion()
    {
        IngestionBindingRequest request = Request();
        IngestionBindingResolvedContext exact = Context(request);
        RecordingSteps steps = new(exact);
        steps.ResolveContexts.Enqueue(exact with
        {
            Source = exact.Source with { SourceVersion = request.SourceVersion + 1 },
        });

        IngestionBindingWorkflowResult result = await IngestionBindingWorkflowRunner.RunAsync(request, steps);

        result.Status.ShouldBe("completed");
        steps.ResolveCalls.ShouldBe(2);
        steps.Delays.ShouldBe(4);
        steps.Started.Count.ShouldBe(3);
        steps.Finalized.ShouldHaveSingleItem();
    }

    private static IngestionBindingRequest Request()
        => new(
            "tenant-a",
            "association-1",
            "intake-1",
            "project-1",
            7,
            "correlation-1",
            "workflow-1");

    private static IngestionBindingResolvedContext Context(IngestionBindingRequest request)
        => new(
            "case-prior",
            new ProjectConversationIngestionSource(
                request.TenantId,
                request.AssociatedProjectId,
                request.AssociationId,
                request.IntakeId,
                "mailbox-1",
                "message-1",
                [new("attachment-1", 1, "application/pdf"), new("attachment-2", 2, "text/plain")],
                request.SourceVersion,
                request.CorrelationId));

    private sealed class RecordingSteps(IngestionBindingResolvedContext context) : IIngestionBindingWorkflowSteps
    {
        private readonly Dictionary<string, int> _polls = new(StringComparer.Ordinal);

        public List<IngestionBindingSourceRequest> Started { get; } = [];

        public List<IngestionBindingFinalizeInput> Finalized { get; } = [];

        public List<string> Events { get; } = [];

        public Queue<IngestionBindingResolvedContext> ResolveContexts { get; } = [];

        public int Delays { get; private set; }

        public int ResolveCalls { get; private set; }

        public int ResolveFailuresRemaining { get; set; }

        public int StartFailuresRemaining { get; set; }

        public int StatusFailuresRemaining { get; set; }

        public int FinalizeFailuresRemaining { get; set; }

        public Task<IngestionBindingResolvedContext> ResolveAsync(IngestionBindingRequest request)
        {
            ResolveCalls++;
            if (ResolveFailuresRemaining > 0)
            {
                ResolveFailuresRemaining--;
                throw new InvalidOperationException("projects_context_temporarily_unavailable");
            }

            return Task.FromResult(ResolveContexts.Count == 0 ? context : ResolveContexts.Dequeue());
        }

        public Task<IngestionBindingSourceOperation> StartAsync(IngestionBindingSourceRequest request)
        {
            if (StartFailuresRemaining > 0)
            {
                StartFailuresRemaining--;
                throw new InvalidOperationException("content_temporarily_unavailable");
            }

            Started.Add(request);
            Events.Add($"start:{request.Ordinal}");
            return Task.FromResult(new IngestionBindingSourceOperation(request, $"instance-{request.Ordinal}"));
        }

        public Task<IngestionBindingSourceStatus> GetStatusAsync(IngestionBindingSourceOperation operation)
        {
            if (StatusFailuresRemaining > 0)
            {
                StatusFailuresRemaining--;
                throw new InvalidOperationException("status_temporarily_unavailable");
            }

            int poll = _polls.TryGetValue(operation.InstanceId, out int existing) ? existing + 1 : 1;
            _polls[operation.InstanceId] = poll;
            Events.Add($"status:{operation.Source.Ordinal}:{poll}");
            bool completed = poll > 1;
            return Task.FromResult(new IngestionBindingSourceStatus(
                completed ? "Completed" : "Running",
                completed ? $"unit-{operation.Source.RecordKind}-{operation.Source.Ordinal}" : null,
                completed));
        }

        public Task DelayAsync(TimeSpan delay)
        {
            Delays++;
            Events.Add("delay");
            return Task.CompletedTask;
        }

        public Task FinalizeAsync(IngestionBindingFinalizeInput input)
        {
            if (FinalizeFailuresRemaining > 0)
            {
                FinalizeFailuresRemaining--;
                throw new InvalidOperationException("finalize_temporarily_unavailable");
            }

            Finalized.Add(input);
            Events.Add("finalize");
            return Task.CompletedTask;
        }
    }
}
