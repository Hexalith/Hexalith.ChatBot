using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Adapters.Parties;

internal sealed record ParticipantDisplaySnapshot(
    ProjectConversationParticipantDisplayKind DisplayKind,
    string? SafeDisplayLabel);
