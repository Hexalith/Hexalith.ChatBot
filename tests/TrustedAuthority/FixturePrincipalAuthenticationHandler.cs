using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>
/// Test-only stand-in for the host's bearer handler. It authenticates nothing itself: it surfaces the synthetic
/// principal a fixture startup filter already placed on the request, so scheme-bound policies evaluate that
/// principal exactly as they would evaluate a validated bearer token, and reports no result for anonymous requests.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
internal sealed class FixturePrincipalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(Context.User.Identities.Any(static identity => identity.IsAuthenticated)
            ? AuthenticateResult.Success(new AuthenticationTicket(Context.User, Scheme.Name))
            : AuthenticateResult.NoResult());
}
