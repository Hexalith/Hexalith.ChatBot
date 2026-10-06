using System.Text.Json;

using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.EventStore.Client.Gateway;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class EventStoreDefinitiveRefusalTests
{
    private const string DomainRejectionType = "https://hexalith.io/problems/domain-rejections/governed-note-rejected";

    [Theory]
    [InlineData(422)]
    [InlineData(409)]
    [InlineData(404)]
    public void TypedDomainRejectionShouldBeDefinitive(int status)
        => EventStoreDefinitiveRefusal.IsDefinitive(Rejection(status, DomainRejectionType, "Hexalith.ChatBot.GovernedNoteRejected"))
            .ShouldBeTrue();

    [Fact]
    public void BackpressureShouldBeDefinitive()
        => EventStoreDefinitiveRefusal.IsDefinitive(new EventStoreGatewayException(
            429, "Too Many Requests", type: "https://hexalith.io/problems/backpressure-exceeded")).ShouldBeTrue();

    [Theory]
    [InlineData(422, DomainRejectionType, null)]
    [InlineData(422, DomainRejectionType, "")]
    [InlineData(422, "https://hexalith.io/problems/validation-error", "Hexalith.ChatBot.GovernedNoteRejected")]
    [InlineData(400, DomainRejectionType, "Hexalith.ChatBot.GovernedNoteRejected")]
    [InlineData(503, DomainRejectionType, "Hexalith.ChatBot.GovernedNoteRejected")]
    [InlineData(409, "https://hexalith.io/problems/idempotency-admission-failure", null)]
    [InlineData(409, "https://hexalith.io/problems/command-identity-conflict", null)]
    [InlineData(409, "https://hexalith.io/problems/concurrency-conflict", null)]
    [InlineData(429, "https://hexalith.io/problems/rate-limit-exceeded", null)]
    [InlineData(429, null, null)]
    [InlineData(503, null, null)]
    public void UnprovenGatewayFailureShouldNotBeDefinitive(int status, string? type, string? rejectionType)
        => EventStoreDefinitiveRefusal.IsDefinitive(Rejection(status, type, rejectionType)).ShouldBeFalse();

    [Fact]
    public void NonGatewayFailureShouldNotBeDefinitive()
    {
        EventStoreDefinitiveRefusal.IsDefinitive(new HttpRequestException("Injected sidecar outage.")).ShouldBeFalse();
        EventStoreDefinitiveRefusal.IsDefinitive(new InvalidOperationException("Injected failure.")).ShouldBeFalse();
        EventStoreDefinitiveRefusal.IsDefinitive(new TaskCanceledException("Injected time-out.")).ShouldBeFalse();
    }

    private static EventStoreGatewayException Rejection(int status, string? type, string? rejectionType)
        => new(
            status,
            "Refused",
            type: type,
            extensions: rejectionType is null
                ? null
                : new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["rejectionType"] = JsonSerializer.SerializeToElement(rejectionType),
                });
}
