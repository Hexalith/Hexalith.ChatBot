using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record RetentionWindow(
    string RetentionClassId,
    string RetentionWindowRef,
    int WindowDays);
