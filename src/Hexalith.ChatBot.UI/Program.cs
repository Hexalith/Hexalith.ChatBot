using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.UI.Components;
using Hexalith.ChatBot.UI.Hosting;
using Hexalith.ChatBot.UI.Localization;
using Hexalith.ChatBot.UI.Registration;
using Hexalith.ChatBot.UI.Services;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.FluentUI.AspNetCore.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

_ = builder.AddChatBotUiHostDefaults();
_ = builder.Services.AddLocalization();
_ = builder.Services.AddRazorComponents().AddInteractiveServerComponents();
_ = builder.Services.AddFluentUIComponents();
_ = builder.Services.AddHexalithFrontComposerQuickstart(static options => options.ScanAssemblies(typeof(Program).Assembly));
_ = builder.Services.AddHexalithDomain<ChatBotUiFrontComposerMarker>();

ChatBotOidcConfiguration oidc = ChatBotOidcConfiguration.Resolve(builder.Configuration, builder.Environment);
if (oidc.Enabled)
{
    _ = builder.Services.AddHexalithFrontComposerServerSecurity(options =>
    {
        options.OpenIdConnect.Enabled = true;
        options.OpenIdConnect.ProviderName = "Keycloak";
        options.OpenIdConnect.Authority = oidc.Authority;
        options.OpenIdConnect.ClientId = oidc.ClientId;
        // hexalith-chatbot is deliberately a public authorization-code/PKCE client. A browser-facing UI must not
        // manufacture or embed a client secret merely to satisfy a confidential-client recipe.
        options.OpenIdConnect.ClientSecret = null;
        options.OpenIdConnect.Audience = oidc.Audience;
        options.OpenIdConnect.ValidIssuer = oidc.Issuer;
        options.OpenIdConnect.RoleClaimType = "roles";
        options.OpenIdConnect.ResponseType = "code";
        options.OpenIdConnect.SaveTokens = true;
        options.TenantClaimTypes.Add("eventstore:tenant");
        options.UserClaimTypes.Add("sub");
    });
    _ = builder.Services.AddAuthorization();
}

// The UI reaches the governed command spine ONLY through the typed Client facade (IChatBotClient over the
// generated transport). It never references the Server, the gateway stages, or the audit/idempotency seams.
IHttpClientBuilder chatBotHttpClient = builder.Services.AddHttpClient<IClient, Client>(static (provider, http) =>
    http.BaseAddress = ResolveChatBotBaseAddress(provider.GetRequiredService<IConfiguration>()));
if (oidc.Enabled)
{
    _ = chatBotHttpClient.AddFrontComposerGatewayAuthorization();
}
_ = builder.Services.AddScoped<IChatBotClient, ChatBotClient>();
// Story 10.6b transport: the ChatBot server base address the project-conversation streaming subscriber dials for its
// SignalR hub connection (the ChatBot-owned project-conversation change hub).
_ = builder.Services.AddSingleton(provider =>
{
    IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
    FrontComposerAccessTokenProvider? tokenProvider = oidc.Enabled
        ? provider.GetRequiredService<FrontComposerAccessTokenProvider>()
        : null;
    return new Hexalith.ChatBot.UI.State.ProjectConversation.ChatBotHubEndpoint(
        ResolveChatBotBaseAddress(configuration),
        tokenProvider is null ? null : () => tokenProvider.GetAccessTokenAsync().AsTask());
});
_ = builder.Services.AddScoped<GovernedOperationService>();
_ = builder.Services.AddScoped<OperationalDashboardService>();
_ = builder.Services.AddScoped<AssociationReviewService>();
_ = builder.Services.AddScoped<ComplianceAuditService>();
_ = builder.Services.AddScoped<ProjectConversationService>();
_ = builder.Services.AddScoped<ChatBotAnnouncementDeduplicationState>();
_ = builder.Services.AddScoped<ChatBotUiTextLocalizer>();
_ = builder.Services.AddScoped<ChatBotCultureFormatter>();

WebApplication app = builder.Build();

_ = app.MapChatBotUiHealthEndpoints();
_ = app.UseStaticFiles();
_ = app.UseRequestLocalization(ChatBotSupportedCultures.CreateRequestLocalizationOptions());
if (oidc.Enabled)
{
    _ = app.UseAuthentication();
    _ = app.UseAuthorization();
}

_ = app.UseAntiforgery();
var components = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
if (oidc.Enabled)
{
    _ = components.RequireAuthorization();
    _ = app.MapHexalithFrontComposerAuthenticationEndpoints();
}

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
