using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Hexalith.ChatBot.Server.Gateway;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// Authenticates the M2 release gate by its shared-secret header, compared in constant time. The resulting
/// principal is neither a human nor a workload, so it satisfies only the dedicated release-gate policy.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
internal sealed class ChatBotM2ReleaseGateAuthenticationHandler(
    IOptionsMonitor<ChatBotM2ReleaseGateAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ChatBotM2ReleaseGateAuthenticationOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? expected = Options.Token;
        if (string.IsNullOrWhiteSpace(expected)
            || !Request.Headers.TryGetValue(ChatBotCompatibilityEndpointExtensions.M2ReleaseGateTokenHeader, out StringValues presented))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented.ToString()), Encoding.UTF8.GetBytes(expected)))
        {
            return Task.FromResult(AuthenticateResult.Fail("The release-gate secret was not accepted."));
        }

        ClaimsIdentity identity = new([new Claim("sub", "chatbot-m2-release-gate")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
