using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a throttle/digest pass: how many deliveries were evaluated, immediately delivered, rolled into a digest, or suppressed fail-closed.</summary>
internal sealed record NotificationThrottleOutcome(int Evaluated, int Delivered, int Throttled, int AuditUnavailable);
