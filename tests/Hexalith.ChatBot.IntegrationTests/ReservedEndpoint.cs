using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>
/// Associates an exact sidecar-backed project resource with the HTTP endpoint selected for isolated port assignment.
/// </summary>
/// <param name="Resource">The selected project resource.</param>
/// <param name="Endpoint">The selected HTTP endpoint annotation.</param>
internal readonly record struct ReservedEndpoint(ProjectResource Resource, EndpointAnnotation Endpoint);
