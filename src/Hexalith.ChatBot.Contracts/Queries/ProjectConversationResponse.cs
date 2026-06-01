using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Metadata-only read model for the S1 project conversation surface.
/// </summary>
public sealed record ProjectConversationResponse(
    string ProjectId,
    string? ProjectDisplayName,
    string? TenantContext,
    ProjectConversationReadStatus Status,
    LifecycleState ConversationState,
    IReadOnlyList<ProjectConversationItem> Items,
    ProjectConversationCursorPage Page,
    string SourceProvenance,
    string RedactionState,
    string RetentionClass,
    string SchemaVersion,
    string CorrelationId,
    string? SafeNextAction = null,
    ProjectAiContextPackage? AiContextPackage = null);
