namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only reference to a file admitted into an AI context package.
/// </summary>
public sealed record ProjectAiContextPackageFile(
    string ReferenceToken,
    string FolderId,
    string FileId,
    string SourceProviderAttachmentId,
    string RedactionState,
    string RetentionClass,
    string SourceEvidenceReference);
