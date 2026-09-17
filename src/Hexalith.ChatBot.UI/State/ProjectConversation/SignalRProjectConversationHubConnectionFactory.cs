using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

internal sealed class SignalRProjectConversationHubConnectionFactory : IProjectConversationHubConnectionFactory
{
    public static SignalRProjectConversationHubConnectionFactory Instance { get; } = new();

    public IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider)
    {
        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(
                hubUri,
                options =>
                {
                    if (accessTokenProvider is not null)
                    {
                        options.AccessTokenProvider = accessTokenProvider;
                    }
                })
            .WithAutomaticReconnect()
            .Build();
        return new SignalRProjectConversationHubConnection(connection);
    }
}
