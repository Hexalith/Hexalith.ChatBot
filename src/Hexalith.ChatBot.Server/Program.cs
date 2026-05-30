using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();

WebApplication app = builder.Build();

_ = app.MapDefaultEndpoints();
_ = app.MapGet("/health/chatbot", () => Results.Ok(new ChatBotHealth(ChatBotClientDescriptor.Default.ModuleName, ChatBotClientDescriptor.Default.DaprAppId)));

app.Run();

public sealed record ChatBotHealth(string ModuleName, string DaprAppId);

public partial class Program;
