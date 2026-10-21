using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotRiskClassification(
    AiActionRiskClassificationRecord? Record = null,
    bool Rejected = false,
    string? RejectionReasonCode = null)
{
    /// <summary>Terminal classification failure; it cannot enter proposal or approval state.</summary>
    public static ChatBotRiskClassification Indeterminate { get; } = new(null, true, Contracts.Messages.ChatBotMessageCodes.ClassifierIndeterminate);

    public static ChatBotRiskClassification PassThrough { get; } = new();

    public static ChatBotRiskClassification Classified(AiActionRiskClassificationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new ChatBotRiskClassification(record, record.Rejected || record.IndeterminateReason is not null,
            record.IndeterminateReason is not null ? Contracts.Messages.ChatBotMessageCodes.ClassifierIndeterminate : record.ReasonCode);
    }
}
