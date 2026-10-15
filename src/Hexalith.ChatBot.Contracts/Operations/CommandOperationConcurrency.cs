using System.Text.Json.Serialization;

using Hexalith.ChatBot.Contracts.Serialization;

namespace Hexalith.ChatBot.Contracts;

/// <summary>The explicit revision guard and optional occupied decision-slot identity.</summary>
[JsonConverter(typeof(CommandOperationConcurrencyJsonConverter))]
public sealed record CommandOperationConcurrency
{
    /// <summary>Creates a guard. Missing and negative revisions are invalid even during replay.</summary>
    public CommandOperationConcurrency(long? expectedRevision, string? decisionSlotId = null)
    {
        if (expectedRevision is null or < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        }
        if (decisionSlotId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(decisionSlotId);
        }
        ExpectedRevision = expectedRevision.Value;
        DecisionSlotId = decisionSlotId;
    }

    /// <summary>Gets the expected authoritative revision.</summary>
    public long ExpectedRevision { get; }

    /// <summary>Gets the optional stable decision slot.</summary>
    public string? DecisionSlotId { get; }
}
