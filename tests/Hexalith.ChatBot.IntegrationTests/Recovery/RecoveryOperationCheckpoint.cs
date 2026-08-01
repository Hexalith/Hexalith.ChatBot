namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Known committed-operation checkpoint captured before EventStore injection.</summary>
internal sealed record RecoveryOperationCheckpoint(int CommittedCount, DateTimeOffset LastCommittedAtUtc, string OperationRef);
