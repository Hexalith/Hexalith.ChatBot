using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a replay-isolation sweep: how many production tenants were swept, breached, and alerted.</summary>
internal sealed record ReplayIsolationProbeOutcome(int TenantsSwept, int Breaches, int Alerted);
