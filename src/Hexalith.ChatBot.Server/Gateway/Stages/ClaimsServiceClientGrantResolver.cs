using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed class ClaimsServiceClientGrantResolver(ChatBotRequestAuthorizer? authorizer = null) : IServiceClientGrantResolver
{
    public const string ServiceClientIdClaim = "chatbot:service-client-id";
    public const string ServiceClientClassClaim = "chatbot:service-client-class";
    public const string GrantIdClaim = "chatbot:service-client-grant-id";
    public const string GrantTenantClaim = "chatbot:service-client-grant-tenant";
    public const string GrantExpiryClaim = "chatbot:service-client-grant-expiry";
    public const string GrantRevokedClaim = "chatbot:service-client-grant-revoked";
    public const string GrantScopeClaim = "chatbot:service-client-scope";
    public const string GrantCommandClaim = "chatbot:service-client-command";
    public const string GrantQueryClaim = "chatbot:service-client-query";
    public const string GrantSurfaceClaim = "chatbot:service-client-surface";
    public const string DelegatedUserIdClaim = "chatbot:delegated-user-id";
    public const string OAuthGrantEvidenceFingerprintClaim = "chatbot:oauth-grant-fingerprint";
    public const string CommandSetVersionClaim = "chatbot:service-client-command-set-version";

    /// <inheritdoc/>
    public async ValueTask<ServiceClientGrantResolution> ResolveAsync(ChatBotCommandSubmission submission, ChatBotAuthenticatedActor actor, ChatBotTenantBinding tenantBinding, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(tenantBinding);
        if (authorizer is null || actor.RequestContext is not { } context || context.TenantId != tenantBinding.TenantId)
        {
            return ServiceClientGrantResolution.Denied(ChatBotAuthorizationReasonCodes.ServiceClientGrantMissing);
        }

        return await authorizer.ResolveServiceGrantResolutionAsync(context, submission.Request.CommandType ?? string.Empty, false, true, cancellationToken).ConfigureAwait(false);
    }
}
