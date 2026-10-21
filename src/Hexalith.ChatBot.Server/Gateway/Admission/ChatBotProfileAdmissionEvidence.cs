using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Current owner-validated profile proofs, bound to one operation and unavailable by default.</summary>
/// <param name="TenantId">Trusted tenant.</param>
/// <param name="ActorId">Currently authorized requester.</param>
/// <param name="CommandType">Exact concrete contract.</param>
/// <param name="OperationId">Stable operation identity.</param>
/// <param name="Profile">The catalog profile the owner verified.</param>
/// <param name="CurrentRevision">Owner-observed current revision.</param>
/// <param name="OwnerConcurrencyGuardReference">Accepted A13 owner concurrency proof, when expected revision is absent.</param>
/// <param name="StageReferences">Independently validated immutable references for every required profile guard.</param>
/// <param name="ObservedAt">Owner evidence observation time.</param>
/// <param name="ExpiresAt">Owner evidence expiry.</param>
/// <param name="Policy">Current exact tenant policy evidence, for AI profiles.</param>
/// <param name="ContextPackage">Current owner-authorized Project context, for AI reads.</param>
/// <param name="Approval">Current independent human approval evidence, for effect execution.</param>
internal sealed record ChatBotProfileAdmissionEvidence(
    string TenantId,
    string ActorId,
    string CommandType,
    string OperationId,
    ChatBotAdmissionProfile Profile,
    long CurrentRevision,
    string? OwnerConcurrencyGuardReference,
    IReadOnlyDictionary<string, string> StageReferences,
    DateTimeOffset ObservedAt,
    DateTimeOffset ExpiresAt,
    TenantAiPolicySnapshot? Policy = null,
    ProjectAiContextPackage? ContextPackage = null,
    ChatBotHumanApprovalEvidence? Approval = null);
