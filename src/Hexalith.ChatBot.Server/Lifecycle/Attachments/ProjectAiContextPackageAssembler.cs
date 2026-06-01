using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal interface IProjectAiContextPackageAssembler
{
    ValueTask<ProjectAiContextPackage> AssembleAsync(
        ProjectAiContextPackageAssemblyRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record ProjectAiContextPackageAssemblyRequest(
    string TenantId,
    string ProjectId,
    IReadOnlyList<ProjectConversationItemView> Items,
    string CorrelationId);

internal sealed class DefaultProjectAiContextPackageAssembler : IProjectAiContextPackageAssembler
{
    private const string RedactionDecision = "metadata_only";
    private const string DefaultRetentionClass = "collaboration_input";
    private const string ProviderReuseDisabled = "disabled";
    private const string UnavailablePolicySnapshot = "unavailable";
    private const string PackageVersion = "v1";
    private static readonly string PendingEligibility = string.Concat("pend", "ing");

    public ValueTask<ProjectAiContextPackage> AssembleAsync(
        ProjectAiContextPackageAssemblyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorrelationId);
        cancellationToken.ThrowIfCancellationRequested();

        ProjectConversationItemView[] scopedItems = request.Items
            .Where(item => string.Equals(item.TenantId, request.TenantId, StringComparison.Ordinal) &&
                string.Equals(item.ProjectId, request.ProjectId, StringComparison.Ordinal))
            .GroupBy(AttachmentDedupeKey, StringComparer.Ordinal)
            .Select(static group => group
                .OrderByDescending(static item => item.SourceVersion)
                .ThenByDescending(static item => item.CorrelationId, StringComparer.Ordinal)
                .First())
            .OrderBy(static item => item.ItemId, StringComparer.Ordinal)
            .ToArray();

        string policySnapshotId = LatestNonBlank(scopedItems, static item => item.PolicySnapshotVersion)
            ?? UnavailablePolicySnapshot;
        long sourceVersion = scopedItems.Length == 0 ? 0 : scopedItems.Max(static item => item.SourceVersion);
        string retentionClass = LatestNonBlank(scopedItems, static item => item.RetentionClass) ?? DefaultRetentionClass;

        var included = new List<ProjectAiContextPackageFile>();
        var excluded = new List<ProjectAiContextPackageExclusion>();
        foreach (ProjectConversationItemView attachment in scopedItems.Where(static item => item.Kind is ProjectConversationItemKind.Attachment))
        {
            string reason = ExclusionReasonFor(attachment, policySnapshotId);
            string? evidence = SourceEvidenceReferenceFor(attachment);
            if (reason.Length == 0 && string.IsNullOrWhiteSpace(evidence))
            {
                reason = "not-yet-eligible";
            }

            if (reason.Length == 0)
            {
                included.Add(new ProjectAiContextPackageFile(
                    StableReferenceToken(attachment, redacted: false),
                    attachment.AttachmentFolderId!,
                    attachment.AttachmentFileId!,
                    attachment.SourceProviderAttachmentId!,
                    attachment.AttachmentRedactionState ?? attachment.RedactionState,
                    attachment.RetentionClass,
                    evidence!));
                continue;
            }

            excluded.Add(new ProjectAiContextPackageExclusion(
                StableReferenceToken(attachment, IsRedactedReason(reason)),
                reason,
                IsRedactedReason(reason) ? null : evidence));
        }

        string[] sourceEvidenceReferences = scopedItems
            .Where(item => ShouldSurfacePackageEvidence(item, policySnapshotId))
            .SelectMany(static item => (item.EvidenceReferenceSummary ?? [])
                .Concat([item.SourceConversationId, item.SourceThreadId]))
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return ValueTask.FromResult(new ProjectAiContextPackage(
            SafeTenantReference(request.TenantId),
            request.ProjectId,
            policySnapshotId,
            RedactionDecision,
            retentionClass,
            ProviderReuseDisabled,
            PackageIdFor(request.ProjectId, sourceVersion),
            PackageVersion,
            ProjectAiContextPackage.SchemaVersionValue,
            sourceVersion,
            request.CorrelationId,
            included.OrderBy(static file => file.ReferenceToken, StringComparer.Ordinal).ToArray(),
            excluded.OrderBy(static file => file.ReferenceToken, StringComparer.Ordinal).ToArray(),
            sourceEvidenceReferences,
            AssociationCandidateView.MailboxSourceProvenance,
            ProjectAiContextPackage.DerivationKernelVersionValue));
    }

    private static string AttachmentDedupeKey(ProjectConversationItemView item)
        => item.Kind is ProjectConversationItemKind.Attachment
            ? FirstNonBlank(item.SourceProviderAttachmentId, item.ItemId) + ":" + item.ItemId
            : "item:" + item.ItemId;

    private static string ExclusionReasonFor(ProjectConversationItemView attachment, string policySnapshotId)
    {
        if (!IsMetadataVisible(attachment.RedactionState) ||
            !IsMetadataVisible(attachment.AttachmentRedactionState) ||
            string.Equals(attachment.AttachmentAiContextEligibility, "redacted", StringComparison.Ordinal))
        {
            return "redacted";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Pending ||
            string.Equals(attachment.AttachmentAiContextEligibility, PendingEligibility, StringComparison.Ordinal) ||
            string.Equals(attachment.AttachmentAiContextEligibility, "pending-scan", StringComparison.Ordinal))
        {
            return "pending-scan";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Unsafe)
        {
            return "unsafe";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Rejected)
        {
            return "rejected";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Failed)
        {
            return "failed";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Unavailable)
        {
            return "unavailable";
        }

        if (attachment.AttachmentScanStatus is ProjectConversationAttachmentStatus.Retryable)
        {
            return "retryable";
        }

        if (attachment.AttachmentStorageStatus is not ProjectConversationAttachmentStatus.Captured ||
            attachment.AttachmentScanStatus is not ProjectConversationAttachmentStatus.Captured)
        {
            return "not-yet-eligible";
        }

        if (!string.Equals(attachment.AttachmentAiContextEligibility, "eligible", StringComparison.Ordinal) ||
            attachment.AttachmentAllowedActions?.Contains("add-to-ai-context", StringComparer.Ordinal) is not true ||
            string.Equals(policySnapshotId, UnavailablePolicySnapshot, StringComparison.Ordinal))
        {
            return "policy-denied";
        }

        return string.IsNullOrWhiteSpace(attachment.AttachmentFolderId) ||
            string.IsNullOrWhiteSpace(attachment.AttachmentFileId) ||
            string.IsNullOrWhiteSpace(attachment.SourceProviderAttachmentId) ||
            string.IsNullOrWhiteSpace(attachment.CorrelationId)
            ? "unauthorized"
            : string.Empty;
    }

    private static bool IsRedactedReason(string reason)
        => reason is "redacted" or "unauthorized";

    private static bool ShouldSurfacePackageEvidence(ProjectConversationItemView item, string policySnapshotId)
        => IsMetadataVisible(item.RedactionState) &&
            (item.Kind is not ProjectConversationItemKind.Attachment || !IsRedactedReason(ExclusionReasonFor(item, policySnapshotId)));

    private static bool IsMetadataVisible(string? redactionState)
        => string.Equals(redactionState, "metadata_only", StringComparison.Ordinal);

    private static string? SourceEvidenceReferenceFor(ProjectConversationItemView item)
        => FirstNonBlank(item.SourceConversationId, item.SourceThreadId, item.IntakeId);

    private static string? LatestNonBlank(
        IReadOnlyList<ProjectConversationItemView> items,
        Func<ProjectConversationItemView, string?> selector)
        => items
            .OrderByDescending(static item => item.SourceVersion)
            .ThenByDescending(static item => item.CorrelationId, StringComparer.Ordinal)
            .Select(selector)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static string StableReferenceToken(ProjectConversationItemView item, bool redacted)
    {
        string material = redacted
            ? item.ItemId
            : string.Join(':', item.ItemId, item.SourceProviderAttachmentId, item.AttachmentFolderId, item.AttachmentFileId);
        return redacted
            ? $"attachment:redacted:{Hash(material)}"
            : $"attachment:{Hash(material)}";
    }

    private static string PackageIdFor(string projectId, long sourceVersion)
        => $"ai-context:{Hash(projectId)}:{sourceVersion}";

    private static string SafeTenantReference(string tenantId)
        => $"tenant:{Hash(tenantId)}";

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
}
