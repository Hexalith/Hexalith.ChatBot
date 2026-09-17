using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to a
/// command capability (Story 7.23, AC6). Carries integer-only tokens — the effective budget, the observed
/// trailing-window admitted-command count, and whether this submission was throttled — mirroring the Story 7.20
/// <c>AiActorRateLimitObservation</c> shape. The throttled count is the backlog/degradation signal (commands held
/// back to protect the tenant workflow). This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring (and the runtime emission of this observation, deferred together with the read-side) is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the command type.</param>
/// <param name="ObservedWindowCount">The command type's admitted-command count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this submission was throttled (denied) because the budget was reached.</param>
internal sealed record CommandCapabilityRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);
