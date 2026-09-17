using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>The result of a rubber-stamp-rate evaluation pass: how many tenant snapshots were evaluated, how many fired
/// the FR41 tuning revisit (and were durably recorded), and how many fired but were suppressed fail-closed.</summary>
internal sealed record ApprovalRubberStampRateOutcome(int Evaluated, int Triggered, int AuditUnavailable);
