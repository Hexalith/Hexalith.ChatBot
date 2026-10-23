using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Operations;
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
    [InlineData("requester-authority-drift", false)]
    [InlineData("machine-approver", false)]
    [InlineData("non-human-approver", false)]
    [InlineData("approver-tenant", false)]
    [InlineData("approver-subject", false)]
    [InlineData("approver-not-current", false)]
    [InlineData("approver-project", false)]
    [InlineData("future-decision", false)]
    [InlineData("expiry-before-decision", false)]
    [InlineData("blank-scope", false)]
    [InlineData("command-request-policy", false)]
    public async Task IndependentHumanApprovalMustMatchPresenceLifetimeAndEveryScopeBinding(string state, bool valid)
    {
        SyntheticHumanApprovalFixture fixture = SyntheticHumanApprovalFixture.Create();
        ChatBotHumanApprovalEvidence proof = fixture.Source.ProfileEvidence!.Approval!;
        if (state == "command-request-policy")
        {
            ExecuteApprovedAIAction command = (ExecuteApprovedAIAction)fixture.Context.Submission.Request.Command;
            fixture.Context.Submission.Request.Command = command with { PolicySnapshotId = "drift" };
            fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
            {
                Policy = fixture.Source.ProfileEvidence.Policy! with { PolicySnapshotId = "drift" },
            };
        }
        fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
        {
            Approval = state switch
            {
                "missing" => null,
                "self" => proof with { Decision = proof.Decision with { DecisionActorId = "requester" }, Approver = Approver(proof, "requester") },
                "service" => proof with { Decision = proof.Decision with { DecisionActorType = "service" } },
                "presence" => proof with { PresenceReference = " " },
                "expired" => proof with { ExpiresAt = fixture.Source.UtcNow },
                "lifetime" => proof with { ExpiresAt = proof.Decision.DecidedAtUtc.AddMinutes(31) },
                "scope-drift" => proof with { CurrentScopeDigest = "drift" },
                "classifier-drift" => proof with { ClassifierVersion = "drift" },
                "classifier-v1" => proof with { ClassifierVersion = "chatbot.ai-action-risk-classifier.m0.v1" },
                "policy-drift" => proof with { Decision = proof.Decision with { PolicySnapshotId = "drift" } },
                "revision-drift" => proof with { Decision = proof.Decision with { SourceVersion = 3 } },
                "requester-authority-drift" => proof with { Request = proof.Request with { RiskInputTuple = proof.Request.RiskInputTuple.Replace("project-contributor", "tenant-admin", StringComparison.Ordinal) } },
                "machine-approver" => proof with { Approver = Approver(proof, "approver", actor: "service") },
                "non-human-approver" => proof with { Approver = Approver(proof, "approver", actor: "system") },
                "approver-tenant" => proof with { Approver = Approver(proof, "approver", tenant: "another-tenant") },
                "approver-subject" => proof with { Approver = Approver(proof, "another-subject") },
                "approver-not-current" => proof with { Approver = Approver(proof, "approver", expiredAt: fixture.Source.UtcNow.AddMinutes(-1)) },
                "approver-project" => proof with { Approver = Approver(proof, "approver", project: "another-project") },
                "future-decision" => proof with { Decision = proof.Decision with { DecidedAtUtc = fixture.Source.UtcNow.AddMinutes(1) } },
                "expiry-before-decision" => proof with { ExpiresAt = proof.Decision.DecidedAtUtc },
                "blank-scope" => proof with { ScopeDigest = " ", CurrentScopeDigest = " " },
                _ => proof,
            },
        };
        ChatBotAdmissionEvidenceValidator validator = new(fixture.Source, fixture.Source);
        string? result = await validator.ValidateProfileAsync(fixture.Context, TestContext.Current.CancellationToken);
        if (valid) { result.ShouldBeNull(); }
        else { result.ShouldBe(ChatBotRefusalReasonCodes.ApprovalStateInvalid); }
        (await validator.ValidateAtomicParticipationAsync(fixture.Context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }
    /// <summary>Sensitive approvals may last at most ten minutes; reversible writes may last at most thirty.</summary>
    [Theory]
    [InlineData(AiActionRiskActionClass.ModifiesState, 30, true)]
    [InlineData(AiActionRiskActionClass.ModifiesState, 31, false)]
    [InlineData(AiActionRiskActionClass.ExposesFiles, 10, true)]
    [InlineData(AiActionRiskActionClass.ExposesFiles, 11, false)]
    [InlineData(AiActionRiskActionClass.SendsExternal, 10, true)]
    [InlineData(AiActionRiskActionClass.SendsExternal, 11, false)]
    [InlineData(AiActionRiskActionClass.CreatesTasks, 10, true)]
    [InlineData(AiActionRiskActionClass.CreatesTasks, 11, false)]
    [InlineData(AiActionRiskActionClass.InvokesTools, 10, true)]
    [InlineData(AiActionRiskActionClass.InvokesTools, 11, false)]
    [InlineData(AiActionRiskActionClass.ActsOnBehalf, 10, true)]
    [InlineData(AiActionRiskActionClass.ActsOnBehalf, 11, false)]
    public async Task ApprovalLifetimeMustRespectTheExactEffectClassMaximum(AiActionRiskActionClass effect, int minutes, bool valid)
    {
        SyntheticHumanApprovalFixture fixture = SyntheticHumanApprovalFixture.Create();
        ChatBotHumanApprovalEvidence proof = fixture.Source.ProfileEvidence!.Approval!;
        AiActionRiskActionClass[] effects = effect == AiActionRiskActionClass.ModifiesState
            ? [effect] : [AiActionRiskActionClass.ModifiesState, effect];
        var classification = fixture.Context.RiskClassification!.Record!;
        classification = classification with
        {
            RiskActionClasses = effects,
            InputTuple = classification.InputTuple with { ActionClasses = effects },
        };
        fixture.Context.SetRiskClassification(ChatBotRiskClassification.Classified(classification));
        proof = proof with
        {
            Request = proof.Request with
            {
                AiRiskActionClasses = effects.Select(GovernedOperationAggregate.AiRiskActionClassToken).ToArray(),
                RiskInputTuple = GovernedOperationAggregate.RiskInputTupleRef(classification.InputTuple),
            },
        };
        fixture.Source.ProfileEvidence = fixture.Source.ProfileEvidence with
        {
            Approval = proof with { ExpiresAt = proof.Decision.DecidedAtUtc.AddMinutes(minutes) },
        };
        ChatBotAdmissionEvidenceValidator validator = new(fixture.Source, fixture.Source);
        (await validator.ValidateClassificationAsync(fixture.Context, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await validator.ValidateProfileAsync(fixture.Context, TestContext.Current.CancellationToken))
            .ShouldBe(valid ? null : ChatBotRefusalReasonCodes.ApprovalStateInvalid);
        fixture.Context.Idempotency.ShouldBeNull();
        fixture.Context.AdmissionEnvelope.ShouldBeNull();
    }

    /// <summary>Creates independently varied synthetic approver identity and current-authority bounds.</summary>
    private static ChatBotAuthorityPrincipal Approver(ChatBotHumanApprovalEvidence proof, string subject,
        string actor = "human", string tenant = "synthetic-tenant", string project = "project", DateTimeOffset? expiredAt = null)
    {
        ChatBotRequestContext original = proof.Approver.Context;
        ChatBotRequestContext context = new(subject, tenant, actor, actor == "service" ? subject : null, original.Origin, original.Principal);
        ChatBotOwnerAuthorityRequest request = new("ChatBot", subject, tenant, project, nameof(ExecuteApprovedAIAction),
            "operation", actor, original.Origin, true);
        var evidence = expiredAt is { } expiry
            ? new[] { (new ChatBotOwnerAuthorityEvidence(request, "synthetic:approval", "1", expiry.AddMinutes(-1),
                expiry.AddMinutes(-1), expiry, true, false), expiry.AddMinutes(-1)) }
            : [];
        return new(context, null, [project], validatedEvidence: evidence);
    }

}
