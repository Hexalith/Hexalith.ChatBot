using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Observability;

namespace Hexalith.ChatBot.Server.Gateway;

internal enum ChatBotCommandAdmissionDecisionKind
{
    Accepted,
    Rejected,
    ReplayPriorOutcome,
}
