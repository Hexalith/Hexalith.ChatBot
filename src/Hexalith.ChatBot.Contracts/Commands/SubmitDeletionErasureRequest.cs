using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The compliance-admin-gated governed command that submits a deletion/erasure request (AC1/AC2/AC4). A structural
/// twin of <see cref="SubmitTenantExportRequest"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write and no destruction on unauthorized scope / invalid command / audit-writer-down.
/// <see cref="DeletionRunId"/> is the stable idempotency/run key (Story 1.5 two-altitude floor).
/// </summary>
public sealed record SubmitDeletionErasureRequest(
    string DeletionRunId,
    string InventorySnapshotId,
    long SourceVersion,
    DeletionErasureRequestSpec RequestSpec,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string ProofFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;
