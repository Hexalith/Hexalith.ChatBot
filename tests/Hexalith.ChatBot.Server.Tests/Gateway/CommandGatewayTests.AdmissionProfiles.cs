using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies command admission containment across origins and transports.</summary>
public sealed partial class CommandGatewayTests
{
    /// <summary>Verifies production Admission Should Reject Bookkeeping Without Atomic Participation For Every Origin.</summary>
    [Theory]
    [InlineData(ChatBotSurfaceOrigin.Api)]
    [InlineData(ChatBotSurfaceOrigin.Ui)]
    [InlineData(ChatBotSurfaceOrigin.Cli)]
    [InlineData(ChatBotSurfaceOrigin.Mcp)]
    [InlineData(ChatBotSurfaceOrigin.Worker)]
    [InlineData(ChatBotSurfaceOrigin.Mailbox)]
    [InlineData(ChatBotSurfaceOrigin.Ai)]
    public async Task ProductionAdmissionShouldRejectBookkeepingWithoutAtomicParticipationForEveryOrigin(ChatBotSurfaceOrigin origin)
    {
        FixedClock clock = new();
        SyntheticObservedAtomicAdmissionValidator evidence = SyntheticBookkeepingEvidence(clock);
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore identities = new(new FixedClock());
        InMemoryOperationStatusStore statuses = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: audit, idempotencyStore: identities,
            operationStatusStore: statuses, admissionEvidence: evidence);
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), new TenantScopedCommand(BoundTenant, "resource"), origin: origin);

        ChatBotGatewayResult result = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        result.Problem.Retryable.ShouldBeFalse();
        evidence.OperationCalls.ShouldBe(1);
        evidence.OperationFailure.ShouldBeNull();
        evidence.ProfileCalls.ShouldBe(1);
        evidence.AtomicCalls.ShouldBe(1);
        evidence.AtomicContext!.AdmissionEvidence.ShouldNotBeNull();
        evidence.AtomicContext.AdmissionEnvelope.ShouldNotBeNull();
        dispatcher.DispatchCount.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        (await statuses.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>Verifies caller Admission Selectors Should Fail Before Reservation.</summary>
    [Theory]
    [InlineData("profile")]
    [InlineData("admissionProfile")]
    [InlineData("nextCommand")]
    [InlineData("successorCommand")]
    [InlineData("riskClassification")]
    [InlineData("executionRecord")]
    [InlineData("ExecutionRecord")]
    [InlineData("AdmissionProfile")]
    public async Task CallerAdmissionSelectorsShouldFailBeforeReservation(string selector)
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore identities = new(new FixedClock());
        RecordingAuditWriter audit = new();
        CommandGateway gateway = Gateway(dispatcher, auditWriter: audit, idempotencyStore: identities,
            admissionEvidence: new ChatBotAdmissionEvidenceValidator());
        Dictionary<string, object> payload = new() { ["noteId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAY", ["tenantId"] = BoundTenant, [selector] = "forged" };
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(Principal(BoundTenant), payload,
            commandType: nameof(Contracts.Commands.RecordGovernedNote)), TestContext.Current.CancellationToken);
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.CommandContractInvalid);
        dispatcher.DispatchCount.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
    }

    /// <summary>Verifies production Ai Admission Should Terminate Without Current Qualification And Never Create Proposal.</summary>
    [Fact]
    public async Task ProductionAiAdmissionShouldTerminateWithoutCurrentQualificationAndNeverCreateProposal()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore identities = new(new FixedClock());
        SyntheticAdmissionEvidenceSource source = new() { Qualification = null };
        source.ProfileEvidence = new(BoundTenant, ActorId, nameof(Contracts.Commands.ProposeAIAction), "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            ChatBotAdmissionProfile.AiEffect, 8, null, new Dictionary<string, string>(), source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1));
        CommandGateway gateway = Gateway(dispatcher, auditWriter: audit, idempotencyStore: identities,
            riskClassifier: new Hexalith.ChatBot.Tests.TrustedAuthority.SyntheticLegacyRiskClassifier(), admissionEvidence: new ChatBotAdmissionEvidenceValidator(source, source));
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(Principal(BoundTenant), AiProposalCommand()), TestContext.Current.CancellationToken);
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);
        result.Problem.Retryable.ShouldBeFalse();
        dispatcher.DispatchCount.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.ClassifierIndeterminate).IsTerminal.ShouldBeTrue();
    }
    /// <summary>Verifies required Classification Should Contain Literal Null Recordless Failed And Indeterminate Outputs.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task RequiredClassificationShouldContainLiteralNullRecordlessFailedAndIndeterminateOutputs(int mode)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore identities = new(new FixedClock());
        CommandGateway gateway = Gateway(dispatcher, auditWriter: audit, idempotencyStore: identities,
            riskClassifier: new SyntheticMissingRiskClassifier(mode));
        ChatBotGatewayResult result = await gateway.SubmitAsync(Submission(Principal(BoundTenant), AiProposalCommand()), TestContext.Current.CancellationToken);
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);
        result.Problem.Retryable.ShouldBeFalse();
        dispatcher.DispatchCount.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
    }

    /// <summary>Verifies sdk Admission Should Reject Signed And Unsigned Bookkeeping Without Commit Participation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SdkAdmissionShouldRejectSignedAndUnsignedBookkeepingWithoutCommitParticipation(bool signed)
    {
        FixedClock clock = new();
        SyntheticObservedAtomicAdmissionValidator evidence = SyntheticBookkeepingEvidence(clock);
        RecordingAuditWriter audit = new();
        RecordingReplayIntentQueue replay = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore identities = new(clock);
        InMemoryOperationStatusStore statuses = new();
        ChatBotCommandAdmissionPipeline pipeline = new(new ClaimsAuthenticationStage(), new ClaimsTenantBindingStage(), new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(), new PassThroughApprovalGate(), identities, audit, replay, alerts,
            statuses, clock, new CommandSubmissionLifecycleTransitionGuard(), new PermissiveSpineCommandAllowlist(),
            Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Authorizer(clock), admissionEvidence: evidence);
        Microsoft.AspNetCore.Http.HttpContextAccessor accessor = new()
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = Principal(BoundTenant) },
        };
        Hexalith.ChatBot.Server.Authentication.ChatBotRequestContextResolver resolver = new(accessor);
        DataProtectionChatBotAdmissionMarker marker = new(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        const string operation = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        System.Text.Json.JsonElement payload = System.Text.Json.JsonSerializer.SerializeToElement(new Contracts.Commands.RecordGovernedNote(operation));
        Dictionary<string, string> extensions = new() { ["actorId"] = ActorId, ["surfaceOrigin"] = "api" };
        if (signed)
        {
            extensions[DataProtectionChatBotAdmissionMarker.ExtensionKey] = marker.Create(operation, BoundTenant, operation,
                nameof(Contracts.Commands.RecordGovernedNote), payload, CorrelationId, ActorId, "api", null);
        }
        Hexalith.EventStore.Contracts.Commands.CommandEnvelope envelope = new(operation, BoundTenant, "chatbot", operation,
            nameof(Contracts.Commands.RecordGovernedNote), System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, ActorId, extensions);
        marker.IsValid(envelope).ShouldBe(signed);
        ChatBotDomainServiceAdmissionStage stage = new(pipeline, identities, marker, resolver);
        Hexalith.EventStore.DomainService.DomainServiceAdmissionResult result = await stage.EvaluateAsync(
            new(new Hexalith.EventStore.Contracts.Commands.DomainServiceRequest(envelope, null)), TestContext.Current.CancellationToken);
        result.IsAccepted.ShouldBeFalse();
        result.RejectionEvents.ShouldHaveSingleItem().ShouldBeOfType<ChatBotDomainServiceAdmissionRejected>().ReasonCode.ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        evidence.OperationCalls.ShouldBe(1);
        evidence.OperationFailure.ShouldBeNull();
        // The marked path skips profile checks under D8/D9, deferred to Story 1.8's commit-participation contract.
        evidence.ProfileCalls.ShouldBe(signed ? 0 : 1);
        evidence.AtomicCalls.ShouldBe(1);
        evidence.AtomicContext!.AdmissionEvidence.ShouldNotBeNull();
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        replay.Intents.ShouldBeEmpty();
        alerts.Alerts.ShouldBeEmpty();
        (await statuses.TryGetAsync(BoundTenant, operation, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>The SDK admission hook enforces the same classifier, approval and caller-metadata failures as HTTP.</summary>
    [Theory]
    [InlineData("classifier", ChatBotMessageCodes.ClassifierIndeterminate)]
    [InlineData("approval-required", ChatBotMessageCodes.ApprovalRequired)]
    [InlineData("selector", ChatBotMessageCodes.CommandContractInvalid)]
    [InlineData("execution-record", ChatBotMessageCodes.CommandContractInvalid)]
    [InlineData("subtype", ChatBotMessageCodes.ClassifierIndeterminate)]
    [InlineData("approval-failure", ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    public async Task SdkProcessMustContainEveryProfileFailureWithoutBookkeeping(string failure, string reason)
    {
        SyntheticAdmissionEvidenceSource source = new();
        object command = failure == "approval-failure" ? ApprovedExecutionCommand() : LowRiskExecutionCommand();
        string commandType = command.GetType().Name;
        System.Text.Json.JsonElement payload = System.Text.Json.JsonSerializer.SerializeToElement(command, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        if (failure is "selector" or "subtype" or "execution-record")
        {
            Dictionary<string, object?> fields = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(payload.GetRawText())!;
            fields[failure switch { "selector" => "admissionProfile", "execution-record" => "executionRecord", _ => "assistanceKind" }]
                = failure switch { "selector" => "ai-read-v1", "execution-record" => new { outcome = "forged" }, _ => (object)"undeclared-subtype" };
            payload = System.Text.Json.JsonSerializer.SerializeToElement(fields);
        }
        const string operation = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        ChatBotAdmissionProfile profile = failure == "approval-failure" ? ChatBotAdmissionProfile.AiEffect : ChatBotAdmissionProfile.AiRead;
        source.ProfileEvidence = new(BoundTenant, ActorId, commandType, operation, profile, 8, "synthetic:owner-concurrency",
            ChatBotCommandAdmissionCatalog.RequiredStages(profile).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1),
            new(BoundTenant, 1, "policy-snap-001", true, "read-only", ["summarize-visible-context"], true, true));
        SyntheticObservedAtomicAdmissionValidator evidence = new(new ChatBotAdmissionEvidenceValidator(source, source));
        RecordingAuditWriter audit = new();
        RecordingReplayIntentQueue replay = new();
        RecordingOperatorAlertSink alerts = new();
        InMemoryCoarseIdempotencyStore identities = new(source);
        InMemoryOperationStatusStore statuses = new();
        IRiskClassifier classifier = failure == "classifier" ? new SyntheticMissingRiskClassifier(0)
            : failure == "subtype" ? new DeterministicAiActionRiskClassifier()
            : new Hexalith.ChatBot.Tests.TrustedAuthority.SyntheticLegacyRiskClassifier();
        ChatBotCommandAdmissionPipeline pipeline = new(new ClaimsAuthenticationStage(), new ClaimsTenantBindingStage(), new PassThroughAuthorizationStage(),
            classifier, new AiActionApprovalGate(new Hexalith.ChatBot.Server.Governance.AiMediation.DefaultAiActionPolicyEvaluator(new FixedTenantAiPolicySnapshotProvider(false))),
            identities, audit, replay, alerts, statuses, source, new CommandSubmissionLifecycleTransitionGuard(), new PermissiveSpineCommandAllowlist(),
            Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Authorizer(source), admissionEvidence: evidence);
        Microsoft.AspNetCore.Http.HttpContextAccessor accessor = new()
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = Principal(BoundTenant) },
        };
        DataProtectionChatBotAdmissionMarker marker = new(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        Hexalith.EventStore.Contracts.Commands.CommandEnvelope envelope = new(operation, BoundTenant, "chatbot", "project-001", commandType,
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, ActorId, new Dictionary<string, string> { ["actorId"] = ActorId, ["surfaceOrigin"] = "api" });
        ChatBotDomainServiceAdmissionStage stage = new(pipeline, identities, marker, new(accessor));

        Hexalith.EventStore.DomainService.DomainServiceAdmissionResult result = await stage.EvaluateAsync(
            new(new Hexalith.EventStore.Contracts.Commands.DomainServiceRequest(envelope, null)), TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.RejectionEvents.ShouldHaveSingleItem().ShouldBeOfType<ChatBotDomainServiceAdmissionRejected>().ReasonCode.ShouldBe(reason);
        evidence.OperationCalls.ShouldBe(1);
        evidence.ClassificationCalls.ShouldBe(failure is "approval-required" or "approval-failure" ? 1 : 0);
        evidence.ProfileCalls.ShouldBe(failure == "approval-failure" ? 1 : 0);
        evidence.AtomicCalls.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldBeEmpty();
        replay.Intents.ShouldBeEmpty();
        alerts.Alerts.ShouldBeEmpty();
        (await statuses.TryGetAsync(BoundTenant, operation, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>Authentic markers never bypass current transport subject and tenant binding.</summary>
    [Theory]
    [InlineData("missing-tenant", "authentication_denied")]
    [InlineData("conflicting-tenant", "authentication_denied")]
    [InlineData("mismatched-tenant", "tenant_mismatch")]
    [InlineData("missing-subject", "authentication_denied")]
    [InlineData("conflicting-subject", "authentication_denied")]
    [InlineData("mismatched-user", "authentication_denied")]
    public async Task MarkedBookkeepingMustRejectInvalidCurrentTransportBindingBeforeOperationValidation(string binding, string reason)
    {
        FixedClock clock = new();
        SyntheticObservedAtomicAdmissionValidator evidence = SyntheticBookkeepingEvidence(clock);
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore identities = new(clock);
        List<System.Security.Claims.Claim> claims = [new("sub", ActorId), new("eventstore:tenant", BoundTenant)];
        if (binding == "missing-tenant") { claims.RemoveAll(claim => claim.Type == "eventstore:tenant"); }
        if (binding == "conflicting-tenant") { claims.Add(new("eventstore:tenant", "synthetic-foreign")); }
        if (binding == "missing-subject") { claims.RemoveAll(claim => claim.Type == "sub"); }
        if (binding == "conflicting-subject") { claims.Add(new("sub", "synthetic-other")); }
        System.Security.Claims.ClaimsPrincipal principal = new(new System.Security.Claims.ClaimsIdentity(claims, "synthetic-authenticated"));
        string tenant = binding == "mismatched-tenant" ? "synthetic-foreign" : BoundTenant;
        string user = binding == "mismatched-user" ? "synthetic-other" : ActorId;
        const string operation = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        System.Text.Json.JsonElement payload = System.Text.Json.JsonSerializer.SerializeToElement(new Contracts.Commands.RecordGovernedNote(operation));
        DataProtectionChatBotAdmissionMarker marker = new(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        Dictionary<string, string> extensions = new() { ["actorId"] = user, ["surfaceOrigin"] = "api" };
        extensions[DataProtectionChatBotAdmissionMarker.ExtensionKey] = marker.Create(operation, tenant, operation,
            nameof(Contracts.Commands.RecordGovernedNote), payload, CorrelationId, user, "api", null);
        Hexalith.EventStore.Contracts.Commands.CommandEnvelope envelope = new(operation, tenant, "chatbot", operation,
            nameof(Contracts.Commands.RecordGovernedNote), System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, user, extensions);
        marker.IsValid(envelope).ShouldBeTrue();
        ChatBotCommandAdmissionPipeline pipeline = new(new ClaimsAuthenticationStage(), new ClaimsTenantBindingStage(), new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(), new PassThroughApprovalGate(), identities, audit, new RecordingReplayIntentQueue(), new RecordingOperatorAlertSink(),
            new InMemoryOperationStatusStore(), clock, new CommandSubmissionLifecycleTransitionGuard(), new PermissiveSpineCommandAllowlist(),
            Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Authorizer(clock), admissionEvidence: evidence);
        Microsoft.AspNetCore.Http.HttpContextAccessor accessor = new()
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal },
        };
        ChatBotDomainServiceAdmissionStage stage = new(pipeline, identities, marker, new(accessor));

        Hexalith.EventStore.DomainService.DomainServiceAdmissionResult result = await stage.EvaluateAsync(
            new(new Hexalith.EventStore.Contracts.Commands.DomainServiceRequest(envelope, null)), TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.RejectionEvents.ShouldHaveSingleItem().ShouldBeOfType<ChatBotDomainServiceAdmissionRejected>().ReasonCode.ShouldBe(reason);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(reason);
        evidence.OperationCalls.ShouldBe(0);
        evidence.AtomicCalls.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
    }

    private static SyntheticObservedAtomicAdmissionValidator SyntheticBookkeepingEvidence(FixedClock clock)
    {
        SyntheticAdmissionEvidenceSource source = new() { UtcNow = clock.UtcNow };
        source.ProfileEvidence = new(BoundTenant, ActorId, nameof(Contracts.Commands.RecordGovernedNote), "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            ChatBotAdmissionProfile.CommandExecution, 0, "synthetic:owner-concurrency",
            ChatBotCommandAdmissionCatalog.RequiredStages(ChatBotAdmissionProfile.CommandExecution).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            clock.UtcNow.AddSeconds(-1), clock.UtcNow.AddMinutes(1));
        return new(new ChatBotAdmissionEvidenceValidator(source, clock));
    }
    /// <summary>Verifies caller Cannot Forge Trusted Command Risk Or Effect Metadata.</summary>
    [Theory]
    [InlineData("risk")]
    [InlineData("effect")]
    [InlineData("policy")]
    [InlineData("metadata-supported")]
    public async Task CallerCannotForgeTrustedCommandRiskOrEffectMetadata(string field)
    {
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore identities = new(new FixedClock());
        Contracts.Commands.ProposeAIAction proposal = AiProposalCommand();
        proposal = field switch
        {
            "risk" => proposal with { CommandDefaultRisk = AiActionRiskClass.LowRisk },
            "effect" => proposal with { EffectSurface = "read-only" },
            "metadata-supported" => proposal with { CommandMetadataSupported = false },
            _ => proposal with { TenantPolicyClassification = "low-risk" },
        };
        ChatBotGatewayResult result = await Gateway(dispatcher, idempotencyStore: identities, admissionEvidence: new ChatBotAdmissionEvidenceValidator())
            .SubmitAsync(Submission(Principal(BoundTenant), proposal), TestContext.Current.CancellationToken);
        result.Problem!.Code.ShouldBe(ChatBotMessageCodes.CommandContractInvalid);
        dispatcher.DispatchCount.ShouldBe(0);
        identities.RecordCount.ShouldBe(0);
    }
    /// <summary>Verifies profile Evidence Failures Should Keep Typed Safe Catalog Outcomes.</summary>
    [Theory]
    [InlineData(ChatBotRefusalReasonCodes.ApprovalStateInvalid, ChatBotMessageCodes.InvalidLifecycleTransition)]
    [InlineData(ChatBotRefusalReasonCodes.ApprovedCommandScopeExceeded, ChatBotMessageCodes.RefusalBlockedAction)]
    [InlineData(ChatBotMessageCodes.ProjectAiContextPackageUnavailable, ChatBotMessageCodes.ProjectAiContextPackageUnavailable)]
    public void ProfileEvidenceFailuresShouldKeepTypedSafeCatalogOutcomes(string reason, string code)
    {
        Hexalith.ChatBot.Client.Generated.ProblemDetails problem = DefaultProblemDetailsFactory().CreateAuthorizationProblem(reason, CorrelationId, TaskId);
        problem.Code.ShouldBe(code);
        problem.Details.Visibility.ShouldBe(Hexalith.ChatBot.Client.Generated.ProblemDetailsDetailsVisibility.Metadata_only);
    }
}
