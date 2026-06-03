using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.8 (AC3, NFR17/NFR18): a per-class extraction failure maps to a bounded
/// <see cref="TenantExportClassStatuses"/> token via the ONE retry taxonomy (<c>RetryFailurePolicy</c>) — no second
/// retryable-vs-terminal classifier. Lives in the Server test project because <c>RetryFailurePolicy</c> is internal
/// to <c>.Server</c> (the boundary the pure planner is kept clear of).
/// </summary>
public sealed class TenantExportFailureClassifierTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 6, 3, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("graph_throttled", 1, TenantExportClassStatuses.FailedRetryable)]
    [InlineData("projection_retryable", 0, TenantExportClassStatuses.FailedRetryable)]
    [InlineData("graph_permission_revoked", 1, TenantExportClassStatuses.FailedTerminal)]
    [InlineData("graph_throttled", 5, TenantExportClassStatuses.FailedTerminal)] // exhausted ⇒ terminal
    public void ClassifyClassStatusShouldReuseTheRetryTaxonomy(string reasonCode, int retryCount, string expected)
        => TenantExportFailureClassifier.ClassifyClassStatus(reasonCode, retryCount, ObservedAt)
            .ShouldBe(expected);
}
