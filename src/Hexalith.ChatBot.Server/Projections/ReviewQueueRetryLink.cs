namespace Hexalith.ChatBot.Server.Projections;

/// <summary>Joins the failure and authorized retry in either delivery order.</summary>
internal sealed record ReviewQueueRetryLink(ReviewQueueTransition? Failure, ReviewQueueRetryRequest[] Requests);
