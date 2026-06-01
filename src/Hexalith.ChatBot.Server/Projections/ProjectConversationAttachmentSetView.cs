using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Intake;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record ProjectConversationAttachmentSetView(
    string TenantId,
    string IntakeId,
    IReadOnlyList<ProjectConversationAttachmentReferenceView> Attachments,
    long SourceVersion,
    string CorrelationId)
{
    public static string KeyFor(string tenantId, string intakeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        return $"{tenantId}:project-conversation-attachments:{intakeId}";
    }

    public static ProjectConversationAttachmentSetView FromIntake(
        string tenantId,
        MailboxMessageIntakeCaptured captured,
        long sourceVersion,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(captured);

        ProjectConversationAttachmentReferenceView[] attachments = captured.AttachmentReferences
            .Select((attachment, index) => ProjectConversationAttachmentReferenceView.FromReference(
                tenantId,
                captured.IntakeId,
                attachment,
                index,
                captured.RedactionState,
                captured.RetentionClass,
                sourceVersion,
                correlationId))
            .ToArray();

        return new ProjectConversationAttachmentSetView(
            tenantId,
            captured.IntakeId,
            attachments,
            sourceVersion,
            correlationId);
    }

    public static bool ShouldReplace(ProjectConversationAttachmentSetView existing, ProjectConversationAttachmentSetView incoming)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(incoming);
        return incoming.SourceVersion >= existing.SourceVersion;
    }
}

internal sealed record ProjectConversationAttachmentReferenceView(
    string TenantId,
    string IntakeId,
    string ProviderAttachmentId,
    int Ordinal,
    string? SafeDisplayName,
    string? ContentType,
    long? SizeInBytes,
    ProjectConversationAttachmentStatus CaptureStatus,
    ProjectConversationAttachmentStatus StorageStatus,
    ProjectConversationAttachmentStatus ScanStatus,
    string? FolderId,
    string? FileId,
    string DuplicateState,
    string RetryState,
    string AiContextEligibility,
    IReadOnlyList<string> AllowedActions,
    string RedactionState,
    string RetentionClass,
    long SourceVersion,
    string CorrelationId,
    long StorageSourceVersion = 0)
{
    public static string KeyFor(string tenantId, string intakeId, int ordinal, string providerAttachmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAttachmentId);
        return $"{tenantId}:project-conversation-attachment:{intakeId}:{ordinal}:{ProjectConversationStableId.Hash(providerAttachmentId)}";
    }

    public static ProjectConversationAttachmentReferenceView FromReference(
        string tenantId,
        string intakeId,
        MailboxAttachmentReference reference,
        int ordinal,
        string redactionState,
        string retentionClass,
        long sourceVersion,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeId);
        ArgumentNullException.ThrowIfNull(reference);

        bool metadataVisible = IsMetadataVisible(redactionState);
        string? safeName = metadataVisible ? SafeDisplayNameText(reference.Name, maxLength: 256) : null;
        string? safeContentType = metadataVisible ? SafeMetadataText(reference.ContentType, maxLength: 120) : null;
        long? safeSize = metadataVisible && reference.SizeInBytes is >= 0 ? reference.SizeInBytes : null;
        string derivedMetadataState = metadataVisible ? "not-evaluated" : "redacted";
        string retryState = metadataVisible ? "not-retryable" : "redacted";

        return new ProjectConversationAttachmentReferenceView(
            tenantId,
            intakeId,
            reference.ProviderAttachmentId,
            ordinal,
            safeName,
            safeContentType,
            safeSize,
            ProjectConversationAttachmentStatus.Captured,
            ProjectConversationAttachmentStatus.Pending,
            ProjectConversationAttachmentStatus.Pending,
            null,
            null,
            derivedMetadataState,
            retryState,
            derivedMetadataState,
            [],
            redactionState,
            retentionClass,
            sourceVersion,
            correlationId,
            sourceVersion);
    }

    public string StableMaterializedIdFor(string associationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(associationId);
        return $"attachment:{associationId}:{Ordinal}:{ProjectConversationStableId.Hash(ProviderAttachmentId)}";
    }

    public bool MatchesStorageOutcome(ProjectConversationAttachmentStorageOutcomeView outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        return string.Equals(TenantId, outcome.TenantId, StringComparison.Ordinal) &&
            string.Equals(IntakeId, outcome.IntakeId, StringComparison.Ordinal) &&
            string.Equals(ProviderAttachmentId, outcome.ProviderAttachmentId, StringComparison.Ordinal) &&
            Ordinal == outcome.Ordinal;
    }

    public ProjectConversationAttachmentReferenceView WithStorageOutcome(ProjectConversationAttachmentStorageOutcomeView outcome)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (!MatchesStorageOutcome(outcome) || outcome.SourceVersion < StorageSourceVersion)
        {
            return this;
        }

        if (StorageStatus is ProjectConversationAttachmentStatus.Captured &&
            outcome.StorageStatus is not ProjectConversationAttachmentStatus.Captured &&
            !string.IsNullOrWhiteSpace(FolderId) &&
            !string.IsNullOrWhiteSpace(FileId))
        {
            return this;
        }

        bool metadataVisible = IsMetadataVisible(RedactionState);
        return this with
        {
            StorageStatus = outcome.StorageStatus,
            FolderId = metadataVisible ? outcome.FolderId : null,
            FileId = metadataVisible ? outcome.FileId : null,
            DuplicateState = SafeMetadataToken(outcome.DuplicateState, DuplicateState),
            RetryState = SafeMetadataToken(outcome.RetryState, RetryState),
            AiContextEligibility = SafeMetadataToken(outcome.AiContextEligibility, AiContextEligibility),
            AllowedActions = outcome.AllowedActions
                .Where(static action => SafeMetadataToken(action, string.Empty).Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray(),
            SourceVersion = Math.Max(SourceVersion, outcome.SourceVersion),
            StorageSourceVersion = outcome.SourceVersion,
            CorrelationId = outcome.CorrelationId,
        };
    }

    private static bool IsMetadataVisible(string redactionState)
        => string.Equals(redactionState, "metadata_only", StringComparison.Ordinal);

    private static string? SafeMetadataText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string sanitized = new(value
            .Where(static character => !char.IsControl(character))
            .Take(maxLength)
            .ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.Trim();
    }

    private static string? SafeDisplayNameText(string? value, int maxLength)
    {
        string? sanitized = SafeMetadataText(value, maxLength);
        if (sanitized is null)
        {
            return null;
        }

        string fileName = sanitized
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault() ?? sanitized;
        return fileName is "." or ".." ? null : fileName;
    }

    private static string SafeMetadataToken(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Any(static character => char.IsControl(character) || char.IsWhiteSpace(character) ||
                !(char.IsLetterOrDigit(character) || character is '_' or '-')))
        {
            return fallback;
        }

        return value;
    }
}

internal static class ProjectConversationStableId
{
    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
