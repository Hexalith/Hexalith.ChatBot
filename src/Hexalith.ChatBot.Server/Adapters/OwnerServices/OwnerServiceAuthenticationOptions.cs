using System.Text.Json;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.ChatBot.Server.Adapters.OwnerServices;

/// <summary>Validated credentials and transport boundary for one REST owner.</summary>
internal sealed class OwnerServiceAuthenticationOptions
{
    /// <summary>Gets the sole owner origin and base path.</summary>
    public required Uri Endpoint { get; init; }
    /// <summary>Gets the optional operator-supplied static token.</summary>
    public string? ApiToken { get; init; }
    /// <summary>Gets the trusted authority.</summary>
    public string? Authority { get; init; }
    /// <summary>Gets the expected issuer.</summary>
    public string? Issuer { get; init; }
    /// <summary>Gets the validated token endpoint within the authority.</summary>
    public string? TokenEndpoint { get; init; }
    /// <summary>Gets the dedicated client identity.</summary>
    public string? ClientId { get; init; }
    /// <summary>Gets the client credential supplied through secret configuration.</summary>
    public string? ClientSecret { get; init; }
    /// <summary>Gets the single explicitly bound tenant, when available.</summary>
    public string? TenantId { get; init; }
    /// <summary>Gets the exact receiving audience.</summary>
    public required string Audience { get; init; }
    /// <summary>Gets the exact owner operation.</summary>
    public required string Operation { get; init; }

    /// <summary>Loads one complete static or authority configuration, rejecting mixed and insecure settings.</summary>
    public static OwnerServiceAuthenticationOptions Load(IConfiguration configuration, string owner,
        string audience, string operation, IHostEnvironment environment)
    {
        string section = "ChatBot:" + owner;
        string auth = section + ":OwnerAuthentication";
        Uri configuredEndpoint = ReadUri(configuration[section + ":Endpoint"], section + ":Endpoint", environment);
        // Both owned clients generate relative paths; their base is a directory, including a non-root mount.
        Uri endpoint = new(configuredEndpoint.AbsoluteUri.TrimEnd('/') + "/");
        string? token = configuration[section + ":ApiToken"];
        bool clientCredentials = configuration.GetSection(auth).Exists();
        if (!clientCredentials)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException(section + ":ApiToken or complete owner client credentials must be configured.");
            if (token.Any(char.IsWhiteSpace)) throw new InvalidOperationException(section + ":ApiToken must be a single bearer value.");
            (bool isJwt, string? tenant) = StaticTokenTenant(token);
            if (owner == "Projects" && isJwt && tenant is null)
                throw new InvalidOperationException(section + ":ApiToken must carry exactly one unambiguous tenant in a static JWT.");
            string? explicitTenant = configuration[section + ":TenantId"];
            if (tenant is not null && explicitTenant is not null && tenant != explicitTenant)
                throw new InvalidOperationException(section + ":TenantId must match the static token's single tenant.");
            return new() { Endpoint = endpoint, ApiToken = token, Audience = audience, Operation = operation,
                TenantId = explicitTenant ?? tenant };
        }
        if (!string.IsNullOrEmpty(token) || configuration[auth + ":SigningKey"] is not null)
            throw new InvalidOperationException(auth + " must use authority client credentials without static tokens or signing keys.");
        string Required(string name) => !string.IsNullOrWhiteSpace(configuration[auth + ":" + name])
            ? configuration[auth + ":" + name]! : throw new InvalidOperationException(auth + ":" + name + " must be configured.");
        Uri authority = ReadUri(Required("Authority"), auth + ":Authority", environment);
        string issuerIdentifier = Required("Issuer");
        Uri issuer = ReadUri(issuerIdentifier, auth + ":Issuer", environment);
        Uri tokenEndpoint = ReadUri(Required("TokenEndpoint"), auth + ":TokenEndpoint", environment);
        if (issuerIdentifier != issuerIdentifier.Trim() || !SameOrigin(authority, issuer)
            || authority.AbsolutePath.TrimEnd('/') != issuer.AbsolutePath.TrimEnd('/') || !SameOrigin(authority, tokenEndpoint)
            || !tokenEndpoint.AbsolutePath.StartsWith(authority.AbsolutePath.TrimEnd('/') + "/", StringComparison.Ordinal))
            throw new InvalidOperationException(auth + " requires matching authority/issuer and a token endpoint within that authority.");
        if (Required("Audience") != audience || Required("Operation") != operation)
            throw new InvalidOperationException(auth + " must grant exactly the owner's audience and operation.");
        if (new[] { Required("ClientId"), Required("TenantId") }.Any(value => value.Any(char.IsWhiteSpace)))
            throw new InvalidOperationException(auth + " requires single identity and tenant values.");
        return new()
        {
            Endpoint = endpoint, Authority = authority.AbsoluteUri.TrimEnd('/'), Issuer = issuerIdentifier,
            TokenEndpoint = tokenEndpoint.AbsoluteUri, ClientId = Required("ClientId"), ClientSecret = Required("ClientSecret"),
            TenantId = Required("TenantId"), Audience = audience, Operation = operation,
        };
    }

    /// <summary>Compares the scheme, canonical host, and port of two absolute URIs.</summary>
    public static bool SameOrigin(Uri left, Uri right) => left.Scheme == right.Scheme
        && string.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase) && left.Port == right.Port;

    /// <summary>Validates a credential destination without including its value in diagnostics.</summary>
    public static Uri ReadUri(string? value, string key, IHostEnvironment environment)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme != Uri.UriSchemeHttps
                && !(environment.IsDevelopment() && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new InvalidOperationException(key + " must use HTTPS or Development loopback HTTP without credentials, query, or fragment.");
        return uri;
    }

    private static (bool IsJwt, string? TenantId) StaticTokenTenant(string token)
    {
        try
        {
            var parsed = new JsonWebToken(token);
            return (true, ReadSingleTenant(parsed));
        }
        catch (Exception exception) when (exception is ArgumentException or SecurityTokenMalformedException or JsonException)
        {
            return (false, null);
        }
    }

    /// <summary>Requires every receiver-recognized tenant selector to name the same single tenant.</summary>
    internal static string? ReadSingleTenant(JsonWebToken parsed)
    {
        using JsonDocument payload = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(parsed.EncodedPayload));
        HashSet<string> tenants = new(StringComparer.Ordinal);
        foreach (JsonProperty property in payload.RootElement.EnumerateObject()
            .Where(property => new[] { "eventstore:current-tenant", "eventstore:tenant", "memories:tenant", "tenant_id", "tenantId", "tid", "tenants", "tenant" }
                .Contains(property.Name, StringComparer.OrdinalIgnoreCase)))
        {
            string? encodedTenants = property.Name.Equals("tenants", StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString()?.TrimStart() : null;
            string?[] values;
            if (encodedTenants is not null && (encodedTenants.StartsWith('[') || encodedTenants.StartsWith('{')))
            {
                // Projects accepts a JSON array encoded inside its tenants string claim. Malformed list evidence
                // must remain a JWT tenant denial rather than escaping into the opaque static-token path.
                try
                {
                    values = JsonSerializer.Deserialize<string?[]>(encodedTenants) ?? [];
                }
                catch (JsonException)
                {
                    return null;
                }
            }
            else
            {
                JsonElement[] elements = property.Value.ValueKind == JsonValueKind.Array
                    ? [.. property.Value.EnumerateArray()] : [property.Value];
                if (elements.Any(value => value.ValueKind != JsonValueKind.String)) return null;
                values = [.. elements.Select(value => value.GetString())];
            }
            if (values.Length == 0) return null;
            foreach (string? value in values)
            {
                if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsWhiteSpace)) return null;
                tenants.Add(value);
            }
        }
        return tenants.Count == 1 ? tenants.Single() : null;
    }
}
