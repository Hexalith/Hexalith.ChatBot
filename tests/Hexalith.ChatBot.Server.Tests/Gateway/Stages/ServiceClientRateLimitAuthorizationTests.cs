using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using ServiceClientRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.ServiceClientRateLimitWindow;
using SubmitServiceClientRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitServiceClientRateLimit;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class ServiceClientRateLimitAuthorizationTests
{
    [Fact]
    public async Task RateLimitShouldRequireSingleHumanTenantAdminWithNoApprover()
    {
        ParticipantAuthorizationStage stage = new();

        // A single authorized human tenant-admin applies it — no approver needed (service-client governance is a
        // TenantAdmin responsibility; there is no service-client AdminScope and no mailbox scope for it).
        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(RateLimitSubmit()),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        // A non-tenant-admin scope (mailbox/policy/compliance/operations-only) is denied; service clients and AI
        // actors are denied even with tenant-admin-looking claims.
        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("human", "operations-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
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

        foreach (SubmitServiceClientRateLimit invalid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = ServiceClientRateLimitBounds.Maximum + 1 },
                     RateLimitSubmit() with { NewBudget = -1 },
                     RateLimitSubmit() with { OldBudget = -1 },
                     RateLimitSubmit() with { SourceVersion = -1 },
                     RateLimitSubmit() with { SchemaVersion = "service-client-rate-limit-schema.custom" },
                     RateLimitSubmit() with { ReasonCode = "unsafe reason" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "tenant-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // A boundary budget (exactly the maximum, and the minimum of zero) is accepted.
        foreach (SubmitServiceClientRateLimit valid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = ServiceClientRateLimitBounds.Maximum },
                     RateLimitSubmit() with { NewBudget = ServiceClientRateLimitBounds.Minimum },
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(valid),
                Actor("human", "tenant-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }
    }

    private static SubmitServiceClientRateLimit RateLimitSubmit()
        => new(
            "service-client-rate-limit-001",
            "service-client:cli-automation-client",
            "service-client-noisy-automation",
            "policy-snapshot:tenant-admin:v1",
            OldBudget: 0,
            NewBudget: 2000,
            ServiceClientRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            ServiceClientRateLimitSchemaVersions.V1,
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
