using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using MailboxRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.MailboxRateLimitWindow;
using SubmitMailboxSourceRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitMailboxSourceRateLimit;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class MailboxSourceRateLimitAuthorizationTests
{
    [Fact]
    public async Task RateLimitShouldRequireHumanMailboxScopeWithNoApprover()
    {
        ParticipantAuthorizationStage stage = new();

        // A single authorized human mailbox-admin (or tenant-admin union) applies it — no approver needed.
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "mailbox-admin"),
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

        // Policy/compliance/operations-only scope is denied; service clients and AI actors are denied even with
        // tenant-admin-looking claims.
        foreach (ChatBotAuthenticatedActor deniedActor in new[]
                 {
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

        foreach (SubmitMailboxSourceRateLimit invalid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = MailboxRateLimitBounds.Maximum + 1 },
                     RateLimitSubmit() with { NewBudget = -1 },
                     RateLimitSubmit() with { OldBudget = -1 },
                     RateLimitSubmit() with { SourceVersion = -1 },
                     RateLimitSubmit() with { SchemaVersion = "mailbox-source-rate-limit-schema.custom" },
                     RateLimitSubmit() with { ReasonCode = "unsafe reason" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "mailbox-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // A boundary budget (exactly the maximum, and the minimum of zero) is accepted.
        foreach (SubmitMailboxSourceRateLimit valid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = MailboxRateLimitBounds.Maximum },
                     RateLimitSubmit() with { NewBudget = MailboxRateLimitBounds.Minimum },
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(valid),
                Actor("human", "mailbox-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }
    }

    private static SubmitMailboxSourceRateLimit RateLimitSubmit()
        => new(
            "mailbox-rate-limit-001",
            "mailbox-source:controlled-mailbox-001",
            "mailbox-source-noisy-intake",
            "policy-snapshot:mailbox:v1",
            OldBudget: 0,
            NewBudget: 200,
            MailboxRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            MailboxSourceRateLimitSchemaVersions.V1,
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
