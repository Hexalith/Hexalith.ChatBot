using System.Net;
using System.Security.Claims;

using Hexalith.ChatBot.UI.State.ProjectConversation;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ChatBotContributorTokenRelayTests
{
    [Fact]
    public async Task HttpAndHubRelayShouldEvictExpiredAndSignedOutTokensAndUseRefreshedTokenOnReconnect()
    {
        MutableTimeProvider time = new(new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero));
        FrontComposerUserTokenStore store = new(time);
        CircuitServicesAccessor circuit = new()
        {
            Services = new ServiceCollection()
                .AddSingleton<AuthenticationStateProvider>(new StaticAuthenticationStateProvider(ActorAlpha()))
                .BuildServiceProvider(),
        };
        FrontComposerAuthenticationOptions authentication = new();
        authentication.OpenIdConnect.Enabled = true;
        authentication.UserClaimTypes.Add("sub");
        FrontComposerAccessTokenProvider tokenProvider = new(
            new HttpContextAccessor(),
            circuit,
            store,
            Options.Create(authentication),
            NullLogger<FrontComposerAccessTokenProvider>.Instance);
        ChatBotHubEndpoint hub = new(new Uri("https://chatbot.example"), () => tokenProvider.GetAccessTokenAsync().AsTask());
        RecordingHandler terminal = new();
        using HttpClient http = new(new FrontComposerGatewayAuthorizationHandler(new HttpContextAccessor(), circuit, store)
        {
            InnerHandler = terminal,
        });

        store.Set("actor-alpha", "token-v1", time.GetUtcNow().AddMinutes(1));
        (await hub.AccessTokenProvider!()).ShouldBe("token-v1");
        _ = await http.GetAsync("https://chatbot.example/current", TestContext.Current.CancellationToken);
        terminal.AuthorizationParameters[^1].ShouldBe("token-v1");

        time.Advance(TimeSpan.FromMinutes(2));
        await Should.ThrowAsync<FrontComposerAuthenticationException>(() => hub.AccessTokenProvider!());
        _ = await http.GetAsync("https://chatbot.example/expired", TestContext.Current.CancellationToken);
        terminal.AuthorizationParameters[^1].ShouldBeNull();

        store.Set("actor-alpha", "token-v2", time.GetUtcNow().AddMinutes(5));
        (await hub.AccessTokenProvider!()).ShouldBe("token-v2");
        _ = await http.GetAsync("https://chatbot.example/reconnected", TestContext.Current.CancellationToken);
        terminal.AuthorizationParameters[^1].ShouldBe("token-v2");

        store.Remove("actor-alpha");
        await Should.ThrowAsync<FrontComposerAuthenticationException>(() => hub.AccessTokenProvider!());
        _ = await http.GetAsync("https://chatbot.example/signed-out", TestContext.Current.CancellationToken);
        terminal.AuthorizationParameters[^1].ShouldBeNull();
    }

    private static ClaimsPrincipal ActorAlpha()
        => new(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "oidc"));

    private sealed class StaticAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string?> AuthorizationParameters { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AuthorizationParameters.Add(request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
