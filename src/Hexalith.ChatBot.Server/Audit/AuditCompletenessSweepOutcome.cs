namespace Hexalith.ChatBot.Server.Audit;

/// <summary>The result of a completeness sweep across every tenant chain: how many tenants were measured, breached the budget, and were unmeasurable.</summary>
internal sealed record AuditCompletenessSweepOutcome(int TenantsMeasured, int Breaches, int Unmeasurable);
