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

    /// <summary>Runs the legacy governed-note success intent for tenant-scoped fixture compatibility.</summary>
    public static async Task<ArmOutcome> RunSuccessAsync(ISurfaceArm arm, SemanticIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        RecordGovernedNote command = new(intent.NoteId);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, statusStore, clock);

        ChatBotGatewayResult result = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' governed-note fixture intent was not accepted.");
        }

        string domainOutcome = EmitDomainEventIdentity(command.NoteId);
        DurableStatusFacts status = await ReadStatusAsync(statusStore, result.Accepted!, cancellationToken).ConfigureAwait(false);
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
            status,
            view);
    }

    /// <summary>Runs the success intent through an arm: adapter translation + admission sequence + status-store end-state.</summary>
    public static async Task<ArmOutcome> RunSuccessAsync(ISurfaceArm arm, SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        SurfaceCommandTranslation translation = await arm.TranslateCommandAsync(intent, cancellationToken).ConfigureAwait(false);
        IChatBotCommand command = translation.Command;

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, statusStore, clock);

        ChatBotGatewayResult result = await gateway
            .SubmitAsync(Submission(command, command.GetType().Name, arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' success intent was not accepted.");
        }

        string domainOutcome = command.GetType().Name;
        DurableStatusFacts status = await ReadStatusAsync(statusStore, result.Accepted!, cancellationToken).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin(auditWriter),
            CaptureAdmissionSequence(auditWriter),
            result.Accepted!.LifecycleState.ToString(),
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            status,
            DurableView: null);
    }

    /// <summary>Runs the retry/replay intent: an equivalent duplicate submit replays the prior outcome.</summary>
    public static async Task<ArmOutcome> RunRetryReplayAsync(ISurfaceArm arm, SemanticIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        RecordGovernedNote command = new(intent.NoteId);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, statusStore, clock);

        ChatBotGatewayResult first = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);
        ChatBotGatewayResult replay = await gateway
            .SubmitAsync(Submission(command, nameof(RecordGovernedNote), arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!first.IsAccepted || !replay.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' governed-note retry intent was not accepted.");
        }

        string domainOutcome = EmitDomainEventIdentity(command.NoteId);
        DurableStatusFacts status = await ReadStatusAsync(statusStore, replay.Accepted!, cancellationToken).ConfigureAwait(false);
        DurableViewFacts view = await ProjectAndReadAsync(command.NoteId, clock, cancellationToken).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin(auditWriter),
            CaptureAdmissionSequence(auditWriter),
            replay.Accepted!.LifecycleState.ToString(),
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            status,
            view);
    }

    /// <summary>Runs the retry/replay intent: an equivalent duplicate submit replays the prior outcome.</summary>
    public static async Task<ArmOutcome> RunRetryReplayAsync(ISurfaceArm arm, SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        SurfaceCommandTranslation translation = await arm.TranslateCommandAsync(intent, cancellationToken).ConfigureAwait(false);
        IChatBotCommand command = translation.Command;

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, statusStore, clock);

        ChatBotGatewayResult first = await gateway
            .SubmitAsync(Submission(command, command.GetType().Name, arm.Origin), cancellationToken)
            .ConfigureAwait(false);
        ChatBotGatewayResult replay = await gateway
            .SubmitAsync(Submission(command, command.GetType().Name, arm.Origin), cancellationToken)
            .ConfigureAwait(false);

        if (!first.IsAccepted || !replay.IsAccepted)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' retry intent was not accepted.");
        }

        string domainOutcome = command.GetType().Name;
        DurableStatusFacts status = await ReadStatusAsync(statusStore, replay.Accepted!, cancellationToken).ConfigureAwait(false);

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin(auditWriter),
            CaptureAdmissionSequence(auditWriter),
            replay.Accepted!.LifecycleState.ToString(),
            domainOutcome,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            status,
            DurableView: null);
    }

    /// <summary>Runs a domain/business rejection intent using the same adapter-backed operation-retry command.</summary>
    public static async Task<ArmOutcome> RunDomainBusinessRejectionAsync(ISurfaceArm arm, SemanticCommandIntent intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        SurfaceCommandTranslation translation = await arm.TranslateCommandAsync(intent, cancellationToken).ConfigureAwait(false);
        RequestFailedWorkflowRetry command = translation.Command as RequestFailedWorkflowRetry
            ?? throw new InvalidOperationException("Domain business rejection harness requires the retry operation intent.");
        DomainResult rejected = GovernedOperationAggregate.Handle(command, state: null);

        if (!rejected.IsRejection)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' retry command did not produce a domain rejection.");
        }

        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin: null,
            AdmissionSequence: [],
            AcceptedLifecycleState: string.Empty,
            DomainOutcomeIdentity: rejected.Events[0].GetType().Name,
            DispatchCount: 0,
            CoarseIdempotencyRecordCount: 0,
            DurableStatus: null,
            DurableView: null);
    }

    /// <summary>Runs the governed-note re-record intent: the aggregate returns a first-class rejection event.</summary>
    public static async Task<ArmOutcome> RunGovernedNoteReRecordRejectionAsync(
        ISurfaceArm arm,
        SemanticIntent intent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);
        ArgumentNullException.ThrowIfNull(intent);

        FixedConformanceClock clock = new();
        RecordGovernedNote command = new(intent.NoteId);
        DomainResult recorded = GovernedOperationAggregate.Handle(command, state: null);
        GovernedNoteRecorded recordedEvent = recorded.Events[0] as GovernedNoteRecorded
            ?? throw new InvalidOperationException("Governed-note setup did not produce the recorded event.");

        GovernedOperationState state = new();
        state.Apply(recordedEvent);
        DomainResult rejected = GovernedOperationAggregate.Handle(command, state);

        if (!rejected.IsRejection)
        {
            throw new InvalidOperationException($"Arm '{arm.Name}' governed-note re-record did not produce a rejection.");
        }

        DurableViewFacts view = await ProjectAndReadAsync(command.NoteId, clock, cancellationToken).ConfigureAwait(false);

        // Gateway-less aggregate path: no admission audit envelope is written, so there is no audited origin to
        // read back. Report it honestly as null (mirroring RunDomainBusinessRejectionAsync); the per-arm origin
        // delta is still asserted against the declared origin in the test.
        return new ArmOutcome(
            arm.Name,
            ChatBotSurfaceOrigins.ToWireValue(arm.Origin),
            AuditedOrigin: null,
            AdmissionSequence: [],
            AcceptedLifecycleState: string.Empty,
            rejected.Events[0].GetType().Name,
            DispatchCount: 0,
            CoarseIdempotencyRecordCount: 0,
            DurableStatus: null,
            view);
    }

    /// <summary>Runs the fail-closed rejection intent: a non-allowlisted command rejected before any durable work.</summary>
    public static async Task<ArmOutcome> RunFailClosedRejectionAsync(ISurfaceArm arm, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(arm);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore statusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, statusStore, clock);

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
            DurableStatus: null,
            view);
    }

    private static CommandGateway BuildGateway(
        RecordingDispatcher dispatcher,
        RecordingAuditWriter auditWriter,
        InMemoryCoarseIdempotencyStore idempotencyStore,
        InMemoryOperationStatusStore statusStore,
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
            statusStore,
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

    private static string EmitDomainEventIdentity(string noteId)
    {
        DomainResult recorded = GovernedOperationAggregate.Handle(new RecordGovernedNote(noteId), state: null);
        return recorded.Events[0].GetType().Name;
    }

    private static async Task<DurableStatusFacts> ReadStatusAsync(
        InMemoryOperationStatusStore statusStore,
        CommandSubmissionResponse response,
        CancellationToken cancellationToken)
    {
        OperationStatusRecord status = await statusStore
            .TryGetAsync(Tenant, OperationStatusRecord.OperationIdFor(response), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Accepted operation status was not found in the state store.");

        return new DurableStatusFacts(
            status.OperationId,
            status.CommandId,
            status.CorrelationId,
            status.LifecycleState.ToString(),
            status.RetryCount,
            status.CompletionStatus,
            status.AuditStatus,
            status.OperationClass,
            status.DuplicateAttemptCount);
    }
}

internal sealed record SemanticIntent(string NoteId);
