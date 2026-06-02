namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record OutboundApprovalContentSnapshot(
    OutboundDraftContent ProposedContent,
    OutboundDraftContent? ApprovedContent,
    string ProposedContentRedactionState,
    string? ApprovedContentRedactionState,
    string PublicRedactionState = "metadata_only",
    string SchemaVersion = "chatbot.outbound-approval-content-snapshot.v1");
