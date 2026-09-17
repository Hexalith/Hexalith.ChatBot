using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The per-data-class deletion/erasure decision (AC1/AC2/AC4). Every field is a bounded, <c>AuditMetadata</c>-safe
/// token. <see cref="ExclusionReason"/> is non-empty only for a <c>retained</c> class. Destruction is fail-closed:
/// an <c>unauthorized</c> class is <c>retained</c>, never destructive.
/// </summary>
public sealed record DeletionErasureClassResult(
    string DataClassId,
    string DeletionBehavior,
    string Action,
    string ExclusionReason,
    string Status,
    string OwnerRole);
