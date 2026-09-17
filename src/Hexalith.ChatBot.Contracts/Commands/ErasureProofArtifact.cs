using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The metadata-only erasure-proof artifact for a completed erasure (AC5, NFR53). <see cref="ProofFingerprint"/> is a
/// deterministic <c>sha256:</c> digest over the confirmation set — a class that did not reach <c>succeeded</c>
/// contributes no entry (the no-partial-proof floor, consistent with AC4).
/// </summary>
public sealed record ErasureProofArtifact(
    string DeletionRunId,
    IReadOnlyList<ErasureProofEntry> Entries,
    string ProofFingerprint,
    DateTimeOffset GeneratedAtUtc,
    string CorrelationId);
