using Hexalith.ChatBot.Contracts.Commands;

namespace Hexalith.ChatBot.Server.Queries;

internal sealed record AssociationRoutingStatusQuery(string AssociationId, string? TaskId);

internal sealed record ProjectConversationQuery(
    string ProjectId,
    string? Cursor,
    int PageSize,
    bool ProjectReadAuthorized,
    bool HasProjectScopeClaims,
    string? TaskId);

internal sealed record TaskIntentReviewQuery(
    string ProjectId,
    string TaskIntentId,
    bool ProjectReadAuthorized,
    string? TaskId);

internal sealed record OperationStatusQuery(string OperationId, string? TaskId);

internal sealed record OperationAuditHistoryQuery(string OperationId, string? TaskId);

internal sealed record GovernedOperationQuery(string NoteId, string? TaskId);

internal sealed record ComplianceAuditSearchQuery(
    ComplianceAuditQueryFilters? Filters,
    bool CanSearchTenantAudit,
    string? TaskId);

internal sealed record ComplianceAuditDetailQuery(
    string AuditRecordRef,
    bool CanSearchTenantAudit,
    IReadOnlyList<string> ExplicitProjectGrants,
    string? TaskId);
