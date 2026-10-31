using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>
/// Presents the Dapr application-channel token that the EventStore sidecar channel policy requires on every
/// subscription delivery, the way the topology's sidecar does, so in-process tests reach the delivery handlers.
/// </summary>
internal static class DaprAppChannelTestAccess
{
    /// <summary>The per-test-host channel token.</summary>
    public const string Token = "chatbot-test-app-channel-token";

    /// <summary>Configures the host's application-channel token.</summary>
    /// <param name="factory">The test host factory.</param>
    /// <returns>A factory whose host expects <see cref="Token"/>.</returns>
    public static WebApplicationFactory<Program> WithAppChannelToken(this WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        return factory.WithWebHostBuilder(static builder => builder.UseSetting(DaprAppChannelToken.ConfigurationKey, Token));
    }

    /// <summary>Creates a client that presents the channel token as the sidecar does.</summary>
    /// <param name="factory">The test host factory.</param>
    /// <returns>The client.</returns>
    public static HttpClient CreateAppChannelClient(this WebApplicationFactory<Program> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, Token);
        return client;
    }
}
