using System.Security.Claims;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Shouldly;

using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Synthetic stage observations establish ordering only, and retain the real unavailable atomic gate.</summary>
internal sealed class SyntheticAdmissionSequenceFixture
{
    private readonly InMemoryCoarseIdempotencyStore _identities;
    private readonly InMemoryAuditWriter _audit = new();
    private readonly InMemoryAuditReplayIntentQueue _replay = new();
    private readonly InMemoryOperatorAlertSink _alerts = new();
    private readonly SyntheticAdmissionSequenceDispatcher _dispatcher = new();

    /// <summary>Ordered stage and store calls observed during this submission.</summary>
    public List<string> Calls { get; } = [];
    /// <summary>Synthetic stage observer retaining the unavailable production atomic boundary.</summary>
    public SyntheticObservedAdmissionEvidenceValidator Evidence { get; }
    /// <summary>The common production gateway under test.</summary>
    public CommandGateway Gateway { get; }
    /// <summary>The immutable transport submission bound to synthetic owner authority.</summary>
    public ChatBotCommandSubmission Submission { get; }

    /// <summary>Creates a synthetic sequence fixture without accepted atomic participation.</summary>
    public SyntheticAdmissionSequenceFixture(string commandType, ChatBotAdmissionProfile profile, string? failure = null)
    {
        SyntheticAdmissionEvidenceSource clock = new();
        _identities = new(clock);
        Evidence = new(Calls, profile, failure);
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        ((ClaimsIdentity)principal.Identity!).AddClaim(new(ParticipantAuthorizationStage.ProjectOwnerClaim, "project-alpha"));
        Submission = new(principal, new CommandSubmissionRequest
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", CommandType = commandType,
            Command = System.Text.Json.JsonSerializer.SerializeToElement(new { projectId = "project-alpha", noteId = "note-alpha", sendId = "send-alpha", draftId = "draft-alpha" }),
        }, "01ARZ3NDEKTSV4RRFFQ69G5FAW", null);
        Gateway = new(new ClaimsAuthenticationStage(), new ClaimsTenantBindingStage(), new PassThroughAuthorizationStage(),
            new SyntheticObservedAdmissionRiskClassifier(Calls), new SyntheticObservedAdmissionApprovalGate(Calls),
            TrustedAuthorityBoundaryProxy.Create<IIdempotencyStore>(_identities, method => Calls.Add("idempotency:" + method)),
            _audit, _replay, _alerts,
            TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(new InMemoryOperationStatusStore(), method => Calls.Add("status:" + method)),
            clock, new CommandSubmissionLifecycleTransitionGuard(), _dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new ChatBotSpineCommandAllowlist(),
            requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, new RegressionOwnerAuthorityProvider(clock, principal)),
            admissionEvidence: Evidence);
    }

    /// <summary>Asserts that reservations, dispatch, audit envelopes and deliveries remain absent.</summary>
    public void AssertNoEffects()
    {
        _identities.RecordCount.ShouldBe(0);
        _audit.Envelopes.ShouldBeEmpty();
        _audit.AuthorizationFailures.ShouldBeEmpty();
        _replay.Intents.ShouldBeEmpty();
        _alerts.Alerts.ShouldBeEmpty();
        _dispatcher.Calls.ShouldBe(0);
        Evidence.Context!.Idempotency.ShouldBeNull();
        Evidence.Context.ExternalEffectAttempted.ShouldBeFalse();
    }
}
