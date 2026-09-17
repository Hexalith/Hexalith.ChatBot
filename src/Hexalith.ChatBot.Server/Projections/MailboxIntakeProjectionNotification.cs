using Hexalith.ChatBot.Server.Association.Intake;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed record MailboxIntakeProjectionNotification(
    string TenantId,
    MailboxMessageIntakeCaptured Captured,
    long SourceVersion,
    string CorrelationId);
