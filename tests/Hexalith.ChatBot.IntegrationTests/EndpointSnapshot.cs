namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Captures every mutable endpoint annotation value so topology isolation tests can prove unrelated endpoints are unchanged.
/// </summary>
/// <param name="ResourceName">The Aspire resource name.</param>
/// <param name="ResourceType">The concrete Aspire resource type.</param>
/// <param name="EndpointIndex">The endpoint annotation index on the resource.</param>
/// <param name="EndpointName">The endpoint name.</param>
/// <param name="Protocol">The transport protocol.</param>
/// <param name="UriScheme">The endpoint URI scheme.</param>
/// <param name="Transport">The optional endpoint transport.</param>
/// <param name="Port">The host port.</param>
/// <param name="TargetPort">The resource target port.</param>
/// <param name="IsExternal">Whether the endpoint is externally exposed.</param>
/// <param name="IsProxied">Whether the endpoint uses the Aspire proxy.</param>
/// <param name="IsExplicitlyProxied">Whether proxying was explicitly configured.</param>
/// <param name="TargetHost">The endpoint target host.</param>
/// <param name="TlsEnabled">Whether TLS is enabled.</param>
/// <param name="ExcludeReferenceEndpoint">Whether the endpoint is excluded from reference expressions.</param>
internal readonly record struct EndpointSnapshot(
    string ResourceName,
    string ResourceType,
    int EndpointIndex,
    string EndpointName,
    string Protocol,
    string UriScheme,
    string? Transport,
    int? Port,
    int? TargetPort,
    bool IsExternal,
    bool IsProxied,
    bool? IsExplicitlyProxied,
    string? TargetHost,
    bool TlsEnabled,
    bool ExcludeReferenceEndpoint);
