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
    public const string AssociationCorrectionTargetUnauthorized = "association_correction_target_unauthorized";
    public const string AssociationCorrectionProjectionUnavailable = "association_correction_projection_unavailable";
}
