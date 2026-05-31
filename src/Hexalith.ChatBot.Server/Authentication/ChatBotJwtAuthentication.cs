using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>
/// JWT bearer authentication for the ChatBot adapter surface. It is wired ONLY when the hosting topology
/// supplies <c>Authentication:JwtBearer:Authority</c> (OIDC discovery, e.g. the Aspire Keycloak realm) or a
/// symmetric <c>SigningKey</c> (dev). When neither is configured — the in-process <c>WebApplicationFactory</c>
/// tests, which inject a test principal directly — no JWT middleware is added, so the test principal survives.
/// </summary>
/// <remarks>
/// Tenant authority comes ONLY from the authenticated token: the gateway binds <c>tenantId</c> from the
/// <c>eventstore:tenant</c>/<c>tenant</c> claim, never from the request body/route/query. Inbound claims are NOT
/// remapped to Microsoft URIs (<c>MapInboundClaims = false</c>) so <c>sub</c> and <c>eventstore:tenant</c> reach
/// the gateway with their original names.
/// </remarks>
internal static class ChatBotJwtAuthentication
{
    public static bool IsConfigured(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return !string.IsNullOrWhiteSpace(configuration["Authentication:JwtBearer:Authority"])
            || !string.IsNullOrWhiteSpace(configuration["Authentication:JwtBearer:SigningKey"]);
    }

    public static IServiceCollection AddChatBotJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!IsConfigured(configuration))
        {
            return services;
        }

        string? authority = configuration["Authentication:JwtBearer:Authority"];
        string? issuer = configuration["Authentication:JwtBearer:Issuer"];
        string? audience = configuration["Authentication:JwtBearer:Audience"];
        string? signingKey = configuration["Authentication:JwtBearer:SigningKey"];
        bool requireHttpsMetadata = bool.TryParse(configuration["Authentication:JwtBearer:RequireHttpsMetadata"], out bool requireHttps) && requireHttps;

        _ = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = requireHttpsMetadata;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                };

                if (!string.IsNullOrWhiteSpace(authority))
                {
                    options.Authority = authority;
                }
                else if (!string.IsNullOrWhiteSpace(signingKey))
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
                }
            });

        return services;
    }
}
