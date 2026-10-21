using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>Owner approval and current independent authority, presence, lifetime and exact drift binding.</summary>
/// <param name="Request">Owner's frozen proposal scope.</param>
/// <param name="Decision">Owner's human decision.</param>
/// <param name="Approver">Current operation-scoped independent approver authority.</param>
/// <param name="PresenceReference">Independently recorded human user-presence evidence.</param>
/// <param name="ExpiresAt">Immutable approval expiry.</param>
/// <param name="ScopeDigest">Approved proposal/content/resource/recipient/tool/target/policy/retention digest.</param>
/// <param name="CurrentScopeDigest">Current owner-computed digest over that same exact scope.</param>
/// <param name="ClassifierVersion">Classifier artifact approved in this decision.</param>
internal sealed record ChatBotHumanApprovalEvidence(
    AiActionApprovalRequested Request,
    AiActionApprovalDecisionRecorded Decision,
    ChatBotAuthorityPrincipal Approver,
    string PresenceReference,
    DateTimeOffset ExpiresAt,
    string ScopeDigest,
    string CurrentScopeDigest,
    string ClassifierVersion);
