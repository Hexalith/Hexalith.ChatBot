namespace Hexalith.ChatBot.Server.Gateway.Correlation;

internal static class ChatBotCorrelationApplicationBuilderExtensions
{
    public static IApplicationBuilder UseChatBotCorrelation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ChatBotCorrelationMiddleware>();
    }
}
