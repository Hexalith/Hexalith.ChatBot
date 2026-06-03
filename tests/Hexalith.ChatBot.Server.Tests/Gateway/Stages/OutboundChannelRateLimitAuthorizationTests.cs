using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using OutboundChannelRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.OutboundChannelRateLimitWindow;
using SubmitOutboundChannelRateLimit = Hexalith.ChatBot.Contracts.Commands.SubmitOutboundChannelRateLimit;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

/// <summary>
/// Story 7.26: authorization of the single-actor <see cref="SubmitOutboundChannelRateLimit"/> command at the
/// admission stage. Enforcement of a configured budget lives at the outbound send seam (see
/// <c>AcceptedCommandDispatcherTests</c>), NOT here — so this fixture only proves the configure-command gating:
/// single human policy-admin (and tenant-admin via the FR75a union) allowed with no approver; non-policy human
/// scopes + service/AI actors denied; out-of-bounds / undeclared budgets rejected at the gateway. There is NO
/// self-lockout test (the subject is an outbound channel, not a governance command type — the 7.24/7.25 divergence).
/// </summary>
public sealed class OutboundChannelRateLimitAuthorizationTests
{
    private const string Tenant = "tenant-alpha";

    // The governed external-send path is identified by its safe AdapterRef token (a finite SafeStableIdentifier).
    private const string OutboundChannel = "adapter:mailbox-outbound";

    [Fact]
    public async Task RateLimitShouldRequireSingleHumanPolicyAdminWithNoApprover()
    {
        ParticipantAuthorizationStage stage = new();

        // Outbound-channel governance is the policy-admin's domain (the "policy administrator" persona maps to
        // AdminScope.Policy — there is no AdminScope.Security). A single authorized human policy-admin applies it — no
        // approver needed. A tenant-admin is also allowed via the FR75a scope union (not a relaxation).
        foreach (ChatBotAuthenticatedActor allowedActor in new[]
                 {
                     Actor("human", "policy-admin"),
                     Actor("human", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(RateLimitSubmit()),
                allowedActor,
                new ChatBotTenantBinding(Tenant),
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
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task RateLimitShouldRejectOutOfBoundsOrUndeclaredBudgetAtGateway()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (SubmitOutboundChannelRateLimit invalid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = OutboundChannelRateLimitBounds.Maximum + 1 },
                     RateLimitSubmit() with { NewBudget = -1 },
                     RateLimitSubmit() with { OldBudget = -1 },
                     RateLimitSubmit() with { SourceVersion = -1 },
                     RateLimitSubmit() with { SchemaVersion = "outbound-channel-rate-limit-schema.custom" },
                     RateLimitSubmit() with { ReasonCode = "unsafe reason" },
                     RateLimitSubmit() with { OutboundChannelRef = "unsafe ref" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        // Boundary budgets (exactly the maximum, and the minimum of zero) are accepted.
        foreach (SubmitOutboundChannelRateLimit valid in new[]
                 {
                     RateLimitSubmit() with { NewBudget = OutboundChannelRateLimitBounds.Maximum },
                     RateLimitSubmit() with { NewBudget = OutboundChannelRateLimitBounds.Minimum },
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(valid),
                Actor("human", "policy-admin"),
                new ChatBotTenantBinding(Tenant),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }
    }

    private static SubmitOutboundChannelRateLimit RateLimitSubmit()
        => new(
            "outbound-channel-rate-limit-001",
            OutboundChannel,
            "outbound-channel-noisy-sends",
            "policy-snapshot:policy-admin:v1",
            OldBudget: 0,
            NewBudget: 500,
            OutboundChannelRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            OutboundChannelRateLimitSchemaVersions.V1,
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
