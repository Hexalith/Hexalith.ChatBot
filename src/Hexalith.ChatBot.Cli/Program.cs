using Hexalith.ChatBot.Client;

namespace Hexalith.ChatBot.Cli;

using GeneratedClient = Client.Generated.Client;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string? baseUrl = Environment.GetEnvironmentVariable("HEXALITH_CHATBOT_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.Error.WriteLine("denied");
            Console.Error.WriteLine("reason-code: cli.configuration.missing-base-url");
            Console.Error.WriteLine("safe-next-action: configure HEXALITH_CHATBOT_BASE_URL");
            return 2;
        }

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        var transport = new GeneratedClient(httpClient);
        var client = new ChatBotClient(transport);
        return await ChatBotCliCommands.InvokeAsync(args, client, Console.Out, Console.Error, CancellationToken.None)
            .ConfigureAwait(false);
    }
}
