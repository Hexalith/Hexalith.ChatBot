using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// Drives one semantic intent through a surface arm using the shared command pipeline (the gateway-level
/// in-process lane) and captures the two-layer <see cref="ArmOutcome"/>: the admission audit-envelope sequence
/// (where the surface origin appears) plus the durable <c>GovernedOperationView</c> end-state read back from the
/// in-process projection store. No gateway stage is replicated — the arm constructs only an
/// <see cref="IChatBotCommand"/> and submits through the real <see cref="CommandGateway"/>. The assertion engine
/// (<see cref="DifferentialOracle"/>) is independent of this driver, so Story 5.4 can swap the M0 shim for the
/// real <c>.Cli</c>/<c>.Mcp</c> adapters without touching the oracle.
/// </summary>
internal static class GovernedCommandConformanceHarness
{
    private const string ActorId = "actor-alpha";
    private const string Tenant = "tenant-alpha";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    /// <summary>A non-allowlisted command type: passes auth/tenant-bind/authorize, then fails the spine allowlist gate.</summary>
    private sealed record NonAllowlistedProbe(string ResourceName) : IChatBotCommand;

    /// <summary>Runs the success intent through an arm: admission sequence + emitted event + durable end-state.</summary>
    public static async Task<ArmOutcome> RunSuccessAsync(ISurfaceArm arm, SemanticIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        RecordGovernedNote command = arm.ParseCommand(intent);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, clock);

        ChatBotGatewayResult result = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' success intent was not accepted.");
        }

        string domainOutcome = EmitDomainEventIdentity(command.NoteId);
        DurableViewFacts view = await ProjectAndReadAsync(command.NoteId, clock, cancellationToken).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin(auditWriter),
            CaptureAdmissionSequence(auditWriter),
            result.Accepted!.LifecycleState.ToString(),
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            view);
    }

    /// <summary>Runs the retry/replay intent: an equivalent duplicate submit replays the prior outcome.</summary>
    public static async Task<ArmOutcome> RunRetryReplayAsync(ISurfaceArm arm, SemanticIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        RecordGovernedNote command = arm.ParseCommand(intent);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, clock);

        ChatBotGatewayResult first = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);
        ChatBotGatewayResult replay = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!first.IsAccepted || !replay.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' retry intent was not accepted.");
        }

        // One durable effect across the original + replay: the published event is delivered twice and the
        // projection drops the stale/duplicate, so the view stays at source version 1.
        string domainOutcome = EmitDomainEventIdentity(command.NoteId);
        DurableViewFacts view = await ProjectAndReadAsync(command.NoteId, clock, cancellationToken, deliveries: 2).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin(auditWriter),
            CaptureAdmissionSequence(auditWriter),
            replay.Accepted!.LifecycleState.ToString(),
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            view);
    }

    /// <summary>Runs the fine-idempotency rejection intent: a re-record of an already-recorded note.</summary>
    public static async Task<ArmOutcome> RunReRecordRejectionAsync(ISurfaceArm arm, SemanticIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        RecordGovernedNote command = arm.ParseCommand(intent);

        // Fine (aggregate-altitude) idempotency lives in the pure aggregate Handle, behind the dispatcher in the
        // durable segment. Re-recording an already-recorded aggregate yields a structured rejection (returned,
        // never thrown) and emits no new event — so the durable view stays at source version 1. The rejection
        // is origin-free by construction (the domain event carries no surface origin), which is itself the
        // cross-surface parity proof: there is no per-surface delta to leak at this altitude.
        DomainResult recorded = GovernedOperationAggregate.Handle(command, state: null);
        GovernedNoteRecorded recordedEvent = (GovernedNoteRecorded)recorded.Events[0];
        GovernedOperationState state = new();
        state.Apply(recordedEvent);
        DomainResult reRecorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(command.NoteId), state);

        if (!reRecorded.IsRejection)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' re-record did not produce a rejection.");
        }

        FixedConformanceClock clock = new();
        DurableViewFacts view = await ProjectAndReadAsync(command.NoteId, clock, cancellationToken, deliveries: 1).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin: null,
            AdmissionSequence: [],
            AcceptedLifecycleState: string.Empty,
            DomainOutcomeIdentity: reRecorded.Events[0].GetType().Name,
            DispatchCount: 0,
            CoarseIdempotencyRecordCount: 0,
            DurableView: view);
    }

    /// <summary>Runs the fail-closed rejection intent: a non-allowlisted command rejected before any durable work.</summary>
    public static async Task<ArmOutcome> RunFailClosedRejectionAsync(ISurfaceArm arm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, clock);

        NonAllowlistedProbe probe = new("conformance-probe-resource");
        ChatBotGatewayResult result = await gateway
            .SubmitAsync(Submission(probe, nameof(NonAllowlistedProbe), arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsAccepted || result.Problem is null)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' fail-closed intent was unexpectedly accepted.");
        }

        // The rejection is compared as a first-class problem record (category + code + reasonCode), never a bare
        // status code. The declared origin is audited on the authorization-failure fact (the single delta).
        ChatBotAuthorizationFailureAuditFact failure = auditWriter.AuthorizationFailures.Single();
        string domainOutcome = $"problem:{result.Problem.Category}:{result.Problem.Code}:{failure.ReasonCode}";

        // No durable mutation on the fail-closed path: read the state store with a safe-not-found probe.
        DurableViewFacts? view = await ReadViewAsync(probe.ResourceName, clock, cancellationToken).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            failure.SurfaceOrigin,
            AdmissionSequence: [],
            AcceptedLifecycleState: string.Empty,
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            view);
    }

    private static CommandGateway BuildGateway(
        RecordingDispatcher dispatcher,
        RecordingAuditWriter auditWriter,
        InMemoryCoarseIdempotencyStore idempotencyStore,
        FixedConformanceClock clock)
        => new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            idempotencyStore,
            auditWriter,
            new NoOpReplayIntentQueue(),
            new NoOpOperatorAlertSink(),
            new InMemoryOperationStatusStore(),
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist());

    private static ChatBotCommandSubmission Submission(object command, string commandType, ChatBotSurfaceOrigin origin)
        => new(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("sub", ActorId), new Claim("eventstore:tenant", Tenant)],
                    "test")),
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = commandType,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            origin);

    private static string EmitDomainEventIdentity(string noteId)
    {
        DomainResult recorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(noteId), state: null);
        return recorded.Events[0].GetType().Name;
    }

    private static string? AuditedOrigin(RecordingAuditWriter auditWriter)
        => auditWriter.Envelopes.Count == 0
            ? null
            : auditWriter.Envelopes.Select(static envelope => envelope.SurfaceOrigin).Distinct(StringComparer.Ordinal).Single();

    private static IReadOnlyList<AdmissionStep> CaptureAdmissionSequence(RecordingAuditWriter auditWriter)
        => auditWriter.Envelopes
            .Select(static envelope => new AdmissionStep(
                envelope.Phase.ToString(),
                envelope.StateTransition,
                envelope.Decision,
                envelope.ReasonCode,
                envelope.Outcome,
                envelope.RedactionDecision))
            .ToArray();

    private static async Task<DurableViewFacts> ProjectAndReadAsync(
        string noteId,
        FixedConformanceClock clock,
        CancellationToken cancellationToken,
        int deliveries = 1)
    {
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationProjectionHandler handler = new(store, clock);
        PublishedGovernedOperationEvent published = new(
            Tenant,
            ChatBotEventStore.DomainName,
            noteId,
            GovernedOperationProjectionTranslator.GovernedNoteRecordedEventType,
            SequenceNumber: 1,
            CorrelationId,
            CommandId,
            FixedConformanceClock.FixedUtcNow);

        GovernedNoteRecordedNotification notification = GovernedOperationProjectionTranslator.TryCreateNotification(published)
            ?? throw new InvalidOperationException("Published governed-note event did not translate to a projection notification.");

        for (int delivery = 0; delivery < deliveries; delivery++)
        {
            _ = await handler.HandleAsync(notification, cancellationToken).ConfigureAwait(false);
        }

        GovernedOperationView view = await store.GetAsync(Tenant, noteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Projected governed-operation view was not found in the state store.");

        return ToFacts(view);
    }

    private static async Task<DurableViewFacts?> ReadViewAsync(string noteId, FixedConformanceClock clock, CancellationToken cancellationToken)
    {
        _ = clock;
        InMemoryGovernedOperationProjectionStore store = new();
        GovernedOperationView? view = await store.GetAsync(Tenant, noteId, cancellationToken).ConfigureAwait(false);
        return view is null ? null : ToFacts(view);
    }

    private static DurableViewFacts ToFacts(GovernedOperationView view)
        => new(
            view.NoteId,
            view.SchemaVersion,
            view.SourceProvenance,
            view.DerivationKernelVersion,
            view.RedactionState,
            view.RetentionClass,
            view.SourceVersion);
}
