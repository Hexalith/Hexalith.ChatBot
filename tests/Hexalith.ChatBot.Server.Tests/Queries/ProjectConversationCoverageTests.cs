using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Queries;

public sealed class ProjectConversationCoverageTests
{
    [Fact]
    public void MixedOwnerStreamsShouldEmitCoverageOnlyForEachOwnersVerifiedContiguousInterval()
    {
        ProjectConversationItemView ownerA5 = AiItem("owner-a-5", "owner-a", 5);
        ProjectConversationItemView ownerA6 = AiItem("owner-a-6", "owner-a", 6);
        ProjectConversationItemView ownerB20 = AiItem("owner-b-20", "owner-b", 20);
        ProjectConversationItemView ownerB22 = AiItem("owner-b-22", "owner-b", 22);
        ProjectConversationPage page = new(
            [ownerB22],
            null,
            false,
            1,
            ownerB22,
            [ownerA5, ownerA6, ownerB20, ownerB22]);

        ProjectConversationResponse response = ChatBotReadQueryResultMapper.BuildProjectConversationResponse(
            "project-alpha",
            "tenant-alpha",
            page,
            null,
            "correlation-alpha",
            null);

        ProjectConversationStreamCoverage ownerA = response.Page.AuthoritativeCoverage.Single(static value => value.StateOwnerAggregateId == "owner-a");
        ownerA.FirstSourceVersion.ShouldBe(5);
        ownerA.LastSourceVersion.ShouldBe(6);
        ownerA.IsContiguous.ShouldBeTrue();
        ownerA.CoversAllKnownEvents.ShouldBeTrue();
        ProjectConversationStreamCoverage ownerB = response.Page.AuthoritativeCoverage.Single(static value => value.StateOwnerAggregateId == "owner-b");
        ownerB.IsContiguous.ShouldBeFalse();
        ownerB.CoversAllKnownEvents.ShouldBeFalse();
    }

    [Fact]
    public void EmptyAuthoritativeProjectionShouldEmitSafeAllCoveringEmptyMetadata()
    {
        ProjectConversationPage page = new([], null, false, 25, null, []);

        ProjectConversationResponse response = ChatBotReadQueryResultMapper.BuildProjectConversationResponse(
            "project-alpha",
            "tenant-alpha",
            page,
            null,
            "correlation-alpha",
            null);

        response.Page.IsAllCoveringEmpty.ShouldBeTrue();
        response.Page.AuthoritativeCoverage.ShouldBeEmpty();
    }

    private static ProjectConversationItemView AiItem(string itemId, string owner, long sourceVersion)
        => new(
            "tenant-alpha",
            "project-alpha",
            "Project Alpha",
            itemId,
            "intake-alpha",
            ProjectConversationItemKind.AiOutcome,
            ProjectConversationActorKind.AiActor,
            "AI",
            new DateTimeOffset(2026, 8, 27, 10, 0, 0, TimeSpan.Zero).AddSeconds(sourceVersion),
            LifecycleState.Associated,
            AssociationThresholdBand.Auto,
            1,
            $"response-{itemId}",
            "mailbox-alpha",
            null,
            null,
            owner,
            null,
            null,
            null,
            null,
            null,
            null,
            "chatbot-ai-execution",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-item.v1",
            sourceVersion,
            "correlation-alpha",
            AiProposalId: $"response-{itemId}",
            AiOperationId: $"generation-{itemId}",
            AiCorrelationId: "correlation-alpha",
            AiResponseSequence: sourceVersion,
            AiResponseProgressState: "rendering",
            AiResponseTerminalReason: "none",
            AiResponseVisibilityState: "metadata_only",
            AiResponseIsTerminal: false);
}
