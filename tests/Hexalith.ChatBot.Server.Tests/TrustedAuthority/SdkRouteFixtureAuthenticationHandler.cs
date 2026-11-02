using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>
/// Test-only authenticated service transport for in-process SDK route fixtures. It preserves every existing
/// workload claim exactly. Other fixture principals retain their subject and tenant evidence and gain only the
/// transport's signed service surface. A claimless caller still has no ChatBot subject or tenant authority.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
internal sealed class SdkRouteFixtureAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // The authenticated test transport asserts only its service surface. Existing workload evidence passes
        // through exactly, including absent or conflicting surfaces used by the SDK denial controls.
        ClaimsPrincipal principal = Context.User.Identities.Any(static identity => identity.IsAuthenticated && identity.AuthenticationType == "EventStoreWorkload")
            ? Context.User
            : new ClaimsPrincipal([.. Context.User.Identities,
                new ClaimsIdentity([new Claim("eventstore:surface", "service")], Scheme.Name)]);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
