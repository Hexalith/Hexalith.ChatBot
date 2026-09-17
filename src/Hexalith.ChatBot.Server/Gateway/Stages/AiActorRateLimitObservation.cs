using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Safe, finite capacity-impact observation emitted at the admission seam when a rate-limit budget applies to an
/// AI actor (Story 7.20, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-proposal count, and whether this proposal was throttled — mirroring the Story 7.17
/// <c>ServiceClientRateLimitObservation</c> shape. The throttled count is the approval-fatigue / backlog signal
/// (proposals held back from reviewers). This is the audit/observable seam only; full Epic-8 operational-dashboard
/// wiring (and the runtime emission of this observation, deferred together with the read-side) is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the AI actor.</param>
/// <param name="ObservedWindowCount">The AI actor's admitted-proposal count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this proposal was throttled (denied) because the budget was reached.</param>
internal sealed record AiActorRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);
