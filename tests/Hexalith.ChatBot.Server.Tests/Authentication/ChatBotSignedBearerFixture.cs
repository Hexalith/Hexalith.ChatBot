using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Hexalith.ChatBot.Server.Tests.Authentication;

/// <summary>Uses the repository's signed-token test pattern with the real ChatBot bearer validator.</summary>
internal static class ChatBotSignedBearerFixture
{
    private const string Issuer = "chatbot-boundary-fixture";
    private const string Audience = "chatbot-boundary-tests";
    private static readonly string SigningKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    /// <summary>Configures issuer, audience and signing material without replacing authentication or policy handlers.</summary>
    public static void Configure(IWebHostBuilder builder)
    {
        _ = builder.UseSetting("Authentication:JwtBearer:Authority", string.Empty);
        _ = builder.UseSetting("Authentication:JwtBearer:SigningKey", SigningKey);
        _ = builder.UseSetting("Authentication:JwtBearer:Issuer", Issuer);
        _ = builder.UseSetting("Authentication:JwtBearer:Audience", Audience);
    }

    /// <summary>Mints a short-lived signed token whose validated claims bind exactly one tenant.</summary>
    public static string CreateToken(string tenantId, bool workload = false, bool invalidSignature = false, bool invalidIssuer = false, bool invalidAudience = false)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<string, object> claims = new()
        {
            ["sub"] = "actor-" + tenantId,
            ["eventstore:tenant"] = tenantId,
            ["chatbot:actor-type"] = "human",
        };
        if (workload)
        {
            claims["eventstore:workload"] = "fixture-workload";
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = invalidIssuer ? "external-issuer" : Issuer,
            Audience = invalidAudience ? "external-audience" : Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(invalidSignature ? new string('a', 64) : SigningKey)), SecurityAlgorithms.HmacSha256),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.AddSeconds(-1).UtcDateTime,
            Expires = now.AddMinutes(5).UtcDateTime,
            Claims = claims,
        });
    }
}
