using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal static class AttachmentUnsafeHandling
{
    public const string Quarantine = "quarantine";
    public const string Block = "block";
    public const string RejectMessage = "reject-message";

    public static string Normalize(string? value)
        => value is Quarantine or Block or RejectMessage ? value : Quarantine;
}

internal interface IAttachmentSafetyPolicy
{
    ValueTask<ProjectConversationAttachmentSafetyOutcomeView> EvaluateAsync(
        AttachmentSafetyPolicyRequest request,
        CancellationToken cancellationToken = default);
}

internal interface IAttachmentUnsafeHandlingResolver
{
    ValueTask<string> ResolveUnsafeHandlingAsync(
        AttachmentUnsafeHandlingResolutionRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record AttachmentUnsafeHandlingResolutionRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    long SourceVersion,
    string CorrelationId);

internal sealed class DefaultAttachmentUnsafeHandlingResolver : IAttachmentUnsafeHandlingResolver
{
    public ValueTask<string> ResolveUnsafeHandlingAsync(
        AttachmentUnsafeHandlingResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AttachmentUnsafeHandling.Quarantine);
    }
}

internal sealed record AttachmentSafetyPolicyRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string SourceMailboxId,
    string ProviderMessageId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ProjectConversationAttachmentStatus CurrentStorageStatus,
    string? FolderId,
    string? FileId,
    MailboxAttachmentContentResult Content,
    long SourceVersion,
    string CorrelationId,
    string? UnsafeHandling);

internal interface IAttachmentScanner
{
    ValueTask<AttachmentScanResult> ScanAsync(
        AttachmentScanRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed record AttachmentScanRequest(
    string TenantId,
    string ProjectId,
    string AssociationId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ReadOnlyMemory<byte> Content,
    string? ContentHashReference,
    long SourceVersion,
    string CorrelationId);

internal enum AttachmentScanResultKind
{
    Clean,
    Unsafe,
    Unavailable,
    Retryable,
    Failed,
    Indeterminate,
}

internal sealed record AttachmentScanResult(
    AttachmentScanResultKind Kind,
    string ReasonCode)
{
    public static AttachmentScanResult Clean()
        => new(AttachmentScanResultKind.Clean, "attachment_scan_clean");

    public static AttachmentScanResult Unsafe(string reasonCode)
        => new(AttachmentScanResultKind.Unsafe, SafeReason(reasonCode, "attachment_scan_unsafe"));

    public static AttachmentScanResult Unavailable(string reasonCode)
        => new(AttachmentScanResultKind.Unavailable, SafeReason(reasonCode, "attachment_scan_unavailable"));

    public static AttachmentScanResult Retryable(string reasonCode)
        => new(AttachmentScanResultKind.Retryable, SafeReason(reasonCode, "attachment_scan_retryable"));

    public static AttachmentScanResult Failed(string reasonCode)
        => new(AttachmentScanResultKind.Failed, SafeReason(reasonCode, "attachment_scan_failed"));

    public static AttachmentScanResult Indeterminate(string reasonCode)
        => new(AttachmentScanResultKind.Indeterminate, SafeReason(reasonCode, "attachment_scan_indeterminate"));

    internal static string SafeReason(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ||
            value.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character) ||
                !(char.IsLetterOrDigit(character) || character is '_' or '-'))
            ? fallback
            : value;
}

internal sealed class PassThroughAttachmentScanner : IAttachmentScanner
{
    public ValueTask<AttachmentScanResult> ScanAsync(
        AttachmentScanRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(AttachmentScanResult.Clean());
    }
}

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
