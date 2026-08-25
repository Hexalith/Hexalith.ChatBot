namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// The identity this domain service presents to EventStore's named-projection capability negotiation.
/// </summary>
/// <remarks>
/// <para>
/// EventStore's operational-index refresher posts <c>AppId</c> and <c>ServiceVersion</c> to
/// <c>/admin/operational-index-metadata</c>, and the domain-service SDK answers
/// <c>400 UnsupportedCapability</c> unless both match the service's own identity. The SDK derives an unconfigured
/// <c>AppId</c> from the <c>DAPR_APP_ID</c> environment variable, falling back to
/// <c>IHostEnvironment.ApplicationName</c> — which is <c>Hexalith.ChatBot.Server</c>, never the DAPR app id
/// EventStore registers and invokes this service under. Where that variable is not present in the application
/// process, the identity silently resolves to the assembly name and every refresh is refused.
/// </para>
/// <para>
/// The consequence is not a lost optimisation. Once the store's v2 writer protocol is active, a projection
/// checkpoint may advance only through the named-projection fenced completion, so a permanently refused binding
/// means the checkpoint never advances and the poller re-delivers the same aggregates indefinitely.
/// </para>
/// <para>
/// <see cref="AppId"/> must equal this service's DAPR app id, because that is the id EventStore registers it under
/// and the id it invokes.
/// </para>
/// </remarks>
internal static class ChatBotDomainServiceIdentity
{
    /// <summary>
    /// The DAPR app id this service is registered and invoked under. Pinned rather than inferred, because the
    /// SDK's fallback is the .NET application name and the two are not the same string.
    /// </summary>
    public const string AppId = "chatbot";

    /// <summary>
    /// The deployed service version. Matches EventStore's default for a registration that declares no version.
    /// </summary>
    public const string ServiceVersion = "v1";

    /// <summary>The configuration section that may override the identity without a code change.</summary>
    public const string ConfigurationSection = "ChatBot:ProjectionIdentity";
}
