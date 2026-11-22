using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>Open work for the current project and one queue family.</summary>
public sealed record GetProjectReviewQueue(string ProjectId, OperationalQueueFamily QueueFamily, string? SourceStreamId = null);
