using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Governance.Outbound;

internal sealed record SenderProjectAuthorityEvidence(
    bool HasProjectAuthority,
    IReadOnlyList<string> Scopes,
    string EvidenceRef)
{
    public bool HasScope(string scope)
        => Scopes.Contains(scope, StringComparer.Ordinal);
}
