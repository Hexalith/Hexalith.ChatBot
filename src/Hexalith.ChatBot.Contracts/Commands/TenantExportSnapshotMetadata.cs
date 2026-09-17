using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The NFR35 policy-snapshot metadata for an export run. Mirrors <see cref="DataClassInventorySnapshotMetadata"/>
/// field-for-field, replacing <c>ChangedDataClassIds</c> with <see cref="ExportedDataClassIds"/> and using
/// <see cref="AdminScope.Compliance"/> for <see cref="ScopeUsed"/>. Old/new values are <c>sha256:</c> fingerprints,
/// never raw export bytes.
/// </summary>
public sealed record TenantExportSnapshotMetadata(
    string SnapshotId,
    string SchemaVersion,
    string SupersedesSnapshotId,
    string SupersededBySnapshotId,
    string SourceChangeId,
    string ActorRef,
    AdminScope ScopeUsed,
    IReadOnlyList<string> ExportedDataClassIds,
    long SourceVersion,
    DateTimeOffset EffectiveAtUtc,
    string CorrelationId,
    string ReasonCode,
    string PolicySnapshotId,
    string OldSnapshotFingerprint,
    string NewSnapshotFingerprint);
