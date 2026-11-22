namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>Source coverage scoped to the queue. Versions are reported only for a single producing stream.</summary>
public sealed record ReviewQueueCoverage(string Scope, string Reason, long? KnownSourceVersion, string OwnerRole, string NextAction);
