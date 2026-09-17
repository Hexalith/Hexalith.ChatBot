using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Workers.Mailbox;

/// <summary>
/// Safe, finite queue-impact observation emitted by the worker when a rate-limit budget applies to a mailbox source
/// (Story 7.14, AC6). Carries integer-only tokens — the effective budget, the observed trailing-window count, and
/// whether this message was deferred — mirroring the Story 7.9 <c>NotificationThrottleOutcome</c> shape. This is the
/// audit/observable seam only; full Epic-8 operational-dashboard wiring is out of scope.
/// </summary>
/// <param name="Budget">The effective in-bounds per-window budget for the source.</param>
/// <param name="ObservedWindowCount">The source's intake count in the trailing window at decision time.</param>
/// <param name="Deferred">Whether this message was deferred (throttled) because the budget was reached.</param>
public sealed record MailboxRateLimitObservation(int Budget, int ObservedWindowCount, bool Deferred);
