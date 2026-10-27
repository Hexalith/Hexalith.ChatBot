using Hexalith.ChatBot.Server.Adapters.OwnerServices;
using Hexalith.Folders.Client.Native;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Hexalith.ChatBot.Server.Adapters.Folders;

/// <summary>Opts into the externally provisioned, tenant-bound native Folders transport.</summary>
internal static class FoldersFolderStoreServiceCollectionExtensions
{
    internal static IServiceCollection AddChatBotNativeFolders(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        IConfigurationSection section = configuration.GetSection("ChatBot:Folders");
        if (!section.GetChildren().Any()) return services;
        string Required(string name) => !string.IsNullOrWhiteSpace(section[name]) ? section[name]!
            : throw new InvalidOperationException("ChatBot:Folders requires Endpoint, TenantId and a dedicated ApiToken.");
        Uri endpoint = OwnerServiceAuthenticationOptions.ReadUri(Required("Endpoint"), "ChatBot:Folders:Endpoint", environment);
        endpoint = new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/");
        string tenant = Required("TenantId"), token = Required("ApiToken");
        if (tenant.Length is < 1 or > 64 || !char.IsAsciiLetterOrDigit(tenant[0]) || !char.IsAsciiLetterOrDigit(tenant[^1]) || tenant.Any(character => !(char.IsAsciiLetterOrDigit(character) || character == '-'))
            || tenant != tenant.ToLowerInvariant() || token.Any(char.IsWhiteSpace) || section.GetSection("OwnerAuthentication").Exists())
            throw new InvalidOperationException("ChatBot:Folders requires a canonical tenant and dedicated static ApiToken.");
        if (token.Count(character => character == '.') == 2)
        {
            var jwt = new JsonWebToken(token);
            if (OwnerServiceAuthenticationOptions.ReadSingleTenant(jwt) != tenant)
                throw new InvalidOperationException("ChatBot:Folders:TenantId must match the static token's single tenant.");
        }
        services.AddHttpClient("ChatBot.Folders.Native", client => client.BaseAddress = endpoint)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false })
            .AddHttpMessageHandler(() => new FoldersNativeBearerHandler(endpoint, tenant, token));
        services.RemoveAll<IFolderStore>();
        services.AddSingleton<IFolderStore>(provider => new FoldersFolderStore(new NativeClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("ChatBot.Folders.Native")) { ReadResponseAsString = true }, tenant));
        return services;
    }
}
