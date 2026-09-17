using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Queries;

public sealed record OperationalQueueFilter(
    int? MinAgeSeconds = null,
    int? MaxAgeSeconds = null,
    string? Risk = null,
    decimal? MinConfidence = null,
    decimal? MaxConfidence = null,
    string? ProjectRef = null,
    string? MailboxRef = null,
    string? FailureState = null,
    string? AssignedReviewerRef = null,
    string? NextAction = null,
    DateTimeOffset? ChangedAfterUtc = null,
    DateTimeOffset? ChangedBeforeUtc = null);
