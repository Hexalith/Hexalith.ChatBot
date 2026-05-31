using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;

namespace Hexalith.ChatBot.Conformance.Tests.Harness;

/// <summary>
/// Drives a tenant-mismatched (or stale/missing/ambiguous-tenant) mutating command for one persona through the
/// REAL gateway lane (the same <see cref="CommandGateway"/> and <see cref="ClaimsTenantBindingStage"/> the HTTP
/// command endpoint uses), then captures the fail-closed evidence: the metadata-only denial, the single
/// authorization-failure fact, and the durable-effect counters (dispatch, coarse-idempotency admission,
/// pre/post-commit audit envelopes, operation-status record). No gateway stage is replicated — the persona only
/// constructs an <see cref="Contracts.Commands.IChatBotCommand"/> and submits. The shared production stores
/// (<see cref="InMemoryCoarseIdempotencyStore"/>, <see cref="InMemoryOperationStatusStore"/>) are consumed
/// directly via IVT so their behavior cannot diverge from production.
/// </summary>
internal static class CrossTenantIsolationHarness
{
    /// <summary>The bound actor id used for every persona submission.</summary>
    public const string BoundActorId = "actor-alpha";

    /// <summary>The fixed correlation id for the in-process lane.</summary>
    public const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    /// <summary>The fixed task id for the in-process lane.</summary>
    public const string TaskId = "01ARZ3NDEKTSV4RRFFQ69G5FAX";

    /// <summary>The fixed command id for the in-process lane.</summary>
    public const string CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    /// <summary>The unresolved/ambiguous tenant-context shape a persona presents.</summary>
    internal enum TenantContextVariant
    {
        /// <summary>A bound principal with a foreign tenant in the command body (<c>TenantId</c> property).</summary>
        ForeignTargetCommandBody,

        /// <summary>A bound principal with a foreign tenant in a scoped identifier (<c>tenant:resource:id</c>).</summary>
        ForeignTargetScopedIdentifier,

        /// <summary>A bound principal with a foreign tenant nested inside a JSON command body.</summary>
        ForeignTargetNestedJson,

        /// <summary>A principal with no tenant claim at all.</summary>
        MissingTenantClaim,

        /// <summary>A principal carrying two distinct tenant claims (ambiguous).</summary>
        MultipleTenantClaims,

        /// <summary>A principal carrying a safe but obsolete tenant claim.</summary>
        StaleTenantClaim,

        /// <summary>A principal carrying an unsafe (whitespace) tenant claim.</summary>
        UnsafeTenantClaim,
    }

    /// <summary>
    /// The captured fail-closed evidence for one persona + variant. The two serialized artifacts are scanned by
    /// the leakage gate; the counters prove no durable work occurred before the denial.
    /// </summary>
    /// <param name="Persona">The persona label.</param>
    /// <param name="DeclaredOrigin">The persona's declared surface-origin wire token.</param>
    /// <param name="IsAccepted">Whether the gateway accepted the submission (must be false).</param>
    /// <param name="ProblemCode">The user-facing problem code.</param>
    /// <param name="ProblemCategory">The user-facing problem category.</param>
    /// <param name="AuthorizationFailureReasonCode">The recorded authorization-failure reason code.</param>
    /// <param name="AuthorizationFailureSurfaceOrigin">The surface origin recorded on the failure fact.</param>
    /// <param name="AuthorizationFailureCount">The number of authorization-failure facts recorded.</param>
    /// <param name="DispatchCount">The number of dispatcher calls (must be zero).</param>
    /// <param name="CoarseIdempotencyRecordCount">The number of coarse-idempotency admissions (must be zero).</param>
    /// <param name="AuditEnvelopeCount">The number of pre/post-commit audit envelopes (must be zero).</param>
    /// <param name="OperationStatusRecordExists">Whether any operation-status record exists (must be false).</param>
    /// <param name="SerializedProblem">The serialized user-facing problem (scanned with the full corpus).</param>
    /// <param name="SerializedAuthorizationFailures">The serialized internal failure facts (scanned excluding the bound tenant).</param>
    internal sealed record MutatingDenialOutcome(
        string Persona,
        string DeclaredOrigin,
        bool IsAccepted,
        string? ProblemCode,
        string? ProblemCategory,
        string? AuthorizationFailureReasonCode,
        string? AuthorizationFailureSurfaceOrigin,
        int AuthorizationFailureCount,
        int DispatchCount,
        int CoarseIdempotencyRecordCount,
        int AuditEnvelopeCount,
        bool OperationStatusRecordExists,
        string SerializedProblem,
        string SerializedAuthorizationFailures);

    /// <summary>Runs one persona's tenant-mismatched submission through the real gateway lane.</summary>
    /// <param name="persona">The actor persona.</param>
    /// <param name="variant">The unresolved-tenant variant to present.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The captured fail-closed evidence.</returns>
    public static async Task<MutatingDenialOutcome> RunMutatingDenialAsync(
        IsolationActorPersona persona,
        TenantContextVariant variant,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(persona);

        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter auditWriter = new();
        FixedConformanceClock clock = new();
        InMemoryCoarseIdempotencyStore idempotencyStore = new(clock);
        InMemoryOperationStatusStore operationStatusStore = new();
        CommandGateway gateway = BuildGateway(dispatcher, auditWriter, idempotencyStore, operationStatusStore, clock);

        (ClaimsPrincipal principal, object command, string commandType) = BuildSubmission(persona, variant);

        ChatBotCommandSubmission submission = new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = CommandId,
                CommandType = commandType,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            CorrelationId,
            TaskId,
            persona.Origin);

        ChatBotGatewayResult result = await gateway.SubmitAsync(submission, cancellationToken).ConfigureAwait(false);

        bool statusExists =
            await operationStatusStore.TryGetAsync(CrossTenantLeakageCorpus.BoundTenant, TaskId, cancellationToken).ConfigureAwait(false) is not null
            || await operationStatusStore.TryGetAsync(CrossTenantLeakageCorpus.ForeignTenant, TaskId, cancellationToken).ConfigureAwait(false) is not null;

        JsonSerializerOptions web = new(JsonSerializerDefaults.Web);
        string serializedProblem = result.Problem is null ? "{}" : JsonSerializer.Serialize(result.Problem, web);
        string serializedFailures = JsonSerializer.Serialize(auditWriter.AuthorizationFailures, web);
        ChatBotAuthorizationFailureAuditFact? fact = auditWriter.AuthorizationFailures.Count > 0
            ? auditWriter.AuthorizationFailures[0]
            : null;

        return new MutatingDenialOutcome(
            persona.Label,
            persona.DeclaredOrigin,
            result.IsAccepted,
            result.Problem?.Code,
            result.Problem?.Category.ToString(),
            fact?.ReasonCode,
            fact?.SurfaceOrigin,
            auditWriter.AuthorizationFailures.Count,
            dispatcher.DispatchCount,
            idempotencyStore.RecordCount,
            auditWriter.Envelopes.Count,
            statusExists,
            serializedProblem,
            serializedFailures);
    }

    private static (ClaimsPrincipal Principal, object Command, string CommandType) BuildSubmission(
        IsolationActorPersona persona,
        TenantContextVariant variant)
    {
        CrossTenantProbeCommand boundBodyProbe = Probe(CrossTenantLeakageCorpus.BoundTenant);

        return variant switch
        {
            TenantContextVariant.ForeignTargetCommandBody =>
                (BoundPrincipal(persona), Probe(CrossTenantLeakageCorpus.ForeignTenant), nameof(CrossTenantProbeCommand)),

            TenantContextVariant.ForeignTargetScopedIdentifier =>
                (BoundPrincipal(persona),
                 new CrossTenantScopedIdentifierProbeCommand($"{CrossTenantLeakageCorpus.ForeignTenant}:chatbot:{CrossTenantLeakageCorpus.ForeignNoteId}"),
                 nameof(CrossTenantScopedIdentifierProbeCommand)),

            TenantContextVariant.ForeignTargetNestedJson =>
                (BoundPrincipal(persona), NestedJsonProbe(), "CrossTenantNestedJsonProbe"),

            TenantContextVariant.MissingTenantClaim =>
                (PrincipalWithTenants(persona), boundBodyProbe, nameof(CrossTenantProbeCommand)),

            TenantContextVariant.MultipleTenantClaims =>
                (PrincipalWithTenants(persona, CrossTenantLeakageCorpus.BoundTenant, CrossTenantLeakageCorpus.ForeignTenant),
                 boundBodyProbe,
                 nameof(CrossTenantProbeCommand)),

            TenantContextVariant.StaleTenantClaim =>
                (PrincipalWithTenants(persona, $"{CrossTenantLeakageCorpus.BoundTenant}-stale"),
                 boundBodyProbe,
                 nameof(CrossTenantProbeCommand)),

            TenantContextVariant.UnsafeTenantClaim =>
                (PrincipalWithTenants(persona, "unsafe tenant value"), boundBodyProbe, nameof(CrossTenantProbeCommand)),

            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown tenant-context variant."),
        };
    }

    private static CrossTenantProbeCommand Probe(string tenantId)
        => new(
            tenantId,
            CrossTenantLeakageCorpus.Sentinel("candidate"),
            CrossTenantLeakageCorpus.Sentinel("evidence"),
            CrossTenantLeakageCorpus.Sentinel("file"),
            CrossTenantLeakageCorpus.Sentinel("cursor"),
            CrossTenantLeakageCorpus.Sentinel("path-fragment"),
            CrossTenantLeakageCorpus.Sentinel("provider-snippet"),
            CrossTenantLeakageCorpus.Sentinel("exception-text"));

    private static JsonElement NestedJsonProbe()
    {
        string json =
            $$"""
            {
              "scope": { "tenantId": "{{CrossTenantLeakageCorpus.ForeignTenant}}" },
              "foreignCandidate": "{{CrossTenantLeakageCorpus.Sentinel("candidate")}}",
              "foreignEvidence": "{{CrossTenantLeakageCorpus.Sentinel("evidence")}}",
              "foreignFile": "{{CrossTenantLeakageCorpus.Sentinel("file")}}",
              "foreignCursor": "{{CrossTenantLeakageCorpus.Sentinel("cursor")}}"
            }
            """;

        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static ClaimsPrincipal BoundPrincipal(IsolationActorPersona persona)
        => PrincipalWithTenants(persona, CrossTenantLeakageCorpus.BoundTenant);

    private static ClaimsPrincipal PrincipalWithTenants(IsolationActorPersona persona, params string[] tenantIds)
    {
        List<Claim> claims = [new("sub", BoundActorId)];
        claims.AddRange(tenantIds.Select(static tenantId => new Claim("eventstore:tenant", tenantId)));
        claims.AddRange(persona.RoleMetadataClaims.Select(static claim => new Claim(claim.Key, claim.Value)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static CommandGateway BuildGateway(
        RecordingDispatcher dispatcher,
        RecordingAuditWriter auditWriter,
        InMemoryCoarseIdempotencyStore idempotencyStore,
        InMemoryOperationStatusStore operationStatusStore,
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
            operationStatusStore,
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist());
}
