using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Net.Http.Headers;

using GeneratedClient = Hexalith.ChatBot.Client.Generated.Client;
using IGeneratedClient = Hexalith.ChatBot.Client.Generated.IClient;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();

_ = builder.Services.AddSingleton(static _ =>
{
    string? baseUrl = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_BASE_URL");
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException("HEXALITH_CHATBOT_BASE_URL must be configured for the MCP adapter.");
    }

    var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl, UriKind.Absolute) };
    string? accessToken = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_ACCESS_TOKEN");
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    return httpClient;
});
_ = builder.Services.AddSingleton<IGeneratedClient>(static provider => new GeneratedClient(provider.GetRequiredService<HttpClient>()));
_ = builder.Services.AddSingleton<IChatBotClient, ChatBotClient>();
_ = builder.Services.AddSingleton<ChatBotMcpService>();

_ = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ChatBotMcpTools>();

await builder.Build().RunAsync().ConfigureAwait(false);
