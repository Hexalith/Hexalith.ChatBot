using System.Security.Claims;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Operations;
using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Synthetic pure-validation fixture; none of these records establishes owner execution or gate closure.</summary>
/// <param name="Context">The synthetic operation context.</param>
/// <param name="Source">The synthetic qualification and owner evidence.</param>
internal sealed record SyntheticHumanApprovalFixture(ChatBotGatewayContext Context, SyntheticAdmissionEvidenceSource Source)
{
    /// <summary>Builds synthetic approval scope and an independent human decision.</summary>
    public static SyntheticHumanApprovalFixture Create()
    {
        SyntheticAdmissionEvidenceSource source = new();
        const string correlation = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        const string operation = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
        const string commandName = AiActionCommandMetadataProvider.AppendConversationMessageCommandName;
        const string allowlist = AiActionCommandMetadataProvider.M0AllowlistVersion;
        ClaimsPrincipal requester = Principal("requester");
        ExecuteApprovedAIAction command = new("project", "proposal", "approval", "intent", "source", "requester", commandName, allowlist,
            2, 1, correlation, "execution", "transition", ["evidence"], ["resource"], ["recipient"], PolicySnapshotId: "policy");
        ChatBotCommandSubmission submission = new(requester, new CommandSubmissionRequest
        {
            CommandId = operation, CommandType = nameof(ExecuteApprovedAIAction), Command = command,
            Concurrency = new() { ExpectedRevision = 5 },
        }, correlation, null);
        ChatBotGatewayContext context = new(submission, new("requester", requester), new("synthetic-tenant"));
        context.SelectAdmissionProfile(ChatBotAdmissionProfile.AiEffect);
        context.SetRiskClassification(ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            commandName, [], null, null, "project-contributor", "policy", null, null, null, "authorized", correlation))));
        AiActionApprovalRequested request = new("approval", "project", "proposal", "intent", "source", null, "requester", "human", source.UtcNow.AddMinutes(-2),
            commandName, allowlist, AiActionRiskClass.ApprovalRequired, ["modifies-state"], GovernedOperationAggregate.RiskInputTupleRef(context.RiskClassification!.Record!.InputTuple), "policy", "metadata_only", ["evidence"],
            [ApprovalEvidenceFreshness.Fresh], ["resource"], ["recipient"], "draft-only", "metadata_only", "metadata_only", 1, correlation);
        AiActionApprovalDecisionRecorded decision = new("approval", "project", "proposal", "source", ApprovalDecisionKind.Approve,
            "approver", "human", source.UtcNow.AddMinutes(-1), 1, "authorized", null, "metadata_only", "audit:synthetic", "available", "policy", "execute-approved-ai-action", 2, correlation);
        if (!ChatBotRequestContextResolver.TryResolve(Principal("approver"), ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? approverContext, out _)) { throw new InvalidOperationException("Synthetic approval identity failed to bind."); }
        ChatBotAuthorityPrincipal approver = new(approverContext!, null, ["project"]);
        ChatBotHumanApprovalEvidence approval = new(request, decision, approver, "synthetic:presence", source.UtcNow.AddMinutes(5),
            "synthetic:scope", "synthetic:scope", AiActionRiskClassifier.CurrentVersion);
        source.ProfileEvidence = new("synthetic-tenant", "requester", nameof(ExecuteApprovedAIAction), operation, ChatBotAdmissionProfile.AiEffect, 5, null,
            ChatBotCommandAdmissionCatalog.RequiredStages(ChatBotAdmissionProfile.AiEffect).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1), new("synthetic-tenant", 1, "policy", false, "project-conversation", [], true, true), Approval: approval);
        return new(context, source);
    }

    private static ClaimsPrincipal Principal(string subject)
        => new(new ClaimsIdentity([new Claim("sub", subject), new Claim("eventstore:tenant", "synthetic-tenant"), new Claim("actor_type", "human")], "synthetic"));
}
