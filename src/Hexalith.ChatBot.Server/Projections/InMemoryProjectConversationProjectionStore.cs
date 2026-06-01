using System.Collections.Concurrent;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Projections;

internal sealed class InMemoryProjectConversationProjectionStore : IProjectConversationProjectionStore
{
    private readonly ConcurrentDictionary<string, ProjectConversationItemView> _items = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProjectConversationSourceEmailView> _sourceEmails = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ParticipantResolutionView> _participants = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _participantsByIntake = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _itemsByIntake = new(StringComparer.Ordinal);

    public Task UpsertAsync(ProjectConversationItemView item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        if (item.Kind != ProjectConversationItemKind.Participant &&
            _sourceEmails.TryGetValue(ProjectConversationSourceEmailView.KeyFor(item.TenantId, item.IntakeId), out ProjectConversationSourceEmailView? source))
        {
            item = item.WithSourceEmail(source);
        }

        string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        _items.AddOrUpdate(
            key,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(key, 0);
        if (item.Kind != ProjectConversationItemKind.Participant &&
            _participantsByIntake.TryGetValue(IntakeIndexKeyFor(item.TenantId, item.IntakeId), out ConcurrentDictionary<string, byte>? participantKeys))
        {
            foreach (string participantKey in participantKeys.Keys)
            {
                if (_participants.TryGetValue(participantKey, out ParticipantResolutionView? participant))
                {
                    UpsertMaterializedParticipant(participant, item);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<ProjectConversationSourceEmailView?> GetSourceEmailAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sourceEmails.TryGetValue(ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId), out ProjectConversationSourceEmailView? source);
        return Task.FromResult(source);
    }

    public Task UpsertSourceEmailAsync(ProjectConversationSourceEmailView source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        cancellationToken.ThrowIfCancellationRequested();

        string sourceKey = ProjectConversationSourceEmailView.KeyFor(source.TenantId, source.IntakeId);
        _sourceEmails.AddOrUpdate(
            sourceKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationSourceEmailView.ShouldReplace(existing, incoming) ? incoming : existing,
            source);
        if (!_sourceEmails.TryGetValue(sourceKey, out ProjectConversationSourceEmailView? effective) ||
            !Equals(effective, source))
        {
            return Task.CompletedTask;
        }

        if (_itemsByIntake.TryGetValue(IntakeIndexKeyFor(source.TenantId, source.IntakeId), out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                _items.AddOrUpdate(
                    itemKey,
                    static (_, _) => throw new InvalidOperationException("Cannot enrich a missing conversation item."),
                static (_, existing, incoming) => existing.WithSourceEmail(incoming),
                    source);
            }
        }

        return Task.CompletedTask;
    }

    public Task UpsertParticipantResolutionAsync(ParticipantResolutionView participant, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(participant);
        cancellationToken.ThrowIfCancellationRequested();

        string participantKey = ParticipantResolutionView.KeyFor(participant.TenantId, participant.ResolutionId, participant.SourceParticipantId);
        _participants.AddOrUpdate(
            participantKey,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => incoming.SourceVersion >= existing.SourceVersion ? incoming : existing,
            participant);
        if (!_participants.TryGetValue(participantKey, out ParticipantResolutionView? effective) ||
            !Equals(effective, participant))
        {
            return Task.CompletedTask;
        }

        string intakeKey = IntakeIndexKeyFor(participant.TenantId, participant.IntakeId);
        _ = _participantsByIntake
            .GetOrAdd(intakeKey, static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(participantKey, 0);

        if (_itemsByIntake.TryGetValue(intakeKey, out ConcurrentDictionary<string, byte>? itemKeys))
        {
            foreach (string itemKey in itemKeys.Keys)
            {
                if (_items.TryGetValue(itemKey, out ProjectConversationItemView? association) &&
                    association.Kind != ProjectConversationItemKind.Participant)
                {
                    UpsertMaterializedParticipant(participant, association);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task<ProjectConversationPage> ReadPageAsync(
        string tenantId,
        string projectId,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!ProjectConversationCursor.TryRead(cursor, tenantId, projectId, out DateTimeOffset cursorTime, out string? cursorItemId))
        {
            return Task.FromResult(new ProjectConversationPage([], null, false, pageSize));
        }

        string prefix = $"{tenantId}:project-conversation:{projectId}:";
        ProjectConversationItemView[] pageItems = _items
            .Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
            .Select(static pair => pair.Value)
            .OrderBy(static item => item.OccurredAt)
            .ThenBy(static item => item.ItemId, StringComparer.Ordinal)
            .Where(item => IsAfterCursor(item, cursorTime, cursorItemId))
            .Take(pageSize + 1)
            .ToArray();
        bool hasMore = pageItems.Length > pageSize;
        ProjectConversationItemView[] visible = pageItems.Take(pageSize).ToArray();
        string? nextCursor = hasMore && visible.Length > 0
            ? ProjectConversationCursor.Create(tenantId, projectId, visible[^1].OccurredAt, visible[^1].ItemId)
            : null;
        return Task.FromResult(new ProjectConversationPage(visible, nextCursor, hasMore, pageSize));
    }

    private static bool IsAfterCursor(ProjectConversationItemView item, DateTimeOffset cursorTime, string? cursorItemId)
        => cursorItemId is null ||
            item.OccurredAt > cursorTime ||
            (item.OccurredAt == cursorTime && string.CompareOrdinal(item.ItemId, cursorItemId) > 0);

    private static string IntakeIndexKeyFor(string tenantId, string intakeId)
        => $"{tenantId}:project-conversation:{intakeId}:items";

    private void UpsertMaterializedParticipant(ParticipantResolutionView participant, ProjectConversationItemView association)
    {
        ProjectConversationItemView item = ProjectConversationItemView.FromParticipant(participant, association);
        string key = ProjectConversationItemView.KeyFor(item.TenantId, item.ProjectId, item.ItemId);
        _items.AddOrUpdate(
            key,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => ProjectConversationItemView.ShouldReplace(existing, incoming) ? incoming : existing,
            item);
        _ = _itemsByIntake
            .GetOrAdd(IntakeIndexKeyFor(item.TenantId, item.IntakeId), static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal))
            .TryAdd(key, 0);
    }
}
