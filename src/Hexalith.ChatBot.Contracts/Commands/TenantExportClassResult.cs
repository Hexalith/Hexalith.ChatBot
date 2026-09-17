using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The per-data-class export decision (AC1/AC2/AC3). Every field is a bounded, <c>AuditMetadata</c>-safe token.
/// <see cref="ArtifactFingerprint"/> is a <c>sha256:</c> token over the produced projection (never raw bytes) and
/// is empty for any class that is not a <c>succeeded</c> includable class (the no-partial-exposure floor).
/// </summary>
public sealed record TenantExportClassResult(
    string DataClassId,
    string ExportEligibility,
    string Disposition,
    string ExclusionReason,
    string RedactionDecision,
    string Status,
    string OwnerRole,
    string ArtifactFingerprint);
