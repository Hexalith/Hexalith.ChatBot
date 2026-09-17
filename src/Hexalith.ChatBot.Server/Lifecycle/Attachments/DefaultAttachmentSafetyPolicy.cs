using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed class DefaultAttachmentSafetyPolicy(IAttachmentScanner scanner) : IAttachmentSafetyPolicy
{
    public const long DefaultMaxSizeInBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "text/csv",
        "text/plain",
    };

    private readonly IAttachmentScanner _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));

    public async ValueTask<ProjectConversationAttachmentSafetyOutcomeView> EvaluateAsync(
        AttachmentSafetyPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string unsafeHandling = AttachmentUnsafeHandling.Normalize(request.UnsafeHandling);
        if (request.SizeInBytes is > DefaultMaxSizeInBytes)
        {
            return Restricted(request, ProjectConversationAttachmentStatus.Rejected, unsafeHandling, "attachment_policy_size_rejected", "review-source-evidence");
        }

        if (!string.IsNullOrWhiteSpace(request.ContentType) && !AllowedContentTypes.Contains(request.ContentType))
        {
            return Restricted(request, ProjectConversationAttachmentStatus.Rejected, unsafeHandling, "attachment_policy_type_rejected", "review-source-evidence");
        }

        if (request.Content.Kind is not MailboxAttachmentContentResultKind.Available)
        {
            ProjectConversationAttachmentStatus status = request.Content.Kind is MailboxAttachmentContentResultKind.Retryable
                ? ProjectConversationAttachmentStatus.Retryable
                : ProjectConversationAttachmentStatus.Unavailable;
            return Restricted(request, status, unsafeHandling, request.Content.ReasonCode, status is ProjectConversationAttachmentStatus.Retryable ? "retry-scan" : "inspect-later");
        }

        AttachmentScanResult scan = await _scanner
            .ScanAsync(
                new AttachmentScanRequest(
                    request.TenantId,
                    request.ProjectId,
                    request.AssociationId,
                    request.IntakeId,
                    request.ProviderAttachmentId,
                    request.Ordinal,
                    request.SafeDisplayName,
                    request.ContentType,
                    request.SizeInBytes,
                    request.Content.Content,
                    request.Content.ContentHashReference,
                    request.SourceVersion,
                    request.CorrelationId),
                cancellationToken)
            .ConfigureAwait(false);

        return scan.Kind switch
        {
            AttachmentScanResultKind.Clean => Clean(request, unsafeHandling),
            AttachmentScanResultKind.Unsafe => Unsafe(request, unsafeHandling),
            AttachmentScanResultKind.Unavailable => Restricted(request, ProjectConversationAttachmentStatus.Unavailable, unsafeHandling, scan.ReasonCode, "inspect-later"),
            AttachmentScanResultKind.Failed => Restricted(request, ProjectConversationAttachmentStatus.Failed, unsafeHandling, scan.ReasonCode, "inspect-later"),
            AttachmentScanResultKind.Indeterminate => Restricted(request, ProjectConversationAttachmentStatus.Retryable, unsafeHandling, scan.ReasonCode, "retry-scan"),
            _ => Restricted(request, ProjectConversationAttachmentStatus.Retryable, unsafeHandling, scan.ReasonCode, "retry-scan"),
        };
    }

    private static ProjectConversationAttachmentSafetyOutcomeView Clean(
        AttachmentSafetyPolicyRequest request,
        string unsafeHandling)
        => Outcome(
            request,
            ProjectConversationAttachmentStatus.Captured,
            "eligible",
            ["add-to-ai-context", "open-governed-file"],
            "not-retryable",
            "none",
            "attachment_scan_clean",
            unsafeHandling);

    private static ProjectConversationAttachmentSafetyOutcomeView Unsafe(
        AttachmentSafetyPolicyRequest request,
        string unsafeHandling)
        => unsafeHandling switch
        {
            AttachmentUnsafeHandling.RejectMessage => Restricted(request, ProjectConversationAttachmentStatus.Rejected, unsafeHandling, "attachment_policy_rejected", "review-source-evidence"),
            AttachmentUnsafeHandling.Block => Restricted(request, ProjectConversationAttachmentStatus.Unsafe, unsafeHandling, "attachment_policy_blocked", "blocked-by-policy"),
            _ => Restricted(request, ProjectConversationAttachmentStatus.Unsafe, unsafeHandling, "attachment_policy_quarantined", "quarantine-review"),
        };

    private static ProjectConversationAttachmentSafetyOutcomeView Restricted(
        AttachmentSafetyPolicyRequest request,
        ProjectConversationAttachmentStatus status,
        string unsafeHandling,
        string reasonCode,
        string safeNextAction)
        => Outcome(
            request,
            status,
            "not-eligible",
            [],
            status is ProjectConversationAttachmentStatus.Retryable ? "retryable" : "not-retryable",
            safeNextAction,
            AttachmentScanResult.SafeReason(reasonCode, "attachment_scan_unavailable"),
            unsafeHandling);

    private static ProjectConversationAttachmentSafetyOutcomeView Outcome(
        AttachmentSafetyPolicyRequest request,
        ProjectConversationAttachmentStatus status,
        string aiContextEligibility,
        IReadOnlyList<string> allowedActions,
        string retryState,
        string safeNextAction,
        string reasonCode,
        string unsafeHandling)
        => new(
            request.TenantId,
            request.ProjectId,
            request.AssociationId,
            request.IntakeId,
            request.ProviderAttachmentId,
            request.Ordinal,
            status,
            aiContextEligibility,
            allowedActions,
            retryState,
            safeNextAction,
            reasonCode,
            request.SourceVersion,
            request.CorrelationId,
            unsafeHandling);
}
