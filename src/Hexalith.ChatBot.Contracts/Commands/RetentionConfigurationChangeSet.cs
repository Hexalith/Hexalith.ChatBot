using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RetentionConfigurationChangeSet(
    IReadOnlyList<RetentionWindow> Windows);
