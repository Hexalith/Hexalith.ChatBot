using System.Net;
using System.Text;

using Hexalith.EventStore.Client.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests;

/// <summary>Verifies refusals at the real loopback route used by the Memories owner probe.</summary>
public sealed class MemoriesProbeCommandGatewayTests
{
    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"principalId\":\"fixture-principal\"}")]
    [InlineData("{\"principalId\":\"fixture-principal\",\"command\":[]}")]
    [InlineData("{\"principalId\":\"fixture-principal\",\"command\":{\"tenant\":\"tenant-alpha\",\"domain\":\"memories-tenants\",\"commandType\":\"register-tenant\",\"payload\":{\"tenantId\":\"tenant-alpha\"}}}")]
    [InlineData("{\"principalId\":\"fixture-principal\",\"command\":{\"tenant\":\"tenant-alpha\",\"domain\":\"memories-tenants\",\"commandType\":\"register-tenant\",\"messageId\":\"fixture-message\",\"payload\":[]}}")]
    public async Task MalformedBoundCommandIsForbiddenBeforeForwarding(string body)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MemoriesProbeCommandGateway gateway = await MemoriesProbeCommandGateway.StartAsync(cancellationToken).ConfigureAwait(true);
        await using var gatewayLifetime = gateway.ConfigureAwait(true);
        using HttpClient client = new() { BaseAddress = new Uri(gateway.BaseAddress) };
        using HttpRequestMessage request = new(HttpMethod.Post, "api/v1/commands/bound/workload")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(BoundWorkloadCommandProof.HeaderName, "fixture-proof");
        request.Headers.Add("X-Hexalith-Workload-Assertion", "fixture-assertion");

        using HttpResponseMessage response = await client.SendAsync(request, cancellationToken).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        gateway.LastStatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
        gateway.AcceptedCommands.ShouldBe(0);
    }
}
