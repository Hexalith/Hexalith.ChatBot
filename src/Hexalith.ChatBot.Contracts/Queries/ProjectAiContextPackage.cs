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
