namespace Hexalith.ChatBot.UI.State.ProjectConversation;

/// <summary>
/// The ChatBot server base address the <see cref="ProjectConversationStreamingSubscriber"/> dials for its SignalR hub
/// connection (Story 10.6b transport). Registered once with the resolved ChatBot base address.
/// </summary>
/// <param name="BaseAddress">The ChatBot server base address.</param>
public sealed record ChatBotHubEndpoint(Uri BaseAddress);
