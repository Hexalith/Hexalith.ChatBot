using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Server.Tests.TrustedAuthority;

/// <summary>
/// Test-only transport for in-process SDK route fixtures. A fixture principal placed on the request by a startup
/// filter passes through unchanged. Otherwise the request keeps its anonymous primary identity and gains one
/// claimless transport identity after it: that satisfies the conforming host's authenticated, scheme-bound policy
/// requirement while ChatBot admission still sees no subject, tenant or workload, exactly as the earlier allow-all
/// fixture policy delivered the request.
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
        ClaimsPrincipal principal = Context.User.Identities.Any(static identity => identity.IsAuthenticated)
            ? Context.User
            : new ClaimsPrincipal([.. Context.User.Identities, new ClaimsIdentity(Scheme.Name)]);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
