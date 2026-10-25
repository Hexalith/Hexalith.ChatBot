using System.Net;
using Hexalith.ChatBot.Server.Adapters.OwnerServices;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters.OwnerServices;

/// <summary>Exercises the production factory configuration against real HTTP redirects.</summary>
public sealed class OwnerServiceHttpFactoryTests
{
    [Theory]
    [InlineData(307, true)]
    [InlineData(308, true)]
    [InlineData(307, false)]
    [InlineData(308, false)]
    public async Task ConfiguredIssuerAndOwnerTransportsNeverFollowRedirectsOrReplayCredentials(int status, bool issuerTransport)
    {
        int destinationRequests = 0;
        bool destinationSawSecret = false;
        bool destinationSawAuthorization = false;
        OwnerAuthenticationTestServer destination = await OwnerAuthenticationTestServer.StartAsync(async context =>
        {
            Interlocked.Increment(ref destinationRequests);
            using StreamReader reader = new(context.Request.Body);
            string body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);
            destinationSawSecret = body.Contains("synthetic-client-secret", StringComparison.Ordinal);
            destinationSawAuthorization = context.Request.Headers.ContainsKey("Authorization");
            context.Response.StatusCode = StatusCodes.Status200OK;
        });
        try
        {
            bool sourceReceivedCredential = false;
            OwnerAuthenticationTestServer source = await OwnerAuthenticationTestServer.StartAsync(async context =>
            {
                if (issuerTransport)
                {
                    IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
                    sourceReceivedCredential = form["client_secret"] == "synthetic-client-secret";
                }
                else sourceReceivedCredential = context.Request.Headers.Authorization == "Bearer synthetic-owner-token";
                context.Response.StatusCode = status;
                context.Response.Headers.Location = new Uri(destination.Endpoint, "redirect-destination").AbsoluteUri;
            });
            try
            {
                ServiceCollection services = new();
                OwnerServiceAuthenticationOptions options = new()
                {
                    Endpoint = source.Endpoint, ApiToken = "synthetic-owner-token",
                    Audience = "hexalith-memories-server", Operation = "memories:derived-store",
                };
                services.AddHttpClient("owner-redirect-test")
                    .WithOwnerAuthentication(options, new OwnerAuthenticationTestEnvironment(), "Memories");
                using ServiceProvider provider = services.BuildServiceProvider();
                using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(
                    issuerTransport ? JwtWorkloadAssertionIssuer.HttpClientName : "owner-redirect-test");
                using HttpRequestMessage request = new(issuerTransport ? HttpMethod.Post : HttpMethod.Get,
                    new Uri(source.Endpoint, issuerTransport ? "realms/local/token" : "api/owner"));
                if (issuerTransport) request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials", ["client_id"] = "synthetic-client",
                    ["client_secret"] = "synthetic-client-secret",
                });
                using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
                ((int)response.StatusCode).ShouldBe(status);
                sourceReceivedCredential.ShouldBeTrue();
                destinationRequests.ShouldBe(0);
                destinationSawSecret.ShouldBeFalse();
                destinationSawAuthorization.ShouldBeFalse();
            }
            finally
            {
                await source.DisposeAsync();
            }
        }
        finally
        {
            await destination.DisposeAsync();
        }
    }
}
