using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies independent human approval evidence and scope drift containment.</summary>
public sealed class ChatBotHumanApprovalEvidenceTests
{
    /// <summary>Rejects independently mismatched approval identities, contradictory decisions and joint allowlist drift.</summary>
    [Theory]
    [InlineData("command-id")]
    [InlineData("request-id")]
    [InlineData("decision-id")]
    [InlineData("request-decision-id")]
    [InlineData("authority-denied")]
    [InlineData("disabled")]
    [InlineData("audit-unavailable")]
    [InlineData("joint-allowlist-drift")]
    public async Task ApprovalMustBindAllThreeIdentitiesAnEnabledAuthorizedDecisionAndTheCurrentAllowlist(string mismatch)
    {
        SyntheticHumanApprovalFixture fixture = SyntheticHumanApprovalFixture.Create();
        ChatBotHumanApprovalEvidence proof = fixture.Source.ProfileEvidence!.Approval!;
        ExecuteApprovedAIAction command = (ExecuteApprovedAIAction)fixture.Context.Submission.Request.Command;
        fixture.Context.Submission.Request.Command = mismatch switch
        {
            "command-id" => command with { ApprovalId = "another-approval" },
            "joint-allowlist-drift" => command with { CommandAllowlistVersion = "stale-allowlist" },
            _ => command,
        };
        fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
        {
            Approval = mismatch switch
            {
                "request-id" => proof with { Request = proof.Request with { ApprovalId = "another-approval" } },
                "decision-id" => proof with { Decision = proof.Decision with { ApprovalId = "another-approval" } },
                "request-decision-id" => proof with { Request = proof.Request with { ApprovalId = "another-approval" }, Decision = proof.Decision with { ApprovalId = "another-approval" } },
                "authority-denied" => proof with { Decision = proof.Decision with { AuthorityResult = "denied" } },
                "disabled" => proof with { Decision = proof.Decision with { DisabledReason = "disabled" } },
                "audit-unavailable" => proof with { Decision = proof.Decision with { AuditStatus = "unavailable" } },
                "joint-allowlist-drift" => proof with { Request = proof.Request with { CommandAllowlistVersion = "stale-allowlist" } },
                _ => proof,
            },
        };
        ChatBotAdmissionEvidenceValidator validator = new(fixture.Source, fixture.Source);

        (await validator.ValidateProfileAsync(fixture.Context, TestContext.Current.CancellationToken)).ShouldBe(mismatch == "joint-allowlist-drift"
            ? ChatBotRefusalReasonCodes.CommandNotAllowlisted : ChatBotRefusalReasonCodes.ApprovalStateInvalid);
        (await validator.ValidateAtomicParticipationAsync(fixture.Context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        AiActionCommandMetadataProvider.TryGet(command.CommandName)!.CommandAllowlistVersion.ShouldBe(command.CommandAllowlistVersion);
    }

    /// <summary>Approved classification and request/decision chronology must still match before evaluating the approval lifetime.</summary>
    [Theory]
    [InlineData("risk-class")]
    [InlineData("missing-effects")]
    [InlineData("different-effects")]
    [InlineData("extra-effects")]
    [InlineData("future-request")]
    [InlineData("request-after-decision")]
    [InlineData("missing-request-time")]
    [InlineData("missing-decision-time")]
    public async Task ApprovalMustBindCurrentRiskEffectsAndRequestDecisionChronology(string mutation)
    {
        SyntheticHumanApprovalFixture fixture = SyntheticHumanApprovalFixture.Create();
        ChatBotHumanApprovalEvidence proof = fixture.Source.ProfileEvidence!.Approval!;
        fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
        {
            Approval = mutation switch
            {
                "risk-class" => proof with { Request = proof.Request with { AiRiskClass = Contracts.Enums.AiActionRiskClass.LowRisk } },
                "missing-effects" => proof with { Request = proof.Request with { AiRiskActionClasses = [] } },
                "different-effects" => proof with { Request = proof.Request with { AiRiskActionClasses = ["sends-external"] } },
                "extra-effects" => proof with { Request = proof.Request with { AiRiskActionClasses = ["modifies-state", "exposes-files"] } },
                "future-request" => proof with { Request = proof.Request with { RequestedAtUtc = fixture.Source.UtcNow.AddMinutes(1) } },
                "request-after-decision" => proof with { Request = proof.Request with { RequestedAtUtc = proof.Decision.DecidedAtUtc.AddSeconds(1) } },
                "missing-request-time" => proof with { Request = proof.Request with { RequestedAtUtc = default } },
                _ => proof with { Decision = proof.Decision with { DecidedAtUtc = default } },
            },
        };
        (await new ChatBotAdmissionEvidenceValidator(fixture.Source, fixture.Source).ValidateProfileAsync(fixture.Context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid);
        fixture.Context.AdmissionEnvelope.ShouldBeNull();
        fixture.Context.Idempotency.ShouldBeNull();
    }

    /// <summary>Verifies independent Human Approval Must Match Presence Lifetime And Every Scope Binding.</summary>
    [Theory]
    [InlineData("current", true)]
    [InlineData("missing", false)]
    [InlineData("self", false)]
    [InlineData("service", false)]
    [InlineData("presence", false)]
    [InlineData("expired", false)]
    [InlineData("lifetime", false)]
    [InlineData("scope-drift", false)]
    [InlineData("classifier-drift", false)]
    [InlineData("classifier-v1", false)]
    [InlineData("policy-drift", false)]
    [InlineData("revision-drift", false)]
    public async Task IndependentHumanApprovalMustMatchPresenceLifetimeAndEveryScopeBinding(string state, bool valid)
    {
        SyntheticHumanApprovalFixture fixture = SyntheticHumanApprovalFixture.Create();
        ChatBotHumanApprovalEvidence proof = fixture.Source.ProfileEvidence!.Approval!;
        fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
        {
            Approval = state switch
            {
                "missing" => null,
                "self" => proof with { Decision = proof.Decision with { DecisionActorId = "requester" } },
                "service" => proof with { Decision = proof.Decision with { DecisionActorType = "service" } },
                "presence" => proof with { PresenceReference = "" },
                "expired" => proof with { ExpiresAt = fixture.Source.UtcNow },
                "lifetime" => proof with { ExpiresAt = proof.Decision.DecidedAtUtc.AddMinutes(31) },
                "scope-drift" => proof with { CurrentScopeDigest = "drift" },
                "classifier-drift" => proof with { ClassifierVersion = "drift" },
                "classifier-v1" => proof with { ClassifierVersion = "chatbot.ai-action-risk-classifier.m0.v1" },
                "policy-drift" => proof with { Decision = proof.Decision with { PolicySnapshotId = "drift" } },
                "revision-drift" => proof with { Decision = proof.Decision with { SourceVersion = 3 } },
                _ => proof,
            },
        };
        ChatBotAdmissionEvidenceValidator validator = new(fixture.Source, fixture.Source);
        string? result = await validator.ValidateProfileAsync(fixture.Context, TestContext.Current.CancellationToken);
        if (valid) { result.ShouldBeNull(); }
        else { result.ShouldNotBeNull(); }
        (await validator.ValidateAtomicParticipationAsync(fixture.Context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }
}
