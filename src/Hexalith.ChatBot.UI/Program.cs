using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.ServiceDefaults;
using Hexalith.ChatBot.UI.Components;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Services;

using Fluxor;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddServiceDefaults();
_ = builder.Services.AddLocalization();
_ = builder.Services.AddRazorComponents().AddInteractiveServerComponents();
_ = builder.Services.AddFluentUIComponents();
_ = builder.Services.AddFluxor(static options => options.ScanAssemblies(typeof(Program).Assembly));

// The UI reaches the governed command spine ONLY through the typed Client facade (IChatBotClient over the
// generated transport). It never references the Server, the gateway stages, or the audit/idempotency seams.
_ = builder.Services.AddHttpClient<IClient, Client>(static (provider, http) =>
    http.BaseAddress = ResolveChatBotBaseAddress(provider.GetRequiredService<IConfiguration>()));
_ = builder.Services.AddScoped<IChatBotClient, ChatBotClient>();
_ = builder.Services.AddScoped<GovernedOperationService>();
_ = builder.Services.AddScoped<ChatBotAnnouncementDeduplicationState>();
_ = builder.Services.AddScoped<ChatBotUiTextLocalizer>();
_ = builder.Services.AddScoped<ChatBotCultureFormatter>();

WebApplication app = builder.Build();

_ = app.MapDefaultEndpoints();
_ = app.UseStaticFiles();
_ = app.UseRequestLocalization(ChatBotSupportedCultures.CreateRequestLocalizationOptions());
_ = app.UseAntiforgery();
_ = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

// Resolves the ChatBot server base address from Aspire service discovery, falling back to a configured value.
static Uri ResolveChatBotBaseAddress(IConfiguration configuration)
{
    string? configured = configuration["services:chatbot:https:0"]
        ?? configuration["services:chatbot:http:0"]
        ?? configuration["ChatBot:BaseAddress"];
    return new Uri(configured ?? "https://chatbot");
}

/// <summary>Entry point marker for the ChatBot UI host (used by WebApplicationFactory in tests).</summary>
public partial class Program;
