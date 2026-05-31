using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AssociationThresholdAuthorizationTests
{
    [Fact]
    public async Task ThresholdMutationShouldRequireTenantAdminHumanActor()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult serviceDenied = await stage.AuthorizeAsync(
            Submission(),
            Actor("service", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        serviceDenied.IsAllowed.ShouldBeFalse();
        serviceDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);

        ChatBotAuthorizationResult adminHuman = await stage.AuthorizeAsync(
            Submission(),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        adminHuman.IsAllowed.ShouldBeTrue();
    }

    private static ChatBotCommandSubmission Submission()
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = nameof(Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds),
                Command = new Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds("association", 0.9, 0.6, "policy-v1", null, null),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);

    private static ChatBotAuthenticatedActor Actor(string actorType, string role)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
        return new ChatBotAuthenticatedActor("actor-alpha", principal);
    }
}
