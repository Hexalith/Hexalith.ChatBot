using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Metadata from one committed transition. Sequence numbers belong only to the named stream.</summary>
internal sealed record ReviewQueueTransition(
    string TenantId, string StreamId, long Version, string OperationId, string CorrelationId,
    OperationalQueueFamily Family, string SubjectId, string? IdentityProject, string[] ProjectIds,
    string State, bool Terminal, string? Reason, string OwnerRole, string NextAction,
    DateTimeOffset At, string? Risk = null, decimal? Confidence = null, int RetryCount = 0,
    string? Assignee = null, string? Actor = null, string? Mailbox = null, bool OpensWork = true, string? Intake = null,
    string? SourceMessageId = null, OperationalQueueFamily? ProducerFamily = null, Operations.WorkflowReviewTarget? RetryTarget = null,
    long? Generation = null, string[]? ResourceIds = null, bool RetainedApproval = false, string? FailedStream = null, long? FailedVersion = null)
{
    public string ItemRef => (Family == OperationalQueueFamily.PendingApproval ? "approval:" : "review:") +
        ProjectConversationStableId.Hash(System.Text.Json.JsonSerializer.Serialize(
            Family == OperationalQueueFamily.PendingApproval
                ? new[] { TenantId, IdentityProject, SubjectId }
                : [TenantId, OperationalQueueFamilies.ToWireValue(Family), IdentityProject, SubjectId]));
}
