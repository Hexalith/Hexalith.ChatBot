using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Contracts;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
    public async Task CommandEndpointShouldRejectUnauthenticatedSubmissionsWithSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authentication_failure");
        root.GetProperty("code").GetString().ShouldBe("authentication_denied");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldAcceptAuthenticatedTenantBoundSubmission()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-alpha", "allowed-resource"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument accepted = JsonDocument.Parse(body);
        JsonElement root = accepted.RootElement;
        root.GetProperty("commandId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("lifecycleState").GetString().ShouldBe("accepted");
        body.ShouldNotContain("allowed-resource", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldRejectAuthenticatedCrossTenantSubmissionWithSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = AuthenticatedFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client
            .SendAsync(CommandSubmissionRequest("tenant-beta", "restricted-project-sentinel"), TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("category").GetString().ShouldBe("authorization_denied");
        root.GetProperty("code").GetString().ShouldBe("authorization_denied");
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAW");
        root.GetProperty("taskId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAX");
        root.GetProperty("details").GetProperty("visibility").GetString().ShouldBe("metadata_only");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-beta", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
    }

    [Fact]
    public async Task CommandEndpointShouldNotEchoInvalidCorrelationMetadataInSafeProblemDetails()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = CommandSubmissionRequest("tenant-alpha", "restricted-project-sentinel");
        request.Headers.Remove("X-Correlation-Id");
        request.Headers.Remove("X-Hexalith-Task-Id");
        request.Headers.Add("X-Correlation-Id", "/tmp/sensitive-correlation");
        request.Headers.Add("X-Hexalith-Task-Id", "payload-sentinel-task");

        using HttpResponseMessage response = await client
            .SendAsync(request, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        string body = await response.Content
            .ReadAsStringAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using JsonDocument problem = JsonDocument.Parse(body);
        JsonElement root = problem.RootElement;
        root.GetProperty("correlationId").GetString().ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAY");
        root.GetProperty("taskId").ValueKind.ShouldBe(JsonValueKind.Null);
        body.ShouldNotContain("/tmp/sensitive-correlation", Case.Insensitive);
        body.ShouldNotContain("payload-sentinel-task", Case.Insensitive);
        body.ShouldNotContain("restricted-project-sentinel", Case.Insensitive);
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

    private static WebApplicationFactory<Program> AuthenticatedFactory(string tenantId)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton<IStartupFilter>(new TestPrincipalStartupFilter(tenantId))));

    private static HttpRequestMessage CommandSubmissionRequest(string tenantId, string resourceName)
    {
        string payload =
            $$"""
            {
              "commandId": "01ARZ3NDEKTSV4RRFFQ69G5FAY",
              "commandType": "TenantScopedCommand",
              "command": {
                "tenantId": "{{tenantId}}",
                "resourceName": "{{resourceName}}"
              },
              "requestSchemaVersion": "v1"
            }
            """;

        HttpRequestMessage request = new(HttpMethod.Post, "/api/v1/commands");
        request.Headers.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        request.Headers.Add("X-Hexalith-Task-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAX");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        return request;
    }

    private sealed class TestPrincipalStartupFilter(string tenantId) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(
                    async (context, continuation) =>
                    {
                        Claim[] claims = [new("sub", "actor-alpha"), new("eventstore:tenant", tenantId)];
                        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
                        await continuation().ConfigureAwait(false);
                    });
                next(app);
            };
    }
}
