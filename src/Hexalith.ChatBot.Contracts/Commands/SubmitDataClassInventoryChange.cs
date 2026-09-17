using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The compliance-admin-gated governed command that edits the data-class inventory (AC2/AC3). A structural twin of
/// <see cref="SubmitRetentionConfigurationChange"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write on unauthorized scope / invalid command / audit-writer-down.
/// </summary>
public sealed record SubmitDataClassInventoryChange(
    string InventoryChangeId,
    string SourceInventorySnapshotId,
    string ProposedInventorySnapshotId,
    long SourceVersion,
    DataClassInventoryChangeSet ChangeSet,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string OldInventorySnapshotFingerprint,
    string NewInventorySnapshotFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;
