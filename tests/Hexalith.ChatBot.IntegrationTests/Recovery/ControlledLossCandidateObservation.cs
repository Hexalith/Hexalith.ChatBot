namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Safe candidate identity exposed before the sandbox rejects the controlled dependency call.</summary>
internal sealed record ControlledLossCandidateObservation(
    string CandidateRef,
    DateTimeOffset ObservedAtUtc,
    bool Rejected);
