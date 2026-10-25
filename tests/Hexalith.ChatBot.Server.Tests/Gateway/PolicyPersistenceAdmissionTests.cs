using System.Security.Claims;
using System.Text.Json;
using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Acceptance;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Governance.Policy;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.ChatBot.Server.Tests.TrustedAuthority;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

// Shares the existing synthetic gateway fixture. No native persisted-end-state or A6 acceptance is claimed.
public sealed partial class CommandGatewayTests
{
    [Theory]
    [InlineData("submit", false)]
    [InlineData("submit", true)]
    [InlineData("approval", false)]
    [InlineData("approval", true)]
    [InlineData("thresholds", false)]
    [InlineData("thresholds", true)]
    [InlineData("escalation", false)]
    [InlineData("escalation", true)]
    [InlineData("notification-routing", false)]
    [InlineData("notification-routing", true)]
    [InlineData("mailbox-configuration", false)]
    [InlineData("mailbox-configuration", true)]
    [InlineData("retention-configuration", false)]
    [InlineData("retention-configuration", true)]
    [InlineData("data-class-inventory", false)]
    [InlineData("data-class-inventory", true)]
    public async Task SyntheticPolicyWritesMustDenyBothLegacyAndCanonicalAdmissionBeforeReservation(string commandKind, bool canonical)
    {
        object command = SyntheticPolicyCommand(commandKind);
        ChatBotCommandSubmission submission = Submission(SyntheticPolicyPrincipal(commandKind), command);
        if (canonical)
        {
            submission.Request.Concurrency = new CommandOperationConcurrency { ExpectedRevision = 0, DecisionSlotId = "synthetic-policy-slot" };
        }
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore reservations = new(new FixedClock());
        InMemoryOperationStatusStore status = new();
        RecordingAuthorizationFailureCounter counter = new();
        ChatBotGatewayResult result = await Gateway(dispatcher, auditWriter: audit, idempotencyStore: reservations, operationStatusStore: status, authorizationFailureCounter: counter)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        dispatcher.DispatchCount.ShouldBe(0);
        reservations.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        counter.RecordedTenants.ShouldBeEmpty();
        (await status.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
        string safe = JsonSerializer.Serialize(result);
        safe.ShouldNotContain("policy-snapshot-current");
        safe.ShouldNotContain("old-fingerprint");
    }

    [Theory]
    [InlineData("submit", false, false)]
    [InlineData("submit", true, false)]
    [InlineData("approval", false, false)]
    [InlineData("approval", true, false)]
    [InlineData("thresholds", false, false)]
    [InlineData("thresholds", true, false)]
    [InlineData("submit", true, true)]
    [InlineData("approval", true, true)]
    [InlineData("thresholds", true, true)]
    [InlineData("escalation", false, false)]
    [InlineData("escalation", true, false)]
    [InlineData("escalation", true, true)]
    [InlineData("notification-routing", false, false)]
    [InlineData("notification-routing", true, false)]
    [InlineData("notification-routing", true, true)]
    [InlineData("mailbox-configuration", false, false)]
    [InlineData("mailbox-configuration", true, false)]
    [InlineData("mailbox-configuration", true, true)]
    [InlineData("retention-configuration", false, false)]
    [InlineData("retention-configuration", true, false)]
    [InlineData("retention-configuration", true, true)]
    [InlineData("data-class-inventory", false, false)]
    [InlineData("data-class-inventory", true, false)]
    [InlineData("data-class-inventory", true, true)]
    public async Task SyntheticSdkPolicyWritesMustDenyEvenAnAuthenticSignedMarker(string commandKind, bool signed, bool workload)
    {
        FixedClock clock = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        RecordingAuthorizationFailureCounter counter = new();
        ChatBotCommandAdmissionPipeline pipeline = new(new ClaimsAuthenticationStage(), new ClaimsTenantBindingStage(), new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(), new PassThroughApprovalGate(), reservations, audit, new RecordingReplayIntentQueue(), new RecordingOperatorAlertSink(),
            new InMemoryOperationStatusStore(), clock, new CommandSubmissionLifecycleTransitionGuard(), new PermissiveSpineCommandAllowlist(),
            Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Authorizer(clock), authorizationFailureCounter: counter);
        ClaimsPrincipal principal = workload
            ? new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "workload:eventstore"), new("eventstore:workload", "eventstore"),
                new("eventstore:operation", "domain-service:process")], "EventStoreWorkload"))
            : SyntheticPolicyPrincipal(commandKind);
        HttpContextAccessor accessor = new() { HttpContext = new DefaultHttpContext { User = principal } };
        ChatBotRequestContextResolver resolver = new(accessor);
        DataProtectionChatBotAdmissionMarker marker = new(new EphemeralDataProtectionProvider());
        object command = SyntheticPolicyCommand(commandKind);
        string commandType = command.GetType().Name;
        JsonElement payload = JsonSerializer.SerializeToElement(command);
        string actor = workload ? "synthetic-actor" : resolver.ResolveCurrent()!.SubjectId;
        Dictionary<string, string> extensions = new() { ["actorId"] = actor, ["surfaceOrigin"] = "api" };
        if (workload)
        {
            extensions["actorType"] = "human";
            extensions[EventStoreGatewayVerifiedOrigin.ExtensionKey] = "chatbot";
            extensions[EventStoreGatewayVerifiedOrigin.ActorExtensionKey] = actor;
        }
        if (signed)
        {
            extensions[DataProtectionChatBotAdmissionMarker.ExtensionKey] = marker.Create("01ARZ3NDEKTSV4RRFFQ69G5FAY", BoundTenant, "policy-change-001", commandType,
                payload, CorrelationId, actor, "api", null);
        }
        CommandEnvelope envelope = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", BoundTenant, "chatbot", "policy-change-001", commandType,
            JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, actor, extensions);
        marker.IsValid(envelope).ShouldBe(signed);
        if (workload)
        {
            RecordingDispatcher dispatcher = new();
            using WebApplicationFactory<Program> factory = SyntheticPolicyHost(principal, clock, audit, reservations, dispatcher, marker, counter);
            using HttpClient client = factory.CreateClient();
            using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(envelope, null), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            DomainServiceWireResult wire = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
            wire.IsRejection.ShouldBeTrue();
            wire.Events.ShouldHaveSingleItem().EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
            dispatcher.DispatchCount.ShouldBe(0);
        }
        else
        {
            ChatBotDomainServiceAdmissionStage stage = new(pipeline, reservations, marker, resolver);
            DomainServiceAdmissionResult result = await stage.EvaluateAsync(new(new DomainServiceRequest(envelope, null)), TestContext.Current.CancellationToken);
            result.IsAccepted.ShouldBeFalse();
            result.RejectionEvents.ShouldHaveSingleItem().ShouldBeOfType<ChatBotDomainServiceAdmissionRejected>()
                .ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        }
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        audit.Envelopes.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        counter.RecordedTenants.ShouldBeEmpty();
    }

    /// <summary>Pins the SDK policy gate before payload parsing with synthetic malformed input.</summary>
    /// <param name="commandKind">The gated policy command fixture.</param>
    [Theory]
    [InlineData("submit")]
    [InlineData("approval")]
    [InlineData("thresholds")]
    [InlineData("escalation")]
    [InlineData("notification-routing")]
    [InlineData("mailbox-configuration")]
    [InlineData("retention-configuration")]
    [InlineData("data-class-inventory")]
    public async Task SyntheticSdkMalformedPolicyPayloadMustRetainDependencyDenialBeforeParsing(string commandKind)
    {
        FixedClock clock = new();
        RecordingAuditWriter audit = new();
        RecordingAuthorizationFailureCounter counter = new();
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        string commandType = SyntheticPolicyCommand(commandKind).GetType().Name;
        CommandEnvelope envelope = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", BoundTenant, "chatbot", "policy-change-001", commandType,
            "{ \"policySnapshotId\": \"restricted/policy-secret\" "u8.ToArray(), CorrelationId, null, ActorId,
            new Dictionary<string, string> { ["actorId"] = ActorId, ["surfaceOrigin"] = "api" });
        using WebApplicationFactory<Program> factory = SyntheticPolicyHost(SyntheticPolicyPrincipal(commandKind), clock, audit, reservations, dispatcher,
            authorizationFailureCounter: counter);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(envelope, null), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        DomainServiceWireResult wire = JsonSerializer.Deserialize<DomainServiceWireResult>(body, Program.QueryJsonOptions).ShouldNotBeNull();
        wire.IsRejection.ShouldBeTrue();
        DomainServiceWireEvent rejected = wire.Events.ShouldHaveSingleItem();
        rejected.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        JsonSerializer.Deserialize<ChatBotDomainServiceAdmissionRejected>(rejected.Payload)!.ReasonCode
            .ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ShouldBe(new ChatBotAuthorizationFailureAuditFact(BoundTenant, ActorId,
            commandType, ChatBotRefusalReasonCodes.PolicySnapshotUnavailable, CorrelationId, null, "api"));
        counter.RecordedTenants.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        dispatcher.DispatchCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        IOperationStatusStore status = factory.Services.GetRequiredService<IOperationStatusStore>();
        (await status.TryGetAsync(BoundTenant, envelope.MessageId, TestContext.Current.CancellationToken)).ShouldBeNull();
        body.ShouldNotContain("restricted/policy-secret");
        JsonSerializer.Serialize(audit.AuthorizationFailures).ShouldNotContain("restricted/policy-secret");
    }

    [Theory]
    [InlineData("submit", false)]
    [InlineData("submit", true)]
    [InlineData("approval", false)]
    [InlineData("approval", true)]
    [InlineData("thresholds", false)]
    [InlineData("thresholds", true)]
    [InlineData("escalation", false)]
    [InlineData("escalation", true)]
    [InlineData("notification-routing", false)]
    [InlineData("notification-routing", true)]
    [InlineData("mailbox-configuration", false)]
    [InlineData("mailbox-configuration", true)]
    [InlineData("retention-configuration", false)]
    [InlineData("retention-configuration", true)]
    [InlineData("data-class-inventory", false)]
    [InlineData("data-class-inventory", true)]
    public async Task SyntheticHttpPolicyTransportsMustReturnSafeDependencyDenial(string commandKind, bool canonical)
    {
        FixedClock clock = new();
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        object command = SyntheticPolicyCommand(commandKind);
        using WebApplicationFactory<Program> factory = SyntheticPolicyHost(SyntheticPolicyPrincipal(commandKind), clock, audit, reservations, dispatcher);
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object> request = new()
        {
            ["commandId"] = "01ARZ3NDEKTSV4RRFFQ69G5FAY", ["commandType"] = command.GetType().Name,
            ["command"] = command, ["requestSchemaVersion"] = "v1",
        };
        if (canonical) { request["concurrency"] = new { expectedRevision = 0, decisionSlotId = "synthetic-policy-slot" }; }
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        ProblemDetails problem = Newtonsoft.Json.JsonConvert.DeserializeObject<ProblemDetails>(body).ShouldNotBeNull();
        problem.Code.ShouldBe(ChatBotMessageCodes.DependencyDegraded);
        problem.Retryable.ShouldBeTrue();
        problem.ClientAction.ShouldBe(ProblemDetailsClientAction.RetryLater);
        body.ShouldNotContain("policy-snapshot-current");
        dispatcher.DispatchCount.ShouldBe(0);
        reservations.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
    }

    [Fact]
    public async Task SyntheticPolicyDenialMustPreserveAuthenticationPrecedence()
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        RecordingAuthorizationFailureCounter counter = new();
        ChatBotGatewayResult unauthenticated = await Gateway(dispatcher, auditWriter: audit, authorizationFailureCounter: counter)
            .SubmitAsync(Submission(new ClaimsPrincipal(), TenantPolicyChangeCommand()), TestContext.Current.CancellationToken);
        unauthenticated.IsAccepted.ShouldBeFalse();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        counter.RecordedTenants.ShouldHaveSingleItem().ShouldBe("unavailable");
        dispatcher.DispatchCount.ShouldBe(0);
    }

    /// <summary>Preserves authorization denials before the unavailable policy-persistence guard.</summary>
    [Theory]
    [InlineData("submit", false)]
    [InlineData("submit", true)]
    [InlineData("approval", false)]
    [InlineData("approval", true)]
    [InlineData("thresholds", false)]
    [InlineData("thresholds", true)]
    [InlineData("escalation", false)]
    [InlineData("escalation", true)]
    [InlineData("notification-routing", false)]
    [InlineData("notification-routing", true)]
    [InlineData("mailbox-configuration", false)]
    [InlineData("mailbox-configuration", true)]
    [InlineData("retention-configuration", false)]
    [InlineData("retention-configuration", true)]
    [InlineData("data-class-inventory", false)]
    [InlineData("data-class-inventory", true)]
    public async Task SyntheticPolicyDenialMustPreserveAuthorizationPrecedence(string commandKind, bool canonical)
    {
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        RecordingAuthorizationFailureCounter counter = new();
        InMemoryCoarseIdempotencyStore reservations = new(new FixedClock());
        InMemoryOperationStatusStore status = new();
        ChatBotCommandSubmission submission = Submission(SyntheticPolicyPrincipal(commandKind), SyntheticPolicyCommand(commandKind));
        if (canonical)
        {
            submission.Request.Concurrency = new CommandOperationConcurrency { ExpectedRevision = 0, DecisionSlotId = "synthetic-policy-slot" };
        }

        ChatBotGatewayResult result = await Gateway(dispatcher,
            authorizationStage: new DenyingAuthorizationStage(ChatBotAuthorizationReasonCodes.AuthorizationDenied),
            auditWriter: audit, idempotencyStore: reservations, operationStatusStore: status, authorizationFailureCounter: counter)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Code.ShouldBe(ChatBotMessageCodes.AuthorizationDenied);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.RecordedTenants.ShouldHaveSingleItem().ShouldBe(BoundTenant);
        audit.Envelopes.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        dispatcher.DispatchCount.ShouldBe(0);
        (await status.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>Exercises real policy evaluation through admission with synthetic unresolved evidence.</summary>
    [Theory]
    [InlineData("missing", false)]
    [InlineData("missing", true)]
    [InlineData("stale", false)]
    [InlineData("stale", true)]
    [InlineData("foreign", false)]
    [InlineData("foreign", true)]
    public async Task SyntheticUnresolvedPolicyMustBlockAdmissionBeforeReservationOrDispatch(string evidenceState, bool canonical)
    {
        TenantAiPolicySnapshot? snapshot = evidenceState == "missing" ? null : new(
            evidenceState == "foreign" ? "synthetic-foreign-tenant" : BoundTenant,
            7, "policy-snap-001", LowRiskAllowed: true, "read-only", ["summarize-visible-context"],
            IsFresh: evidenceState != "stale", IsValid: true);
        SyntheticAdmissionPolicySnapshotProvider provider = new(snapshot);
        RecordingDispatcher dispatcher = new();
        RecordingAuditWriter audit = new();
        RecordingAuthorizationFailureCounter counter = new();
        InMemoryCoarseIdempotencyStore reservations = new(new FixedClock());
        InMemoryOperationStatusStore status = new();
        ChatBotCommandSubmission submission = Submission(Principal(BoundTenant), LowRiskExecutionCommand());
        if (canonical)
        {
            submission.Request.Concurrency = new CommandOperationConcurrency { ExpectedRevision = 0, DecisionSlotId = "synthetic-ai-policy-slot" };
        }

        ChatBotGatewayResult result = await Gateway(dispatcher, auditWriter: audit, idempotencyStore: reservations, operationStatusStore: status,
            riskClassifier: new SyntheticLowRiskPolicyRiskClassifier(),
            approvalGate: new AiActionApprovalGate(new DefaultAiActionPolicyEvaluator(provider)),
            authorizationFailureCounter: counter)
            .SubmitAsync(submission, TestContext.Current.CancellationToken);

        provider.ReadCount.ShouldBe(1);
        result.IsAccepted.ShouldBeFalse();
        result.Problem.ShouldNotBeNull().Code.ShouldBe(ChatBotMessageCodes.RefusalBlockedAction);
        result.Problem.Status.ShouldBe(StatusCodes.Status403Forbidden);
        result.Problem.Retryable.ShouldBeFalse();
        dispatcher.DispatchCount.ShouldBe(0);
        reservations.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldBeEmpty();
        counter.RecordedTenants.ShouldBeEmpty();
        (await status.TryGetAsync(BoundTenant, submission.Request.CommandId, TestContext.Current.CancellationToken)).ShouldBeNull();
        string safe = JsonSerializer.Serialize(result);
        safe.ShouldNotContain("synthetic-foreign-tenant");
        safe.ShouldNotContain("policy-snap-001");
        safe.ShouldNotContain("context-package-001");
    }

    [Theory]
    [InlineData("missing-tenant", false, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("missing-tenant", true, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("conflicting-tenant", false, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("conflicting-tenant", true, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("mismatched-tenant", false, ChatBotAuthorizationReasonCodes.TenantMismatch)]
    [InlineData("mismatched-tenant", true, ChatBotAuthorizationReasonCodes.TenantMismatch)]
    [InlineData("missing-subject", false, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("missing-subject", true, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("conflicting-subject", false, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("conflicting-subject", true, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("mismatched-user", false, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    [InlineData("mismatched-user", true, ChatBotAuthorizationReasonCodes.AuthenticationDenied)]
    public async Task SyntheticSdkPolicyBindingFailuresMustRetainSafeReasonsWithoutEffects(string binding, bool signed, string reason)
    {
        FixedClock clock = new();
        RecordingAuditWriter audit = new();
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        List<Claim> claims = [new("sub", "synthetic-actor"), new("eventstore:tenant", BoundTenant)];
        if (binding == "missing-tenant") { claims.RemoveAll(static claim => claim.Type == "eventstore:tenant"); }
        if (binding == "conflicting-tenant") { claims.Add(new("eventstore:tenant", "synthetic-foreign")); }
        if (binding == "missing-subject") { claims.RemoveAll(static claim => claim.Type == "sub"); }
        if (binding == "conflicting-subject") { claims.Add(new("sub", "synthetic-other")); }
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, "synthetic-authenticated"));
        object command = SyntheticPolicyCommand("approval");
        JsonElement payload = JsonSerializer.SerializeToElement(command);
        string tenant = binding == "mismatched-tenant" ? "synthetic-foreign" : BoundTenant;
        string user = binding == "mismatched-user" ? "synthetic-other" : "synthetic-actor";
        DataProtectionChatBotAdmissionMarker marker = new(new EphemeralDataProtectionProvider());
        Dictionary<string, string> extensions = new() { ["actorId"] = user, ["surfaceOrigin"] = "api" };
        if (signed)
        {
            extensions[DataProtectionChatBotAdmissionMarker.ExtensionKey] = marker.Create("01ARZ3NDEKTSV4RRFFQ69G5FAY", tenant,
                "policy-change-001", command.GetType().Name, payload, CorrelationId, user, "api", null);
        }
        CommandEnvelope envelope = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", tenant, "chatbot", "policy-change-001", command.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, user, extensions);
        marker.IsValid(envelope).ShouldBe(signed);
        using WebApplicationFactory<Program> factory = SyntheticPolicyHost(principal, clock, audit, reservations, dispatcher, marker);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(envelope, null), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult wire = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
        wire.IsRejection.ShouldBeTrue();
        DomainServiceWireEvent rejected = wire.Events.ShouldHaveSingleItem();
        rejected.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        JsonSerializer.Deserialize<ChatBotDomainServiceAdmissionRejected>(rejected.Payload)!.ReasonCode.ShouldBe(reason);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(reason);
        audit.Envelopes.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        dispatcher.DispatchCount.ShouldBe(0);
    }

    /// <summary>Denies untrusted signed-workload fallback metadata before any policy admission effect.</summary>
    [Theory]
    [InlineData("tampered-marker", false)]
    [InlineData("missing-gateway-origin", true)]
    [InlineData("conflicting-gateway-origin", true)]
    [InlineData("missing-bound-actor", true)]
    [InlineData("conflicting-bound-actor", true)]
    [InlineData("missing-actor-id", false)]
    [InlineData("conflicting-actor-id", true)]
    [InlineData("missing-actor-type", true)]
    [InlineData("conflicting-actor-type", true)]
    [InlineData("missing-surface-origin", false)]
    [InlineData("conflicting-surface-origin", true)]
    public async Task SyntheticSignedWorkloadPolicyFallbackMustRejectTamperingAndAmbiguousBindings(string failure, bool validMarker)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failure);
        FixedClock clock = new();
        RecordingAuditWriter audit = new();
        RecordingDispatcher dispatcher = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        ClaimsPrincipal principal = new(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "workload:eventstore"),
            new("eventstore:workload", "eventstore"), new("eventstore:operation", "domain-service:process")], "EventStoreWorkload"));
        object command = SyntheticPolicyCommand("approval");
        JsonElement payload = JsonSerializer.SerializeToElement(command);
        DataProtectionChatBotAdmissionMarker marker = new(new EphemeralDataProtectionProvider());
        string signedMarker = marker.Create("01ARZ3NDEKTSV4RRFFQ69G5FAY", BoundTenant, "policy-change-001", command.GetType().Name,
            payload, CorrelationId, "synthetic-actor", "api", null);
        Dictionary<string, string> extensions = new()
        {
            ["actorId"] = "synthetic-actor", ["actorType"] = "human", ["surfaceOrigin"] = "api",
            [EventStoreGatewayVerifiedOrigin.ExtensionKey] = "chatbot", [EventStoreGatewayVerifiedOrigin.ActorExtensionKey] = "synthetic-actor",
            [DataProtectionChatBotAdmissionMarker.ExtensionKey] = failure == "tampered-marker" ? "invalid-" + signedMarker : signedMarker,
            ["taskId"] = "restricted/policy-secret",
        };
        if (failure != "tampered-marker")
        {
            string key = failure[(failure.IndexOf('-') + 1)..] switch
            {
                "gateway-origin" => EventStoreGatewayVerifiedOrigin.ExtensionKey,
                "bound-actor" => EventStoreGatewayVerifiedOrigin.ActorExtensionKey,
                "actor-id" => "actorId",
                "actor-type" => "actorType",
                "surface-origin" => "surfaceOrigin",
                _ => throw new ArgumentOutOfRangeException(nameof(failure)),
            };
            if (failure.StartsWith("missing-", StringComparison.Ordinal)) { extensions.Remove(key); }
            else { extensions.Add(key.ToUpperInvariant(), "restricted/policy-secret"); }
        }
        CommandEnvelope envelope = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", BoundTenant, "chatbot", "policy-change-001", command.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(payload), CorrelationId, null, "synthetic-actor", extensions);
        marker.IsValid(envelope).ShouldBe(validMarker);
        using WebApplicationFactory<Program> factory = SyntheticPolicyHost(principal, clock, audit, reservations, dispatcher, marker);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(envelope, null), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult wire = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
        wire.IsRejection.ShouldBeTrue();
        DomainServiceWireEvent rejected = wire.Events.ShouldHaveSingleItem();
        rejected.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
        JsonSerializer.Deserialize<ChatBotDomainServiceAdmissionRejected>(rejected.Payload)!.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthenticationDenied);
        audit.AuthorizationFailures.ShouldHaveSingleItem().ShouldBe(new ChatBotAuthorizationFailureAuditFact("unavailable", "anonymous",
            command.GetType().Name, ChatBotAuthorizationReasonCodes.AuthenticationDenied, CorrelationId, null, "api"));
        audit.Envelopes.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        dispatcher.DispatchCount.ShouldBe(0);
        string safe = JsonSerializer.Serialize(new { wire, audit.AuthorizationFailures });
        safe.ShouldNotContain("restricted/policy-secret");
        safe.ShouldNotContain(signedMarker);
        safe.ShouldNotContain("policy-snapshot-current");
    }

    /// <summary>Checks the real host keeps production policy unavailable and synthetic models unregistered.</summary>
    [Fact]
    public async Task SyntheticPolicyModelsMustRemainUnregisteredAndProductionProviderUnavailableWhenAcceptanceIsDisabled()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(Story132AcceptanceServiceCollectionExtensions.EnabledConfigurationKey, "false");
            builder.ConfigureServices(services =>
            {
                services.ShouldNotContain(static descriptor => descriptor.ServiceType == typeof(ImmutableTenantPolicySnapshot) ||
                    descriptor.ServiceType == typeof(TenantPolicySnapshotHistory) || descriptor.ServiceType == typeof(TenantPolicySnapshotResolver));
                foreach (ServiceDescriptor service in services.Where(static service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToArray())
                {
                    services.Remove(service);
                }
            });
        });
        factory.Services.GetRequiredService<IConfiguration>().GetValue<bool>(Story132AcceptanceServiceCollectionExtensions.EnabledConfigurationKey).ShouldBeFalse();
        ITenantAiPolicySnapshotProvider provider = factory.Services.GetServices<ITenantAiPolicySnapshotProvider>().ShouldHaveSingleItem();
        provider.ShouldBeOfType<UnavailableTenantAiPolicySnapshotProvider>();
        (await provider.TryGetAsync(BoundTenant, "project-001", "synthetic-policy", TestContext.Current.CancellationToken)).ShouldBeNull();
        factory.Services.GetService<ImmutableTenantPolicySnapshot>().ShouldBeNull();
        factory.Services.GetService<TenantPolicySnapshotHistory>().ShouldBeNull();
        factory.Services.GetService(typeof(TenantPolicySnapshotResolver)).ShouldBeNull();
    }

    private static WebApplicationFactory<Program> SyntheticPolicyHost(ClaimsPrincipal principal, ISystemClock clock, IAuditWriter audit,
        IIdempotencyStore reservations, ICommandDispatcher dispatcher, IChatBotAdmissionMarker? marker = null,
        IAuthorizationFailureCounter? authorizationFailureCounter = null)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers(services);
            services.AddSingleton(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(new Hexalith.ChatBot.Tests.TrustedAuthority.SyntheticOwnerAuthorityProvider(clock));
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(_ => principal));
            services.AddSingleton<IAuthorizationStage>(new PassThroughAuthorizationStage());
            services.AddSingleton(dispatcher);
            services.AddSingleton(audit);
            services.AddSingleton(reservations);
            services.AddSingleton<IOperationStatusStore>(new InMemoryOperationStatusStore());
            if (marker is not null) { services.AddSingleton(marker); }
            if (authorizationFailureCounter is not null) { services.AddSingleton(authorizationFailureCounter); }
            foreach (ServiceDescriptor service in services.Where(static service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToArray())
            {
                services.Remove(service);
            }
        }));

    private static ClaimsPrincipal SyntheticPolicyPrincipal(string commandKind)
        => AdminPrincipal(commandKind switch
        {
            "mailbox-configuration" => "mailbox-admin",
            "retention-configuration" or "data-class-inventory" => "compliance-admin",
            _ => "policy-admin",
        });

    private static object SyntheticPolicyCommand(string commandKind)
        => commandKind switch
        {
            "submit" => TenantPolicyChangeCommand(),
            "approval" => new Hexalith.ChatBot.Contracts.Commands.ApproveTenantPolicyChange("policy-change-001", "policy-snapshot-current", "policy-snapshot-proposed", 8,
                [TenantPolicyKnobIds.AssociationTHigh], "synthetic", "admin-requester", "admin-approver", TenantPolicySchemaVersions.M0, CorrelationId),
            "thresholds" => new Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds("policy-change-001", 0.90, 0.60,
                "synthetic-v1", "synthetic-evaluation", FixedClock.FixedUtcNow),
            "escalation" => EscalationPolicyChangeCommand(),
            "notification-routing" => NotificationRoutingChangeCommand(),
            "mailbox-configuration" => MailboxConfigurationChangeCommand(),
            "retention-configuration" => RetentionConfigurationChangeCommand(),
            "data-class-inventory" => DataClassInventoryChangeCommand(),
            _ => throw new ArgumentOutOfRangeException(nameof(commandKind)),
        };

}
