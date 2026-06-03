using System.Security.Claims;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// See-only read policy for the operational dashboard. It reuses <see cref="AdminQueueSummaryReadPolicy"/>
/// verbatim: a human admin holding <c>AdminScope.SeeOnly</c> may read tenant-wide queue/health summaries without
/// per-project membership, and the read fails closed above the audit-availability threshold when audit is
/// unavailable. Service/AI/non-human callers (even with tenant-admin-looking claims) are denied before state load
/// with a safe reason code. The dashboard adds no new authorization path — the safety floor stays on the gateway
/// spine, never inside this trim-able dashboard read stage.
/// </summary>
internal static class OperationalDashboardReadPolicy
{
    public static AdminQueueSummaryReadDecision Evaluate(
        ClaimsPrincipal principal,
        int aggregationCount,
        int auditThreshold,
        bool auditAvailable)
        => AdminQueueSummaryReadPolicy.Evaluate(principal, aggregationCount, auditThreshold, auditAvailable);
}
