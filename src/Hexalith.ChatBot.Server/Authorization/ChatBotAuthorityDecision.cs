namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>The typed metadata-only result of owner authorization.</summary>
/// <param name="Principal">The server-issued narrow authority, absent for denial.</param>
/// <param name="ReasonCode">The normalized safe denial code.</param>
/// <param name="EvidenceReferences">Safe owner version references, without owner payloads.</param>
internal sealed record ChatBotAuthorityDecision(ChatBotAuthorityPrincipal? Principal, string ReasonCode, IReadOnlyList<string> EvidenceReferences)
{
    /// <summary>Whether all required current owners granted this exact scope.</summary>
    public bool IsAllowed => Principal is not null;
}
