namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Application-observed fault transition with a stable reason token.</summary>
internal sealed record RecoveryFaultObservation(DateTimeOffset ObservedAtUtc, string ReasonCode);
