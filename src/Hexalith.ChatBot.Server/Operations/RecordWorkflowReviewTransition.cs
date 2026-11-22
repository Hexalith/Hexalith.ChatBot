using System.Text.Json;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>Internal workflow command. Each subject owns one stream and one causal generation per upstream version.</summary>
public sealed record RecordWorkflowReviewTransition(
    OperationalQueueFamily Family, string SubjectId, string ProjectId, long Generation, int Attempt, string State, string Reason,
    WorkflowReviewTarget? Target = null)
{
    internal string StreamId(string tenant) => "review-" + ProjectConversationStableId.Hash(
        JsonSerializer.Serialize(new[] { tenant, OperationalQueueFamilies.ToWireValue(Family), ProjectId, SubjectId })).ToLowerInvariant();

    internal int Stage => State switch { "retrying" => 1, "failed" => 2, "exhausted" or "nonretryable" => 3, "recovered" => 4, _ => 0 };
}
