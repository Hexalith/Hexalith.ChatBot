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

    // FR74 two-person governance control state — contained-for-review. Distinct from both the Epic 5 Keycloak-sourced
    // grant revocation (ServiceClientGrantRevoked) and the same-family disabled control state (ServiceClientDisabled).
    // Set only through the SubmitServiceClientQuarantine→ApproveServiceClientQuarantine two-person path.
    public const string ServiceClientQuarantined = "service_client_quarantined";
}
