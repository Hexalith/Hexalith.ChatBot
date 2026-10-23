using System.Text.Json;

using Hexalith.ChatBot.RecoverySandbox;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Executable guards for the sandbox's closed route, request-header and restore contracts.</summary>
public sealed class RecoverySandboxBoundaryTests
{
    [Theory]
    [InlineData("fault")]
    [InlineData("restore")]
    [InlineData("status")]
    public void NonProcessRoutesRejectCorrelationLocators(string action)
    {
        RecoverySandboxRoute.ScopedOutage("tenant", "graph", action)
            .ShouldBe($"/recovery/tenant/scoped-outage/graph/{action}");
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRoute.ScopedOutage("tenant", "graph", action, "correlation"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ProcessRoutesRequireCorrelationLocators(string? correlation)
        => Should.Throw<InvalidOperationException>(() => RecoverySandboxRoute.ScopedOutage("tenant", "graph", "process", correlation));

    [Fact]
    public void RoutesEscapeSegmentsAndRejectUnknownActions()
    {
        RecoverySandboxRoute.ScopedOutage("replay-test:a/b", "graph?query", "process", "a/b#c")
            .ShouldBe("/recovery/replay-test%3Aa%2Fb/scoped-outage/graph%3Fquery/process/a%2Fb%23c");
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRoute.ScopedOutage("tenant", "graph", "delete"));
    }

    [Theory]
    [InlineData("continuity", "checkpoint")]
    [InlineData("continuity", "recovery")]
    [InlineData("graph", "recovery")]
    [InlineData("controlled-loss", "pre-fault")]
    [InlineData("controlled-loss", "loss")]
    [InlineData("controlled-loss", "post-recovery")]
    public void HeadersComposeOnlyTheSelectedLaneAndPhase(string lane, string phase)
    {
        HeaderDictionary headers = Headers(lane, phase);
        RecoveryNotificationIdentity.TryComposeFromHeaders(headers, "provider-message", out string? identity).ShouldBeTrue();
        identity.ShouldBe($"provider-message-{lane}-{phase}");
    }

    [Theory]
    [InlineData(null, "recovery")]
    [InlineData("", "recovery")]
    [InlineData("unknown", "recovery")]
    [InlineData("graph", null)]
    [InlineData("graph", "")]
    [InlineData("graph", "checkpoint")]
    [InlineData("continuity", "loss")]
    [InlineData("controlled-loss", "recovery")]
    [InlineData("graph", "unknown")]
    public void MissingUnknownOrCrossLaneHeadersAreRefused(string? lane, string? phase)
    {
        RecoveryNotificationIdentity.TryComposeFromHeaders(Headers(lane, phase), "provider-message", out string? identity).ShouldBeFalse();
        identity.ShouldBeNull();
    }

    [Theory]
    [InlineData(RecoveryNotificationIdentity.ScenarioLaneHeaderName)]
    [InlineData(RecoveryNotificationIdentity.HeaderName)]
    public void RepeatedHeadersAreRefusedEvenWhenTheirValuesAgree(string header)
    {
        HeaderDictionary headers = Headers("graph", "recovery");
        string value = headers[header].ToString();
        headers[header] = new StringValues([value, value]);
        RecoveryNotificationIdentity.TryComposeFromHeaders(headers, "provider-message", out string? identity).ShouldBeFalse();
        identity.ShouldBeNull();
    }

    [Fact]
    public void RestoreReadersRejectFlatOrMissingSnapshots()
    {
        using JsonDocument flat = JsonDocument.Parse("{\"faulted\":false}");
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRestoreResponse.IsCurrentlyFaulted(flat.RootElement));
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRestoreResponse.WasPreviouslyFaulted(flat.RootElement));
        using JsonDocument currentOnly = JsonDocument.Parse("{\"current\":{\"faulted\":false}}");
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRestoreResponse.WasPreviouslyFaulted(currentOnly.RootElement));
        using JsonDocument priorOnly = JsonDocument.Parse("{\"prior\":{\"faulted\":true}}");
        Should.Throw<InvalidOperationException>(() => RecoverySandboxRestoreResponse.IsCurrentlyFaulted(priorOnly.RootElement));
    }

    private static HeaderDictionary Headers(string? lane, string? phase)
    {
        HeaderDictionary headers = new();
        if (lane is not null)
        {
            headers[RecoveryNotificationIdentity.ScenarioLaneHeaderName] = lane;
        }

        if (phase is not null)
        {
            headers[RecoveryNotificationIdentity.HeaderName] = phase;
        }

        return headers;
    }
}
