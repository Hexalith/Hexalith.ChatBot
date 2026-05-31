namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// A tenant-scoped, metadata-only reader over recorded audit envelopes. It backs the UI's audit-history surface
/// (Story 1.9 M3): a real read of the operation's post-commit audit envelope summary, never a client-side
/// fabrication. The full audit query/investigation surface remains Epic 9 (Story 9.3).
/// </summary>
internal interface IAuditHistoryReader
{
    /// <summary>
    /// Returns the post-commit audit envelope(s) recorded for the given command within the tenant. A foreign
    /// tenant or an unknown command yields an empty list (the caller collapses to a safe-not-found), so the read
    /// never confirms existence across the tenant boundary.
    /// </summary>
    /// <param name="tenantId">The authenticated tenant scope.</param>
    /// <param name="commandId">The command identity tying the operation to its audit envelopes.</param>
    /// <returns>The matching post-commit envelopes, in record order.</returns>
    IReadOnlyList<AuditEnvelope> GetPostCommitEnvelopes(string tenantId, string commandId);
}
