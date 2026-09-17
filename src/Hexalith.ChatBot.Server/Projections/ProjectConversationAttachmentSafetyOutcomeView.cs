using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationAttachmentSafetyOutcomeView(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    ProjectConversationAttachmentStatus ScanStatus,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    string RetryState,
    string SafeNextAction,
    string ReasonCode,
    long SourceVersion,
    string CorrelationId,
    string UnsafeHandling,
    bool SupersedesTerminalState = false);
