using Aspire.Hosting.ApplicationModel;

namespace Hexalith.ChatBot.IntegrationTests;

internal readonly record struct ReservedEndpoint(ProjectResource Resource, EndpointAnnotation Endpoint);
