using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Governance.AiMediation;

internal sealed record TaskIntentDetectionResult(
    TaskIntentState State,
    string ReasonCode,
    TaskIntentRecord? Record,
    string SafeMessageCode);
