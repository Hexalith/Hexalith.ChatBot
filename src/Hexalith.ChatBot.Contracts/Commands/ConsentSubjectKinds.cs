using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The closed governed-subject dimension (AC1). Each value is an <c>AuditMetadata</c>-safe bounded token. Mirrors
/// the <see cref="DeletionErasureClassActions"/> shape line-for-line. The four kinds are the FR20 governed subjects:
/// external participants, retained content, attachments, and AI-processing events.
/// </summary>
public static class ConsentSubjectKinds
{
    public const string ExternalParticipant = "external-participant";
    public const string RetainedContent = "retained-content";
    public const string Attachment = "attachment";
    public const string AiProcessing = "ai-processing";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>([ExternalParticipant, RetainedContent, Attachment, AiProcessing], StringComparer.Ordinal);

    public static bool Contains(string? value)
        => !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}
