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
