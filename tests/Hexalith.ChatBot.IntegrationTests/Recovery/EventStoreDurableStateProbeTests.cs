using System.Net;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Fail-closed actor-state and polling-boundary tests for the live recovery durable probe.</summary>
public sealed class EventStoreDurableStateProbeTests
{
    private static readonly Uri Endpoint = new("http://eventstore-dapr.test");

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task SuccessfulMalformedActorStateNeverMeansAbsent(string body)
    {
        using SequenceHttpMessageHandler handler = new((_, _) => SequenceHttpMessageHandler.Json(body));
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(5));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => probe.IsMailboxIntakeCommittedAsync(
            "recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AuthorizationFailureNeverMeansAbsent()
    {
        using SequenceHttpMessageHandler handler = new((_, _) => new HttpResponseMessage(HttpStatusCode.Forbidden));
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(5));

        _ = await Should.ThrowAsync<InvalidOperationException>(() => probe.IsMailboxIntakeCommittedAsync(
            "recovery-validation",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PresenceWaitToleratesATransientInconsistentReadThenSucceeds()
    {
        const string tenant = "recovery-validation";
        const string intake = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        using SequenceHttpMessageHandler handler = new((requestNumber, _) => requestNumber switch
        {
            // First commit-check attempt: metadata is present, but the event-content read disagrees — a transient
            // artifact of the two actor-state keys committing via separate writes.
            1 => SequenceHttpMessageHandler.Json("{\"currentSequence\":1}"),
            2 => SequenceHttpMessageHandler.Json(
                "{\"tenantId\":\"tenant-other\",\"domain\":\"chatbot\",\"aggregateId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\",\"eventTypeName\":\"Hexalith.ChatBot.Server.Association.Intake.MailboxMessageIntakeCaptured\"}"),
            // Second attempt: fully consistent.
            3 => SequenceHttpMessageHandler.Json("{\"currentSequence\":1}"),
            _ => SequenceHttpMessageHandler.Json(
                "{\"tenantId\":\"recovery-validation\",\"domain\":\"chatbot\",\"aggregateId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\",\"eventTypeName\":\"Hexalith.ChatBot.Server.Association.Intake.MailboxMessageIntakeCaptured\"}"),
        });
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(20));

        await probe.WaitForMailboxIntakeAsync(tenant, intake, TestContext.Current.CancellationToken);

        handler.Requests.ShouldBeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task PresenceWaitPerformsFinalReadAfterBoundaryDelay()
    {
        const string tenant = "recovery-validation";
        const string intake = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        using SequenceHttpMessageHandler handler = new((requestNumber, _) => requestNumber switch
        {
            // The one in-loop poll observes absence. The commit lands only after that poll's delay elapses; the
            // presence window itself is shorter than the delay, so only the final closing read can observe it.
            1 => new HttpResponseMessage(HttpStatusCode.NotFound),
            2 => SequenceHttpMessageHandler.Json("{\"currentSequence\":1}"),
            _ => SequenceHttpMessageHandler.Json(
                "{\"tenantId\":\"recovery-validation\",\"domain\":\"chatbot\",\"aggregateId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\",\"eventTypeName\":\"Hexalith.ChatBot.Server.Association.Intake.MailboxMessageIntakeCaptured\"}"),
        });
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(20));

        await probe.WaitForMailboxIntakeAsync(tenant, intake, TestContext.Current.CancellationToken);

        handler.Requests.ShouldBe(3);
    }

    [Fact]
    public async Task PresenceWaitsFinalReadIsNotToleratedFailsClosedWithTheRealDiagnostic()
    {
        const string tenant = "recovery-validation";
        const string intake = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        using SequenceHttpMessageHandler handler = new((requestNumber, _) => requestNumber switch
        {
            // The in-loop poll observes absence; the closing read after the boundary delay hits a persistent
            // metadata/event-content mismatch. Unlike a mid-window poll, this must fail closed with the underlying
            // InvalidOperationException instead of being reinterpreted as "still absent" and swallowed into a
            // generic TimeoutException.
            1 => new HttpResponseMessage(HttpStatusCode.NotFound),
            2 => SequenceHttpMessageHandler.Json("{\"currentSequence\":1}"),
            _ => SequenceHttpMessageHandler.Json(
                "{\"tenantId\":\"tenant-other\",\"domain\":\"chatbot\",\"aggregateId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\",\"eventTypeName\":\"Hexalith.ChatBot.Server.Association.Intake.MailboxMessageIntakeCaptured\"}"),
        });
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(20));

        // A bug that reverts to swallowing the closing read's failure would surface this as a bare TimeoutException
        // instead, which ThrowAsync<InvalidOperationException> below would correctly reject.
        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => probe.WaitForMailboxIntakeAsync(tenant, intake, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AbsenceWindowPerformsFinalReadAfterBoundaryDelay()
    {
        const string tenant = "recovery-validation";
        const string intake = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        using SequenceHttpMessageHandler handler = new((requestNumber, _) => requestNumber switch
        {
            1 => new HttpResponseMessage(HttpStatusCode.NotFound),
            2 => SequenceHttpMessageHandler.Json("{\"currentSequence\":1}"),
            _ => SequenceHttpMessageHandler.Json(
                "{\"tenantId\":\"recovery-validation\",\"domain\":\"chatbot\",\"aggregateId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAW\",\"eventTypeName\":\"Hexalith.ChatBot.Server.Association.Intake.MailboxMessageIntakeCaptured\"}"),
        });
        using EventStoreDurableStateProbe probe = new(
            Endpoint,
            handler,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(20));

        bool absent = await probe.RemainsAbsentAsync(
            tenant,
            intake,
            TimeSpan.FromMilliseconds(1),
            TestContext.Current.CancellationToken);

        absent.ShouldBeFalse();
        handler.Requests.ShouldBe(3);
    }
}
