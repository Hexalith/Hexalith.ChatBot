using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotRiskClassification(
    AiActionRiskClassificationRecord? Record = null,
    bool Rejected = false,
    string? RejectionReasonCode = null)
{
    public static ChatBotRiskClassification PassThrough { get; } = new();

    public static ChatBotRiskClassification Classified(AiActionRiskClassificationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new ChatBotRiskClassification(record, record.Rejected, record.ReasonCode);
    }
}
