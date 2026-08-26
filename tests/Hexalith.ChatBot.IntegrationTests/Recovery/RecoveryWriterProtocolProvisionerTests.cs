using System.Net;
using System.Net.Http.Headers;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Contract tests for the clean-store writer-protocol activation state machine.</summary>
public sealed class RecoveryWriterProtocolProvisionerTests
{
    [Fact]
    public async Task SuccessfulActivationUsesTheAdminBearerAndCutoverProvenance()
    {
        using SequenceHttpMessageHandler handler = new((_, request) =>
        {
            request.Method.ShouldBe(HttpMethod.Post);
            request.RequestUri!.AbsolutePath.ShouldBe("/api/v1/admin/projections/delivery-writer-protocol/activate");
            request.Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("Bearer", "admin-bearer"));
            request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken)
                .GetAwaiter().GetResult().ShouldContain("\"cutoverCommit\":\"abc123\"");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://eventstore.test") };

        await RecoveryWriterProtocolProvisioner.ActivateAsync(
            client,
            "admin-bearer",
            "abc123",
            TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero).ConfigureAwait(true);

        handler.Requests.ShouldBe(1);
    }

    [Fact]
    public async Task ConflictIsAcceptedOnlyAfterHealthProvesTheExistingMarkerIsCurrent()
    {
        using SequenceHttpMessageHandler handler = new((requestNumber, request) => requestNumber switch
        {
            1 => new HttpResponseMessage(HttpStatusCode.Conflict),
            2 when request.RequestUri!.AbsolutePath == "/health" => new HttpResponseMessage(HttpStatusCode.OK),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
        });
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://eventstore.test") };

        await RecoveryWriterProtocolProvisioner.ActivateAsync(
            client,
            "admin-bearer",
            "abc123",
            TestContext.Current.CancellationToken,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero).ConfigureAwait(true);

        handler.Requests.ShouldBe(2);
    }

    [Fact]
    public async Task PermanentlyUnavailableCapabilityFailsWithoutBurningTheRetryBudget()
    {
        using SequenceHttpMessageHandler handler = new((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("The projection delivery cutover capability is not available."),
        });
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://eventstore.test") };

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            RecoveryWriterProtocolProvisioner.ActivateAsync(
                client,
                "admin-bearer",
                "abc123",
                TestContext.Current.CancellationToken,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.Zero)).ConfigureAwait(true);

        exception.Message.ShouldContain("can never be activated");
        handler.Requests.ShouldBe(1);
    }
}
