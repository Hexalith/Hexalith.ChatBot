using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hexalith.ChatBot.Server.Projections;

internal static class ProjectConversationCursor
{
    private static readonly byte[] SigningKey = "chatbot-project-conversation-cursor-v1"u8.ToArray();

    public static string Create(string tenantId, string projectId, DateTimeOffset occurredAt, string itemId)
    {
        string tenantHash = Hash(tenantId);
        string projectHash = Hash(projectId);
        string payload = JsonSerializer.Serialize(new CursorPayload(tenantHash, projectHash, occurredAt.UtcTicks, itemId));
        string signature = Sign(payload);
        return Base64UrlEncode(Encoding.UTF8.GetBytes(payload + "." + signature));
    }

    public static bool TryRead(
        string? cursor,
        string tenantId,
        string projectId,
        out DateTimeOffset occurredAt,
        out string? itemId)
    {
        occurredAt = default;
        itemId = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return true;
        }

        try
        {
            string decoded = Encoding.UTF8.GetString(Base64UrlDecode(cursor));
            int separator = decoded.LastIndexOf('.');
            if (separator <= 0 || separator == decoded.Length - 1)
            {
                return false;
            }

            string payloadText = decoded[..separator];
            string signature = decoded[(separator + 1)..];
            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(payloadText)),
                Encoding.UTF8.GetBytes(signature)))
            {
                return false;
            }

            CursorPayload? payload = JsonSerializer.Deserialize<CursorPayload>(payloadText);
            if (payload is null ||
                !string.Equals(payload.TenantHash, Hash(tenantId), StringComparison.Ordinal) ||
                !string.Equals(payload.ProjectHash, Hash(projectId), StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(payload.ItemId))
            {
                return false;
            }

            occurredAt = new DateTimeOffset(payload.UtcTicks, TimeSpan.Zero);
            itemId = payload.ItemId;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Sign(string payload)
        => Convert.ToHexString(HMACSHA256.HashData(SigningKey, Encoding.UTF8.GetBytes(payload)));

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        string padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }

    private sealed record CursorPayload(string TenantHash, string ProjectHash, long UtcTicks, string ItemId);
}
