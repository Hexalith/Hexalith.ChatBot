namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// A single prioritized, grouped approval row for the reviewer work surface (Story 7.8). The group is keyed by the
/// safe <c>(requester × command × project)</c> fingerprint and carries the safe priority label/explanation token plus
/// the per-item count rendered on the one primary batch action. All values are safe refs/tokens or bounded integers —
/// never project names, evidence, recipient PII, audit reasons, or command bodies.
/// </summary>
/// <param name="GroupKey">The safe <c>sha256:</c> group fingerprint.</param>
/// <param name="RequesterRef">Safe requester ref label (redacted/omitted when unauthorized).</param>
/// <param name="CommandRef">Safe command token.</param>
/// <param name="ProjectRef">Safe project ref label (redacted/omitted when unauthorized).</param>
/// <param name="PriorityLabel">Plain-language priority headline preceding the raw token.</param>
/// <param name="PriorityExplanation">Safe single-token priority explanation (no spaces, ascii-safe).</param>
/// <param name="ItemCount">The number of underlying approval items in the group (≥ 1) shown on the batch action.</param>
public sealed record ChatBotApprovalPriorityGroupRow(
    string GroupKey,
    string RequesterRef,
    string CommandRef,
    string ProjectRef,
    string PriorityLabel,
    string PriorityExplanation,
    int ItemCount);
