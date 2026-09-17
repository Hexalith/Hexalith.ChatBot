using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.Admin;
using Hexalith.ChatBot.Server.Lifecycle.Retry;

namespace Hexalith.ChatBot.Server.Audit;

/// <summary>
/// Story 9.8 (AC3, NFR17/NFR18): maps a per-class extraction failure to a bounded
/// <see cref="TenantExportClassStatuses"/> token via the ONE retry taxonomy (<see cref="RetryFailurePolicy"/>) —
/// a retryable decision is <c>failed-retryable</c>, a terminal/exhausted decision is <c>failed-terminal</c>. No
/// second retryable-vs-terminal classifier is introduced. (The byte-producing extraction runtime that calls this
/// is the deferred surface; the classification seam itself ships now.)
/// </summary>
internal static class TenantExportFailureClassifier
{
    public static string ClassifyClassStatus(string reasonCode, int retryCount, DateTimeOffset observedAt)
    {
        RetryPolicyDecision decision = RetryFailurePolicy.Classify(reasonCode, retryCount, observedAt);
        return decision.IsRetryable
            ? TenantExportClassStatuses.FailedRetryable
            : TenantExportClassStatuses.FailedTerminal;
    }
}
