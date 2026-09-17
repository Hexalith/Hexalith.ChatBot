using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The correlation-stamped result of a deletion/erasure run (AC1/AC4/AC5). <see cref="DeletionRunId"/> is the stable
/// idempotency/run key. <see cref="Proof"/> seals exactly the <c>succeeded</c> destructive classes — a
/// failed/retained class contributes no proof entry.
/// </summary>
public sealed record DeletionErasureRunResult(
    string DeletionRunId,
    string Mode,
    string RunStatus,
    IReadOnlyList<DeletionErasureClassResult> ClassResults,
    ErasureProofArtifact Proof,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);
