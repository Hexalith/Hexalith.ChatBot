namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only, project-scoped AI context manifest derived from authorized attachment projections.
/// </summary>
public sealed record ProjectAiContextPackage(
    string TenantId,
    string ProjectId,
    string PolicySnapshotId,
    string RedactionDecision,
    string RetentionClass,
    string ProviderReuseSetting,
    string PackageId,
    string PackageVersion,
    string SchemaVersion,
    long SourceVersion,
    string CorrelationId,
    IReadOnlyList<ProjectAiContextPackageFile> IncludedFiles,
    IReadOnlyList<ProjectAiContextPackageExclusion> ExcludedFiles,
    IReadOnlyList<string> SourceEvidenceReferences,
    string SourceProvenance,
    string DerivationKernelVersion)
{
    public const string SchemaVersionValue = "chatbot.project-ai-context-package.v1";
    public const string DerivationKernelVersionValue = "chatbot.project-ai-context-package.kernel.v1";
}

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

/// <summary>
/// Metadata-only record of a file excluded from an AI context package.
/// </summary>
public sealed record ProjectAiContextPackageExclusion(
    string ReferenceToken,
    string ReasonCode,
    string? SourceEvidenceReference = null);
