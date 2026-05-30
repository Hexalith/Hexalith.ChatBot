using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Contracts;

using Microsoft.AspNetCore.Mvc.Testing;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

public sealed class ServerBootstrapApiTests
{
    [Fact]
    public async Task HealthEndpointShouldReturnHealthyStatus()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("Healthy");
    }

    [Fact]
    public async Task AliveEndpointShouldReturnAliveStatus()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/alive", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        body.ShouldContain("Alive");
    }

    [Fact]
    public async Task ChatBotHealthEndpointShouldExposeModuleIdentity()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/chatbot", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ChatBotHealth? health = await response.Content
            .ReadFromJsonAsync<ChatBotHealth>(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        health.ShouldNotBeNull();
        health.ModuleName.ShouldBe(ChatBotModuleInfo.ModuleName);
        health.DaprAppId.ShouldBe(ChatBotModuleInfo.DaprAppId);
    }

    [Fact]
    public async Task UnknownEndpointShouldReturnNotFound()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .GetAsync("/health/missing", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthEndpointShouldRejectUnsupportedMethods()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .PostAsync("/health", null, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }
}
