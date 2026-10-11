using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Signs the exact owner stream independently of the OIDC operation assertion.</summary>
internal sealed class ChatBotStreamReadProofSource(IConfiguration configuration, ISystemClock clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Returns no proof when the configured key or requested owner scope is unavailable.</summary>
    public string? Issue(StreamReadRequest request)
    {
        string? privateKey = configuration["Authentication:BoundStreamRead:PrivateKey"];
        if (request.Domain is not ("chatbot" or "projects" or "party" or "tenants") ||
            string.IsNullOrWhiteSpace(request.Tenant) || string.IsNullOrWhiteSpace(request.AggregateId) ||
            string.IsNullOrWhiteSpace(privateKey))
        {
            return null;
        }

        try
        {
            using RSA rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
            if (rsa.KeySize < 2048) { return null; }
            DateTimeOffset now = clock.UtcNow;
            if (now.Offset != TimeSpan.Zero) { return null; }
            var payload = new ProofPayload(1, "chatbot", request.Tenant, request.Domain, request.AggregateId,
                now.ToUnixTimeSeconds(), now.AddSeconds(30).ToUnixTimeSeconds(),
                Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
            string body = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions));
            string signature = Base64UrlEncode(rsa.SignData(Encoding.ASCII.GetBytes(body), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            return $"{body}.{signature}";
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Signs an exact ChatBot command and represented principal.</summary>
    public string? IssueCommand(BoundWorkloadCommandRequest request)
    {
        string? privateKey = configuration["Authentication:BoundStreamRead:PrivateKey"];
        if (request.Command.Domain != "chatbot" || string.IsNullOrWhiteSpace(privateKey))
        {
            return null;
        }

        try
        {
            return BoundWorkloadCommandProof.Issue(request, "chatbot", privateKey, clock.UtcNow);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or CryptographicException)
        {
            return null;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record ProofPayload(int Version, string Caller, string Tenant, string Domain, string AggregateId,
        long IssuedAt, long ExpiresAt, string Nonce);
}
