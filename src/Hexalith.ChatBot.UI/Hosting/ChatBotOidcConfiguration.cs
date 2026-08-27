namespace Hexalith.ChatBot.UI.Hosting;

internal sealed record ChatBotOidcConfiguration(
    bool Enabled,
    Uri? Authority,
    string? ClientId,
    string? Audience,
    string? Issuer)
{
    public static ChatBotOidcConfiguration Resolve(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string? authorityValue = configuration["Authentication:OpenIdConnect:Authority"];
        string? clientId = configuration["Authentication:OpenIdConnect:ClientId"];
        string? audience = configuration["Authentication:OpenIdConnect:Audience"];
        string? issuer = configuration["Authentication:OpenIdConnect:Issuer"];
        bool configured = !string.IsNullOrWhiteSpace(authorityValue) ||
            !string.IsNullOrWhiteSpace(clientId) ||
            !string.IsNullOrWhiteSpace(audience) ||
            !string.IsNullOrWhiteSpace(issuer);

        if (!configured && !environment.IsProduction())
        {
            return new ChatBotOidcConfiguration(false, null, null, null, null);
        }

        if (!TryHttpAddress(authorityValue, out Uri? authority) || string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Authentication:OpenIdConnect:Authority must be an absolute HTTP(S) URI and ClientId must be configured. "
                + "Production UI authentication cannot fail open.");
        }

        string resolvedAudience = string.IsNullOrWhiteSpace(audience) ? clientId : audience;
        string resolvedIssuer = string.IsNullOrWhiteSpace(issuer)
            ? authority!.ToString().TrimEnd('/')
            : issuer;
        if (string.IsNullOrWhiteSpace(resolvedAudience) || !TryHttpAddress(resolvedIssuer, out _))
        {
            throw new InvalidOperationException(
                "Authentication:OpenIdConnect:Audience must be non-empty and Issuer must be an absolute HTTP(S) URI.");
        }

        return new ChatBotOidcConfiguration(true, authority, clientId, resolvedAudience, resolvedIssuer);
    }

    private static bool TryHttpAddress(string? value, out Uri? uri)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }
}
