namespace Hexalith.ChatBot.UI.State.ProjectConversation;

public sealed record LoadProjectConversationAction(string ProjectId, string? Cursor = null)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");

    public bool IsHistory => !string.IsNullOrWhiteSpace(Cursor);
}

public sealed record ProjectConversationLoadedAction(
    ProjectConversationModel Conversation,
    string? RequestId = null,
    string? RequestedProjectId = null,
    string? Cursor = null);

public sealed record ProjectConversationFailedAction(
    string ErrorCode,
    string? RequestId = null,
    string? RequestedProjectId = null,
    string? Cursor = null);

public sealed record SetProjectConversationComposerModeAction(ProjectConversationComposerMode Mode);

public sealed record SubmitProjectConversationComposerAction(
    string ProjectId,
    ProjectConversationComposerMode Mode,
    string Text,
    string Locale,
    long ExpectedSourceVersion)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
}

public sealed record ProjectConversationComposerValidationFailedAction(string ErrorCode)
{
    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationSubmissionAcceptedAction(ProjectConversationSubmissionReceiptModel Receipt)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationSubmissionFailedAction(string ErrorCode)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationAiResponseNudgeModel(
    string ProjectId,
    string ConversationId,
    string ResponseId,
    string GenerationId,
    string CorrelationId,
    long SourceVersion,
    long Sequence,
    string State,
    string RedactionState,
    string VisibilityState);

// Raised by the workspace when the signal-only EventStore projection-changed SignalR transport reports the
// project-conversation projection changed for the current tenant. Carries no version/sequence (signal-only); the
// effect synthesizes a forward-looking metadata-only nudge for the current conversation and re-queries authoritative
// server state. ProjectId/TenantId let the effect fail closed on a signal that does not match the loaded conversation.
public sealed record ProjectConversationProjectionSignalReceivedAction(string ProjectId, string TenantId)
{
    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationAiResponseNudgeReceivedAction(ProjectConversationAiResponseNudgeModel Nudge);

public sealed record ProjectConversationAiResponseNudgeRejectedAction(string ErrorCode);

public sealed record ProjectConversationAiResponseReconnectAction(string ProjectId)
{
    public long ScopeVersion { get; init; }
}

public sealed record StopProjectConversationAiResponseAction(ProjectConversationAiResponseProgressModel Progress)
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
}

public sealed record ProjectConversationAiResponseCancellationPendingAction(string ResponseId, string GenerationId)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationAiResponseCancellationAcceptedAction(ProjectConversationSubmissionReceiptModel Receipt)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record ProjectConversationAiResponseCancellationFailedAction(string ErrorCode)
{
    public string? ProjectId { get; init; }

    public string? RequestId { get; init; }

    public long ScopeVersion { get; init; }
}

public sealed record OpenProjectAssociationWhyPanelAction(string ProjectId, string AssociationId);

public sealed record ProjectAssociationWhyPanelLoadedAction(
    string ProjectId,
    string AssociationId,
    ProjectAssociationWhyPanelModel Panel);

public sealed record ProjectAssociationWhyPanelFailedAction(string ProjectId, string AssociationId, string ErrorCode);

public sealed record CloseProjectAssociationWhyPanelAction;
