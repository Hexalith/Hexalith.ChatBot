using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Folders;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Lifecycle.Attachments;

internal sealed record AttachmentCaptureCoordinatorRequest(
    string TenantId,
    string IntakeId,
    long SourceVersion,
    string CorrelationId, string? ProjectId = null, string? AssociationId = null, string? SubjectId = null, int Attempt = 0, string? RetryId = null);
