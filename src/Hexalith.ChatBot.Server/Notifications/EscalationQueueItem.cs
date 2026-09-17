using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A tenant-bound unresolved queue item paired with its safe per-resource authority key (or <see langword="null"/>
/// for an aggregate item with no item-specific context). The authority key comes from the tenant-bound queue
/// snapshot, never from restricted content.
/// </summary>
internal sealed record EscalationQueueItem(
    AdminQueueSummaryProjectionItem Item,
    string? ItemProjectRef = null);
