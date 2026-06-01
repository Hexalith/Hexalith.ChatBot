using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal static class TaskIntentIdempotency
{
    public static string ComposeKey(
        string tenantId,
        string projectId,
        string sourceMessageId,
        string requesterPartyId,
        string kernelVersion,
        ProjectConversationDetectedActionKind actionKind,
        IReadOnlyList<TaskIntentSourceEvidenceOffset> evidenceOffsets)
    {
        string evidence = string.Join(
            "|",
            evidenceOffsets
                .Select(static item => string.Join(':', Normalize(item.EvidenceReference), item.StartOffset?.ToString("D", System.Globalization.CultureInfo.InvariantCulture) ?? "", item.EndOffset?.ToString("D", System.Globalization.CultureInfo.InvariantCulture) ?? "", Normalize(item.Token)))
                .Order(StringComparer.Ordinal));
        string material = string.Join(
            '|',
            Normalize(tenantId),
            Normalize(projectId),
            Normalize(sourceMessageId),
            Normalize(requesterPartyId),
            Normalize(kernelVersion),
            actionKind.ToString(),
            evidence);
        return $"task-intent:{Sha256Token(material)}";
    }

    private static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();

    private static string Sha256Token(string material)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash).ToLowerInvariant()[..32];
    }
}
