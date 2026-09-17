using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Gateway.Stages;

/// <summary>
/// Safe, finite capacity-impact observation emitted at the send seam when a rate-limit budget applies to an outbound
/// channel (Story 7.26, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window
/// admitted-send count, and whether this send was throttled — mirroring the Story 7.23
/// <c>CommandCapabilityRateLimitObservation</c> shape. The throttled count is the backlog/degradation signal (sends
/// held back to keep external volume within tenant policy). This is the audit/observable seam only; full Epic-8
/// operational-dashboard wiring (and the runtime emission of this observation, deferred together with the read-side)
/// is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the outbound channel.</param>
/// <param name="ObservedWindowCount">The channel's admitted-send count in the trailing window at decision time.</param>
/// <param name="Throttled">Whether this send was throttled (rejected) because the budget was reached.</param>
internal sealed record OutboundChannelRateLimitObservation(int Budget, int ObservedWindowCount, bool Throttled);
