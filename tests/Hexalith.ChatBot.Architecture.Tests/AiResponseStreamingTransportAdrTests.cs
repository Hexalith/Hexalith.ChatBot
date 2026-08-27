using Shouldly;

namespace Hexalith.ChatBot.Architecture.Tests;

public static class AiResponseStreamingTransportAdrTests
{
    [Fact]
    public static void AiResponseStreamingTransportAdr_ShouldRecordAcceptedSignalRProjectionNudgeDecision()
    {
        string adr = ReadProjectFile("docs/adrs/ai-response-streaming-transport.md");

        adr.ShouldContain("## Status");
        adr.ShouldContain("Accepted (2026-06-19, Story 10.6a).");
        adr.ShouldContain("extend the existing SignalR projection-nudge model");
        adr.ShouldContain("metadata-only AI response progress nudges");
        adr.ShouldContain("must not introduce a dedicated token/content streaming channel");
        adr.ShouldContain("Introduce a dedicated streaming channel");
        adr.ShouldContain("Rejected as the default for Story 10.6b");
        adr.ShouldContain("Stream raw AI provider tokens directly to the UI");
        adr.ShouldContain("Polling can remain a degraded fallback");
    }

    [Fact]
    public static void AiResponseStreamingTransportAdr_ShouldPreserveSafetyFloorAndCommandGatewayAuthority()
    {
        string adr = ReadProjectFile("docs/adrs/ai-response-streaming-transport.md");

        adr.ShouldContain("SignalR messages are advisory nudges only.");
        adr.ShouldContain("must not carry authoritative response text, raw provider chunks");
        adr.ShouldContain("After each accepted nudge, the UI re-queries the typed server read endpoint");
        adr.ShouldContain("Durable completion is claimed only after a server query verifies a terminal state");
        adr.ShouldContain("A final SignalR nudge alone is not completion evidence.");
        adr.ShouldContain("fail closed on missing, stale, unauthorized, ambiguous, cross-tenant, cross-project");
        adr.ShouldContain("The server owns tenant, authorization, project, conversation, and generation-session binding");
        adr.ShouldContain("CommandGateway");
        adr.ShouldContain("Cancellation remains governed by CommandGateway.");
        adr.ShouldContain("CLI and MCP parity are preserved");
    }

    [Fact]
    public static void AiResponseStreamingTransportAdr_ShouldUnblockStoryTenSixBWithConcreteHandoff()
    {
        string adr = ReadProjectFile("docs/adrs/ai-response-streaming-transport.md");

        adr.ShouldContain("Stop/Cancel semantics for 10.6b");
        adr.ShouldContain("reconnect, resume, and stale-message handling for 10.6b");
        adr.ShouldContain("Accessibility handoff for 10.6b");
        adr.ShouldContain("progressive partial response rendering");
        adr.ShouldContain("Stop/Cancel always reachable by keyboard in a stable focusable position");
        adr.ShouldContain("polite live-region announcement");
        adr.ShouldContain("focus return to the composer or AI proposal panel");
        adr.ShouldContain("reduced-motion behavior and no motion-only status");
        adr.ShouldContain("ignores stale or out-of-order nudges");
        adr.ShouldContain("On reconnect, the client rejoins only server-authorized project/conversation groups");
        adr.ShouldContain("Every nudge that affects a visible response must include enough correlation metadata");
    }

    [Fact]
    public static void AiResponseStreamingTransportAdr_ShouldNameExpectedTestsForStoryTenSixB()
    {
        string adr = ReadProjectFile("docs/adrs/ai-response-streaming-transport.md");

        adr.ShouldContain("Tests expected for Story 10.6b");
        adr.ShouldContain("Progressive rendering re-query test");
        adr.ShouldContain("Durable-completion gate test");
        adr.ShouldContain("Fail-closed test");
        adr.ShouldContain("Stop/Cancel governance test");
        adr.ShouldContain("Reconnect/resume test");
        adr.ShouldContain("Stale/out-of-order nudge test");
        adr.ShouldContain("Metadata-only payload guard");
        adr.ShouldContain("Accessibility tests for UX-DR32");
    }

    [Fact]
    public static void Architecture_ShouldLinkAcceptedAiResponseStreamingTransportAdrWithCanonicalMapping()
    {
        string architecture = ReadProjectFile("_bmad-output/planning-artifacts/architecture.md");

        architecture.ShouldContain("AI-response streaming transport (accepted ADR, canonical Story 13.2; legacy Story 10.6a/10.6b)");
        architecture.ShouldContain("[`docs/adrs/ai-response-streaming-transport.md`](../../docs/adrs/ai-response-streaming-transport.md)");
        architecture.ShouldContain("SignalR projection-nudge model with metadata-only AI response progress nudges");
        architecture.ShouldContain("rejects a dedicated streaming");
        architecture.ShouldContain("Stop/Cancel");
        architecture.ShouldContain("10.6b");
    }

    [Fact]
    public static void StoryTenSixARecord_ShouldRemainDecisionWorkAndDelegateImplementationToTenSixB()
    {
        string story = ReadProjectFile("_bmad-output/implementation-artifacts/10-6a-streaming-transport-adr.md");

        story.ShouldContain("No production streaming implementation lands in this story.");
        story.ShouldContain("production code changes are absent");
        story.ShouldContain("progressive rendering, Stop/Cancel transport wiring, hubs/channels, provider integration, and UI behavior changes remain out of scope");
        story.ShouldContain("Confirm Story 10.6b remains the owner of implementation");
        story.ShouldContain("git diff --name-only -- src tests Hexalith.FrontComposer");
        story.ShouldContain("docs/adrs/ai-response-streaming-transport.md");
        story.ShouldContain("Transport decision: extend the existing SignalR projection-nudge model");
    }

    [Fact]
    public static void StoryTenSixBImplementation_ShouldUseMetadataOnlyNudgeAndTypedRequery()
    {
        string actions = ReadProjectFile("src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationActions.cs");
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs");
        string contract = ReadProjectFile("src/Hexalith.ChatBot.Contracts/Queries/AiResponseProgressNudge.cs");

        actions.ShouldContain("ProjectConversationAiResponseNudgeReceivedAction");
        effects.ShouldContain("LoadProjectConversationAction(action.Nudge.ProjectId)");
        contract.ShouldContain("Metadata-only SignalR projection nudge");
        contract.ShouldNotContain("Text");
        contract.ShouldNotContain("Chunk");
        contract.ShouldNotContain("Prompt");
        contract.ShouldNotContain("StackTrace");
    }

    [Fact]
    public static void StoryTenSixBImplementation_ShouldGateStopAnnouncementOnServerVerifiedState()
    {
        string stopControl = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotStreamingStopControl.razor");
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string service = ReadProjectFile("src/Hexalith.ChatBot.UI/Services/ProjectConversationService.cs");

        stopControl.ShouldContain("StopVerified");
        stopControl.ShouldContain("ResolvedStopAnnouncement");
        workspace.ShouldContain("IsStopVerified(progress)");
        workspace.ShouldContain("StopProjectConversationAiResponseAction");
        service.ShouldContain("CancelAiResponseGenerationCommand");
        service.ShouldContain("origin: ContractSurfaceOrigin.Ui");
    }

    [Fact]
    public static void StoryTenSixBProducer_ShouldProjectRealExecutionLifecycleAsServerVerifiedAiResponseProgress()
    {
        string translator = ReadProjectFile("src/Hexalith.ChatBot.Server/Projections/LowRiskAiOutcomeProjectionTranslator.cs");

        // The producer projects the REAL governed low-risk execution lifecycle: the "executing" started event becomes a
        // non-terminal Rendering progress; completion becomes a server-verified terminal state. No synthetic generation.
        translator.ShouldContain("AiResponseProgressState: \"rendering\"");
        translator.ShouldContain("AiResponseIsTerminal: false");
        translator.ShouldContain("responseTerminalState");
        translator.ShouldContain("AiResponseIsTerminal: true");
        // Metadata-only visibility on the progress projection (never content).
        translator.ShouldContain("AiResponseVisibilityState: \"metadata_only\"");
    }

    [Fact]
    public static void StoryTenSixBTransport_ShouldUseChatBotOwnedMetadataOnlySignalRHubNotAContentChannel()
    {
        string hub = ReadProjectFile("src/Hexalith.ChatBot.Server/Projections/ChatBotProjectConversationHub.cs");
        string publisher = ReadProjectFile("src/Hexalith.ChatBot.Server/Projections/SignalRProjectConversationChangePublisher.cs");
        string store = ReadProjectFile("src/Hexalith.ChatBot.Server/Projections/InMemoryProjectConversationProjectionStore.cs");

        // ChatBot-owned, tenant-grouped, metadata-only SignalR hub (the metadata-only projection-nudge model, not a
        // token/content streaming channel): the broadcast carries only the tenant id and the client re-queries.
        hub.ShouldContain(": Hub");
        hub.ShouldContain("project-conversation:");
        hub.ShouldContain("JoinTenant");
        hub.ShouldContain("tenant-forbidden");

        // The broadcaster sends to the tenant group via IHubContext and stays fail-open. Metadata-only is structural:
        // the broadcast argument list is the tenant id ONLY (no response text/chunk/prompt payload).
        publisher.ShouldContain("IHubContext<ChatBotProjectConversationHub>");
        publisher.ShouldContain("Group(ChatBotProjectConversationHub.GroupFor(tenantId))");
        publisher.ShouldContain("ProjectConversationChangedClientMethod, tenantId, cancellationToken");

        // The store emits the advisory signal only when a server-verified AI response progress row is materialized.
        store.ShouldContain("PublishProjectConversationChangedAsync");
        store.ShouldContain("AiResponseProgressState");
    }

    [Fact]
    public static void StoryTenSixBClient_ShouldSubscribeToChatBotHubAndReQueryViaNudge()
    {
        string workspace = ReadProjectFile("src/Hexalith.ChatBot.UI/Components/Governed/ChatBotProjectConversationWorkspace.razor");
        string subscriber = ReadProjectFile("src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationStreamingSubscriber.cs");
        string effects = ReadProjectFile("src/Hexalith.ChatBot.UI/State/ProjectConversation/ProjectConversationEffects.cs");

        // The workspace owns the subscriber and forwards an observed change as a fail-closed re-query signal action, and
        // re-queries authoritative state on reconnect (not just rejoining the group). [AC5]
        workspace.ShouldContain("ProjectConversationStreamingSubscriber");
        workspace.ShouldContain("EnsureSubscribedAsync");
        workspace.ShouldContain("ProjectConversationProjectionSignalReceivedAction");
        workspace.ShouldContain("ProjectConversationAiResponseReconnectAction");

        // The subscriber connects to the ChatBot-owned hub, joins the tenant group, reacts to the change broadcast, and
        // on reconnect rejoins the group AND triggers a re-query (SignalR does not replay signals missed in the gap). [AC5]
        subscriber.ShouldContain("HubConnection");
        subscriber.ShouldContain("JoinTenant");
        subscriber.ShouldContain("ProjectConversationChanged");
        subscriber.ShouldContain("Reconnected");
        subscriber.ShouldContain("_onReconnected");

        // The transport signal has no stream/version evidence, so the effect must never synthesize a rich nudge or an
        // invented watermark. It fails closed on project, tenant and scope, then requests the typed authoritative read.
        effects.ShouldContain("HandleProjectionSignalAsync");
        effects.ShouldContain("conversation.TenantContext");
        effects.ShouldContain("action.ScopeVersion != _state.Value.ProjectScopeVersion");
        effects.ShouldContain("new LoadProjectConversationAction(conversation.ProjectId)");
        effects.ShouldNotContain("BuildReQueryNudge(conversation)");
        // Rich nudges remain an independently validated path and re-query only when their reducer accepted the exact
        // nudge instance (effect/reducer agree).
        effects.ShouldContain("ReferenceEquals(_state.Value.LastAcceptedAiResponseNudge, action.Nudge)");
    }

    private static string ReadProjectFile(string relativePath)
        => File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath));

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
