namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Validated metadata-only observation read from one persisted EventStore actor event envelope.</summary>
internal sealed record DurableCommitObservation(
    string TenantRef,
    string AggregateRef,
    string EventRef,
    long SequenceNumber,
    DateTimeOffset CommittedAtUtc);
