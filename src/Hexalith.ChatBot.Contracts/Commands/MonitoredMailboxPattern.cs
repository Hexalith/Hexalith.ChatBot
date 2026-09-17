using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MonitoredMailboxPattern(
    string MailboxId,
    string SourceContext,
    string ProviderConnectionRef,
    bool IsEnabled,
    string PatternRef);
