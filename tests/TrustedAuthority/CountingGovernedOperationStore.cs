using Hexalith.ChatBot.Server.Projections;

namespace Hexalith.ChatBot.Tests.TrustedAuthority;

/// <summary>Detects any protected query or persistence effect.</summary>
internal sealed class CountingGovernedOperationStore : IGovernedOperationProjectionStore
{
    /// <summary>The protected read count.</summary>
    public int Reads { get; private set; }
    /// <summary>The persistence effect count.</summary>
    public int Writes { get; private set; }
    /// <summary>The synthetic stored view.</summary>
    public GovernedOperationView? View { get; set; }
    /// <summary>Other existing protected records, including foreign tenant records in the same fixture.</summary>
    public List<GovernedOperationView> AdditionalViews { get; } = [];
    /// <inheritdoc/>
    public Task<GovernedOperationView?> GetAsync(string tenantId, string noteId, CancellationToken cancellationToken = default)
    {
        Reads++;
        return Task.FromResult(View?.TenantId == tenantId && View.NoteId == noteId ? View : AdditionalViews.SingleOrDefault(view => view.TenantId == tenantId && view.NoteId == noteId));
    }
    /// <inheritdoc/>
    public Task SaveAsync(GovernedOperationView view, CancellationToken cancellationToken = default)
    {
        Writes++;
        View = view;
        return Task.CompletedTask;
    }
}
