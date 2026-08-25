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

    /// <summary>
    /// Resolves the app id by precedence: explicit ChatBot configuration, then the SDK's own configuration key,
    /// then the DAPR-supplied app id, and only then the pinned constant.
    /// </summary>
    /// <param name="configured">The <c>ChatBot:ProjectionIdentity:AppId</c> value, if any.</param>
    /// <param name="sdkConfigured">The <c>EventStore:DomainService:AppId</c> value, if any.</param>
    /// <param name="daprAppId">The <c>DAPR_APP_ID</c> environment value, if any.</param>
    /// <returns>The app id to present to EventStore.</returns>
    public static string ResolveAppId(string? configured, string? sdkConfigured, string? daprAppId)
        => FirstUsable(configured, sdkConfigured, daprAppId) ?? AppId;

    /// <summary>Resolves the service version by the same precedence, minus the DAPR-supplied value.</summary>
    /// <param name="configured">The <c>ChatBot:ProjectionIdentity:ServiceVersion</c> value, if any.</param>
    /// <param name="sdkConfigured">The <c>EventStore:DomainService:ServiceVersion</c> value, if any.</param>
    /// <returns>The service version to present to EventStore.</returns>
    public static string ResolveServiceVersion(string? configured, string? sdkConfigured)
        => FirstUsable(configured, sdkConfigured) ?? ServiceVersion;

    /// <summary>
    /// Whether a resolved identity component is usable. EventStore compares these verbatim, so anything that is
    /// not a safe stable identifier can only ever produce a silent capability refusal.
    /// </summary>
    /// <param name="value">The resolved component.</param>
    /// <returns><see langword="true"/> when the component can participate in the comparison.</returns>
    public static bool IsUsableIdentityComponent(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && value.Length <= 128
            && value.All(static c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static string? FirstUsable(params string?[] candidates)
        => candidates.FirstOrDefault(static candidate => !string.IsNullOrWhiteSpace(candidate))?.Trim();
}
