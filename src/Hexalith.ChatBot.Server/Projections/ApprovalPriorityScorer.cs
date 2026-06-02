using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// The deterministic, explainable priority result for a single pending approval (Story 7.8, NFR46). All fields are
/// metadata-only safe tokens — never project content, evidence, recipient PII, or command bodies.
/// </summary>
/// <param name="Score">The deterministic priority score; higher sorts first.</param>
/// <param name="Explanation">A safe single-token summary of the contributing dimensions (no spaces, ascii-safe).</param>
/// <param name="GroupKey">The tenant-scoped <c>sha256:</c> fingerprint over <c>(requester × command × project)</c>.</param>
internal sealed record ApprovalPriorityResult(decimal Score, string Explanation, string GroupKey);

/// <summary>
/// Pure, clock-injected approval-queue prioritization + grouping engine (Story 7.8, NFR46, FR75d, NFR2).
///
/// <para>
/// Priority is the deterministic, explainable product <c>(risk-class × authority-of-affected-party × time-in-queue)</c>,
/// computed from finite token ladders (<see cref="RiskClasses.Rank(RiskClass)"/>,
/// <see cref="SenderAuthorityClasses.Rank(SenderAuthorityClass)"/>) and server-measured UTC time-in-queue
/// (<c>now − RequestedAtUtc</c>, clamped to ≥ 0, never client/item-supplied time). The tenant-configurable
/// <see cref="ApprovalPriorityWeights"/> scale each dimension's relative contribution; the formula itself is closed —
/// tenants cannot add dimensions or supply an expression. The class never reads the wall clock.
/// </para>
///
/// <para>
/// Grouping derives a stable <c>sha256:</c> fingerprint over the canonical <c>(RequesterId, CommandName, ProjectId)</c>
/// triple within the authenticated tenant binding (never <c>GetHashCode()</c>): identical input shapes share a group;
/// any differing dimension — or a different tenant — yields a different fingerprint and is never merged. Grouping is a
/// read/UI construct; it creates no authority, write path, or approval truth source.
/// </para>
/// </summary>
internal static class ApprovalPriorityScorer
{
    /// <summary>Server-measured time-in-queue is clamped to this upper bound to keep the score deterministic and bounded.</summary>
    public const long MaxTimeInQueueSeconds = 30L * 24 * 60 * 60;

    /// <summary>The terminal/decided statuses excluded from the prioritized pending queue.</summary>
    private static readonly IReadOnlySet<ApprovalStatus> TerminalStatuses = new HashSet<ApprovalStatus>
    {
        ApprovalStatus.Approved,
        ApprovalStatus.Rejected,
        ApprovalStatus.RevisionRequested,
        ApprovalStatus.Cancelled,
        ApprovalStatus.Executed,
        ApprovalStatus.Failed,
    };

    /// <summary>Returns <see langword="true"/> only for items that still belong in the prioritized pending queue.</summary>
    public static bool IsPending(ApprovalEventView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return view.Status == ApprovalStatus.Pending && !TerminalStatuses.Contains(view.Status);
    }

    /// <summary>
    /// Deterministic numeric core: <c>(1 + riskWeight·riskRank) × (1 + authorityWeight·authorityRank) ×
    /// (1 + timeWeight·timeInQueueSeconds)</c>. A weight of zero collapses its factor to 1 (no contribution); the product
    /// keeps every active dimension multiplicative so highest-risk/highest-authority/oldest sorts first.
    /// </summary>
    public static decimal Score(int riskRank, int authorityRank, long timeInQueueSeconds, ApprovalPriorityWeights weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        ApprovalPriorityWeights safe = weights.IsWithinBounds ? weights : ApprovalPriorityWeights.SafeDefaults;
        long clampedAge = Math.Clamp(timeInQueueSeconds, 0, MaxTimeInQueueSeconds);

        decimal riskFactor = 1m + ((decimal)safe.RiskWeight * Math.Max(0, riskRank));
        decimal authorityFactor = 1m + ((decimal)safe.AuthorityWeight * Math.Max(0, authorityRank));
        decimal ageFactor = 1m + ((decimal)safe.TimeInQueueWeight * clampedAge);
        return riskFactor * authorityFactor * ageFactor;
    }

    /// <summary>
    /// Evaluates the priority score, the safe explanation token, and the group fingerprint for a pending approval using
    /// server-measured time-in-queue against the injected clock.
    /// </summary>
    public static ApprovalPriorityResult Evaluate(ApprovalEventView view, ApprovalPriorityWeights weights, ISystemClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return Evaluate(view, weights, clock.UtcNow);
    }

    /// <summary>Clock-free overload used by unit tests to exercise exactly-equal-score and time boundary cases.</summary>
    public static ApprovalPriorityResult Evaluate(ApprovalEventView view, ApprovalPriorityWeights weights, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(weights);

        RiskClass riskClass = view.RiskClass ?? RiskClass.None;
        SenderAuthorityClass authority = SenderAuthorityClasses.FromWireValueOrLowest(view.SenderAuthorityClass);
        int riskRank = RiskClasses.Rank(riskClass);
        int authorityRank = SenderAuthorityClasses.Rank(authority);
        long timeInQueueSeconds = TimeInQueueSeconds(view.RequestedAtUtc, now);

        decimal score = Score(riskRank, authorityRank, timeInQueueSeconds, weights);

        // Safe single-token explanation (no spaces; ascii-safe). Authority wire tokens contain spaces, so normalise.
        string authorityToken = SenderAuthorityClasses.ToWireValue(authority).Replace(' ', '-');
        string explanation = string.Create(
            CultureInfo.InvariantCulture,
            $"risk:{RiskClasses.ToWireValue(riskClass)}|authority:{authorityToken}|age:{timeInQueueSeconds}s");

        string groupKey = GroupKey(view.TenantId, view.RequesterId, view.CommandName, view.ProjectId);
        return new ApprovalPriorityResult(score, explanation, groupKey);
    }

    /// <summary>
    /// Stable, tenant-scoped group fingerprint over the canonical <c>(RequesterId, CommandName, ProjectId)</c> triple.
    /// The tenant id comes from the authenticated binding (never an item/requester/project ref). Always
    /// <c>sha256:</c>-over-canonical, never <c>GetHashCode()</c>.
    /// </summary>
    public static string GroupKey(string tenantId, string? requesterId, string? commandName, string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        string canonical = string.Join(
            "|",
            "tenant:" + tenantId,
            "requester:" + (requesterId ?? string.Empty),
            "command:" + (commandName ?? string.Empty),
            "project:" + projectId);
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static long TimeInQueueSeconds(DateTimeOffset? requestedAtUtc, DateTimeOffset now)
    {
        if (requestedAtUtc is not { } requested)
        {
            return 0;
        }

        double seconds = (now.ToUniversalTime() - requested.ToUniversalTime()).TotalSeconds;
        return seconds <= 0 ? 0 : (long)Math.Min(seconds, MaxTimeInQueueSeconds);
    }
}
