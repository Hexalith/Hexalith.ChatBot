namespace Hexalith.ChatBot.Server.Gateway;

internal static class ChatBotAuthorizationReasonCodes
{
    public const string AuthenticationDenied = "authentication_denied";
    public const string TenantMissing = "tenant_missing";
    public const string TenantMismatch = "tenant_mismatch";
    public const string AuthorizationDenied = "authorization_denied";
    public const string SafeNotFound = "safe_not_found";
}
