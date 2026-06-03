using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.AiMediation;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Governance.AiMediation;

public sealed class ApprovedAiActionCommandAllowlistTests
{
    [Fact]
    public void M0AllowlistShouldContainOnlyAppendConversationMessageAndRemainTheDefault()
    {
        ApprovedAiActionCommandAllowlist allowlist = new();

        // The default (un-pinned) version is the M0 floor — v1 is opt-in via the version-pin knob (never widen first).
        allowlist.CurrentVersion.ShouldBe(AiActionCommandMetadataProvider.M0AllowlistVersion);
        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeTrue();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeFalse();
        allowlist.IsAllowed("Project.SendEmail", AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeFalse();
    }

    [Fact]
    public void M0SetMustNotBeMutatedByV1Work()
    {
        // AC3: the deployed M0 version's membership is exactly {Project.AppendConversationMessage} — adding v1 must
        // never silently mutate v0.
        ApprovedAiActionCommandAllowlist.ResolveMembers(AiActionCommandMetadataProvider.M0AllowlistVersion)
            .ShouldBe([AiActionCommandMetadataProvider.AppendConversationMessageCommandName], ignoreOrder: true);
    }

    [Fact]
    public void V1AllowlistShouldAddBreadthWithoutRelaxingTheVersionGate()
    {
        ApprovedAiActionCommandAllowlist allowlist = new();

        // v1 resolves the expected AI-invocable set (carry-over append + the read-only low-risk assistance command).
        ApprovedAiActionCommandAllowlist.ResolveMembers(AiActionCommandMetadataProvider.V1AllowlistVersion).ShouldBe(
            [
                AiActionCommandMetadataProvider.AppendConversationMessageCommandName,
                AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
            ],
            ignoreOrder: true);

        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, AiActionCommandMetadataProvider.V1AllowlistVersion).ShouldBeTrue();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, AiActionCommandMetadataProvider.V1AllowlistVersion).ShouldBeTrue();
    }

    [Fact]
    public void EveryV1MemberMustResolveNonNullMetadataWithAllFourRequiredFields()
    {
        // AC2/AC10: every allowlisted command MUST resolve non-null metadata carrying the four required v1 fields
        // (effect surface, authority class, default risk, idempotency contract). A member with missing metadata is a defect.
        foreach (string commandName in ApprovedAiActionCommandAllowlist.ResolveMembers(AiActionCommandMetadataProvider.V1AllowlistVersion))
        {
            AiActionCommandMetadata metadata = AiActionCommandMetadataProvider.TryGet(commandName).ShouldNotBeNull();
            metadata.CommandName.ShouldBe(commandName);

            // Field 1 — effect surface.
            metadata.EffectSurface.ShouldNotBeNullOrWhiteSpace();

            // Field 2 — authority class (finite safe token, never a free string).
            metadata.RequiredAuthorityClass.ShouldBeOneOf(AiActionAuthorityClass.ReadOnlyAssistant, AiActionAuthorityClass.DelegatedProjectContributor);

            // Field 3 — default risk: every member MUST carry a defined risk class (the four-field contract is
            // incomplete without it). The two known members carry their documented classifications.
            metadata.CommandDefaultRisk.ShouldBeOneOf(AiActionRiskClass.LowRisk, AiActionRiskClass.ApprovalRequired);
            Enum.IsDefined(metadata.CommandDefaultRisk).ShouldBeTrue();
            AiActionRiskClass expectedRisk = string.Equals(commandName, AiActionCommandMetadataProvider.AppendConversationMessageCommandName, StringComparison.Ordinal)
                ? AiActionRiskClass.ApprovalRequired
                : AiActionRiskClass.LowRisk;
            metadata.CommandDefaultRisk.ShouldBe(expectedRisk);

            // Field 4 — idempotency contract.
            metadata.IdempotencyContract.ShouldNotBeNull();
            metadata.IdempotencyContract.KeyTemplate.ShouldBe("tenant_id+command_name+command_input_hash+requester_id");
            metadata.IdempotencyContract.WindowSeconds.ShouldBe(60);
            metadata.IdempotencyContract.OnDuplicate.ShouldBe(AiActionIdempotencyResolution.ReturnPriorOutcome);
        }
    }

    [Fact]
    public void CommandRequestedAtTheWrongVersionMustBeRejected()
    {
        // Version-gating preserved: a v1 member requested at M0 is rejected; an M0 member requested at v1 is allowed
        // (it is a v1 member too), but an unknown version fails closed for any command.
        ApprovedAiActionCommandAllowlist allowlist = new();

        allowlist.IsAllowed(AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, AiActionCommandMetadataProvider.M0AllowlistVersion).ShouldBeFalse();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, "ai-action-command-allowlist.m99").ShouldBeFalse();
        allowlist.IsAllowed(AiActionCommandMetadataProvider.AppendConversationMessageCommandName, null).ShouldBeFalse();
        ApprovedAiActionCommandAllowlist.ResolveMembers("ai-action-command-allowlist.m99").ShouldBeEmpty();
    }

    [Fact]
    public void TenantDisallowedForAiCommandsAreExcludedFromV1()
    {
        // AC10: a command outside the curated AI-invocable subset (i.e. human-only / disallowed-for-AI such as an
        // outbound send or an admin-queue operation) is excluded from v1 and fails closed at v1.
        ApprovedAiActionCommandAllowlist allowlist = new();

        foreach (string excluded in new[] { "Project.SendEmail", "ExecuteApprovedOutboundDraft", "ExecuteAdminQueueOperation" })
        {
            allowlist.IsAllowed(excluded, AiActionCommandMetadataProvider.V1AllowlistVersion).ShouldBeFalse();
            ApprovedAiActionCommandAllowlist.ResolveMembers(AiActionCommandMetadataProvider.V1AllowlistVersion).ShouldNotContain(excluded);
        }
    }
}
