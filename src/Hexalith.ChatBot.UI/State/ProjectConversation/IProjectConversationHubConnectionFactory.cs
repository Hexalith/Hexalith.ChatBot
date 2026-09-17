using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.ChatBot.UI.State.ProjectConversation;

internal interface IProjectConversationHubConnectionFactory
{
    IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider);
}
