namespace Hexalith.ChatBot.IntegrationTests;

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
