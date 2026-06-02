using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts.Enums;

[JsonConverter(typeof(JsonEnumMemberStringConverter<AssociationReasonCode>))]
public enum AssociationReasonCode
{
    [EnumMember(Value = "explicit-project-identifier-matched")]
    ExplicitProjectIdentifierMatched,

    [EnumMember(Value = "mailbox-routing-rule-matched")]
    MailboxRoutingRuleMatched,

    [EnumMember(Value = "conversation-thread-matched")]
    ConversationThreadMatched,

    [EnumMember(Value = "required-evidence-present")]
    RequiredEvidencePresent,

    [EnumMember(Value = "missing-required-evidence")]
    MissingRequiredEvidence,

    [EnumMember(Value = "conflicting-deterministic-evidence")]
    ConflictingDeterministicEvidence,

    [EnumMember(Value = "no-authorized-candidate")]
    NoAuthorizedCandidate,

    [EnumMember(Value = "multiple-authorized-candidates")]
    MultipleAuthorizedCandidates,

    [EnumMember(Value = "authorization-evidence-unavailable")]
    AuthorizationEvidenceUnavailable,

    [EnumMember(Value = "unauthorized-candidate-suppressed")]
    UnauthorizedCandidateSuppressed,

    [EnumMember(Value = "scorer-error")]
    ScorerError,

    [EnumMember(Value = "external-sender-strict-review")]
    ExternalSenderStrictReview,

    [EnumMember(Value = "external-sender-paranoid-fail-closed")]
    ExternalSenderParanoidFailClosed,

    [EnumMember(Value = "authenticity-strict-review")]
    AuthenticityStrictReview,

    [EnumMember(Value = "authenticity-paranoid-fail-closed")]
    AuthenticityParanoidFailClosed,

    [EnumMember(Value = "authenticity-strictness-policy-unavailable")]
    AuthenticityStrictnessPolicyUnavailable,

    [EnumMember(Value = "authenticity-strictness-policy-invalid")]
    AuthenticityStrictnessPolicyInvalid,
}
