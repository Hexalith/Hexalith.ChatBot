using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The compliance-admin-gated governed command that submits a tenant export request (AC1/AC3/AC4). A structural
/// twin of <see cref="SubmitDataClassInventoryChange"/> — gated at the <c>ParticipantAuthorizationStage</c> by
/// <c>HasHumanAdminScope(.., AdminScope.Compliance)</c>, routed through the one CommandGateway audit-commit spine,
/// fail-closed with no durable write and no exposed artifact on unauthorized scope / invalid command /
/// audit-writer-down. <see cref="ExportRunId"/> is the stable idempotency/run key (Story 1.5 two-altitude floor).
/// </summary>
public sealed record SubmitTenantExportRequest(
    string ExportRunId,
    string InventorySnapshotId,
    long SourceVersion,
    TenantExportRequestSpec RequestSpec,
    string ReasonCode,
    string RequesterRef,
    string SchemaVersion,
    string CorrelationId,
    string PolicySnapshotId,
    string ManifestFingerprint,
    DateTimeOffset EffectiveAtUtc) : IChatBotCommand;
