using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Server.Gateway;

internal static class ChatBotAuthorizationReasonCodes
{
    public const string AuthenticationDenied = "authentication_denied";
    public const string TenantMissing = "tenant_missing";
    public const string TenantMismatch = "tenant_mismatch";
    public const string AuthorizationDenied = "authorization_denied";
    public const string SafeNotFound = "safe_not_found";
    public const string CommandNotAllowlisted = ChatBotRefusalReasonCodes.CommandNotAllowlisted;
    public const string UnresolvedParticipant = "unresolved_participant";
    public const string UnauthorizedParticipant = "unauthorized_participant";
    public const string ParticipantDirectoryDegraded = "participant_directory_degraded";
    public const string ThresholdPolicyUnauthorized = "threshold_policy_unauthorized";
    public const string NotificationRoutingUnauthorized = "notification_routing_unauthorized";
    public const string EscalationPolicyUnauthorized = "escalation_policy_unauthorized";
    public const string AssociationCorrectionTargetUnauthorized = "association_correction_target_unauthorized";
    public const string AssociationCorrectionProjectionUnavailable = "association_correction_projection_unavailable";
    public const string ServiceClientGrantMissing = "service_client_grant_missing";
    public const string ServiceClientGrantAmbiguous = "service_client_grant_ambiguous";
    public const string ServiceClientGrantExpired = "service_client_grant_expired";
    public const string ServiceClientGrantRevoked = "service_client_grant_revoked";
    public const string ServiceClientGrantOverScoped = "service_client_grant_over_scoped";
    public const string ServiceClientGrantUnderScoped = "service_client_grant_under_scoped";
    public const string ServiceClientGrantTenantMismatch = "service_client_grant_tenant_mismatch";
    public const string ServiceClientWrongSurface = "service_client_wrong_surface";

    // FR74 two-person governance control state — distinct from the Epic 5 Keycloak-sourced grant revocation
    // (ServiceClientGrantRevoked). Set only through the SubmitServiceClientDisable→ApproveServiceClientDisable
    // two-person path; reflects the ChatBot-domain disabled control state, never the external grant flag.
    public const string ServiceClientDisabled = "service_client_disabled";

    // FR74 two-person AI-actor governance control state — a dedicated AI-actor control plane. Distinct from the
    // service-client disabled control (ServiceClientDisabled) and from the Epic 5 Keycloak-sourced grant revocation
    // (ServiceClientGrantRevoked), even though an AI actor shares the ServiceClientId space and the
    // ServiceClientGrantValidator seam with service clients (distinguished only by the actor_type claim). Set only
    // through the SubmitAiActorDisable→ApproveAiActorDisable two-person path; blocks future AI proposals and commands.
    public const string AiActorDisabled = "ai_actor_disabled";

    // FR74 two-person AI-actor governance control state — contained-for-review. A dedicated AI-actor control plane,
    // distinct from the same-family AI-actor disabled control (AiActorDisabled), the service-client quarantine
    // (ServiceClientQuarantined, a different subject class), and the Epic 5 Keycloak-sourced grant revocation
    // (ServiceClientGrantRevoked). Set only through the SubmitAiActorQuarantine→ApproveAiActorQuarantine two-person
    // path; blocks future AI proposals and commands while existing records stay auditable.
    public const string AiActorQuarantined = "ai_actor_quarantined";

    // FR74 two-person governance control state — contained-for-review. Distinct from both the Epic 5 Keycloak-sourced
    // grant revocation (ServiceClientGrantRevoked) and the same-family disabled control state (ServiceClientDisabled).
    // Set only through the SubmitServiceClientQuarantine→ApproveServiceClientQuarantine two-person path.
    public const string ServiceClientQuarantined = "service_client_quarantined";

    // FR74/FR75 single-actor standard policy mutation — a transient command-admission throttle. Distinct from the
    // terminal control states (ServiceClientDisabled/ServiceClientQuarantined) and from the Epic 5 grant lifecycle
    // codes (ServiceClientGrantRevoked/ServiceClientGrantOverScoped). Returned as the final admission gate at
    // ServiceClientGrantValidator when the client's trailing-window admitted-command count reaches its bounded budget.
    public const string ServiceClientRateLimited = "service_client_rate_limited";

    // FR74/FR75 single-actor standard policy mutation — a transient AI-actor proposal-admission throttle (Story 7.20).
    // A dedicated AI-actor plane, distinct from the terminal AI-actor control states (AiActorDisabled/AiActorQuarantined),
    // the service-client rate-limit (ServiceClientRateLimited, a different subject class), and the Epic 5 grant lifecycle
    // codes (ServiceClientGrantRevoked/ServiceClientGrantOverScoped). Returned as the final admission gate at
    // ServiceClientGrantValidator (branched on the `ai` actor type) when the AI actor's trailing-window admitted-proposal
    // count reaches its bounded budget. The throttled proposal never reaches the downstream AiActionApprovalGate.
    public const string AiActorRateLimited = "ai_actor_rate_limited";
}
