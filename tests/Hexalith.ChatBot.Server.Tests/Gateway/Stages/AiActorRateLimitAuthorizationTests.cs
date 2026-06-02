using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using AiActorRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.AiActorRateLimitWindow;
using SubmitAiActorRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitAiActorRateLimit;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AiActorRateLimitAuthorizationTests
{
    [Fact]
    public async Task RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover()
    {
        ParticipantAuthorizationStage stage = new();

        // AI-action governance is the policy-admin's domain (Story 7.2): a single authorized human policy-admin
        // applies it — no approver needed. A tenant-admin is also allowed because it holds the FR75a scope union
        // (this is not a relaxation). This is the 7.18/7.19 divergence from the 7.17 service-client rate-limit
        // (which gated on tenant-admin because no finer service-client scope exists).
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(RateLimitSubmit()),
                allowedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        // Every non-policy human scope is denied, and service/AI actors are denied even with admin-looking claims.
        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("ai", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(RateLimitSubmit()),
                deniedActor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitAiActorRateLimit invalid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = AiActorRateLimitBounds.Maximum + 1 },
                     RateLimitSubmit() with { NewBudget = -1 },
                     RateLimitSubmit() with { OldBudget = -1 },
                     RateLimitSubmit() with { SourceVersion = -1 },
                     RateLimitSubmit() with { SchemaVersion = "ai-actor-rate-limit-schema.custom" },
                     RateLimitSubmit() with { ReasonCode = "unsafe reason" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // A boundary budget (exactly the maximum, and the minimum of zero) is accepted.
        foreach (SubmitAiActorRateLimit valid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = AiActorRateLimitBounds.Maximum },
                     RateLimitSubmit() with { NewBudget = AiActorRateLimitBounds.Minimum },
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(valid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }
    }

    private static SubmitAiActorRateLimit RateLimitSubmit()
        => new(
            "ai-actor-rate-limit-001",
            "ai-actor:gpt-mediation-actor",
            "ai-actor-noisy-proposals",
            "policy-snapshot:policy-admin:v1",
            OldBudget: 0,
            NewBudget: 200,
            AiActorRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            AiActorRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ChatBotCommandSubmission Submission(object command)
        => new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = command.GetType().Name,
                Command = command,
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
