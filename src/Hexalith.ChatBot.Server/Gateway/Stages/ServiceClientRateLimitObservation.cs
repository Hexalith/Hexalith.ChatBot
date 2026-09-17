using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to a
/// service client (Story 7.17, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-command count, and whether this command was throttled — mirroring the Story 7.14
/// <c>MailboxRateLimitObservation</c> shape. This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the service client.</param>
/// <param name="ObservedWindowCount">The client's admitted-command count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this command was throttled (denied) because the budget was reached.</param>
internal sealed record ServiceClientRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);
