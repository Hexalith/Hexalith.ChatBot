namespace Hexalith.ChatBot.UI.State.ProjectConversation;

/// <summary>
/// The ChatBot server base address the <see cref="ProjectConversationStreamingSubscriber"/> dials for its SignalR hub
/// connection (Story 10.6b transport). Registered once with the resolved ChatBot base address.
/// </summary>
/// <param name="BaseAddress">The ChatBot server base address.</param>
/// <param name="AccessTokenProvider">
/// Optional bearer-token provider forwarded to the hub <see cref="ProjectConversationStreamingSubscriber"/>
/// <c>HubConnection</c>. Null in the no-JWT dev/test posture (the hub allows unauthenticated joins there); a JWT-on
/// deployment supplies it so the connection carries the caller's token and the server binds the tenant claim and fails
/// closed cross-tenant. The FrontComposer host supplies the authenticated circuit token provider when OIDC is enabled.
/// </param>
public sealed record ChatBotHubEndpoint(Uri BaseAddress, Func<Task<string?>>? AccessTokenProvider = null);
