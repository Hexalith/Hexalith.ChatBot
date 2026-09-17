using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a chain-verification pass: how many tenant chains were checked, breached, and alerted.</summary>
internal sealed record AuditChainVerificationOutcome(int TenantsChecked, int Breaches, int Alerted);
