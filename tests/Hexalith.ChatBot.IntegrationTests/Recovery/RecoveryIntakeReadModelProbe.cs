using System.Collections.Concurrent;

using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Checks production intake roots and indexed fixture descendants, retaining captured cleanup identities.</summary>
internal sealed class RecoveryIntakeReadModelProbe
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);
    private readonly IReadModelConditionalEraser _readModelEraser;
    private readonly ReadModelProjectConversationProjectionStore _readModels;
    private readonly ConcurrentDictionary<(string Tenant, string Intake), IReadOnlyList<string>> _capturedKeys = new();
    private readonly TimeSpan _pollInterval;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a probe with the production polling cadence.</summary>
    /// <param name="readModelStore">The production read-model channel used for inventory reads.</param>
    /// <param name="readModelEraser">The read-model channel used for authoritative presence checks.</param>
    public RecoveryIntakeReadModelProbe(IReadModelStore readModelStore, IReadModelConditionalEraser readModelEraser)
        : this(readModelStore, readModelEraser, DefaultPollInterval)
    {
    }

    /// <summary>Initializes a probe with a test-specific polling cadence.</summary>
    /// <param name="readModelStore">The production read-model channel used for inventory reads.</param>
    /// <param name="readModelEraser">The read-model channel used for authoritative presence checks.</param>
    /// <param name="pollInterval">The delay between sustained-absence observations.</param>
    /// <param name="timeProvider">The monotonic clock, or the system clock when omitted.</param>
    internal RecoveryIntakeReadModelProbe(
        IReadModelStore readModelStore,
        IReadModelConditionalEraser readModelEraser,
        TimeSpan pollInterval,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(readModelStore);
        ArgumentNullException.ThrowIfNull(readModelEraser);
        ArgumentOutOfRangeException.ThrowIfLessThan(pollInterval, TimeSpan.Zero);

        _readModelEraser = readModelEraser;
        _readModels = new ReadModelProjectConversationProjectionStore(readModelStore);
        _pollInterval = pollInterval;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Gets the polling cadence used by sustained-absence checks.</summary>
    internal TimeSpan PollInterval => _pollInterval;

    /// <summary>Returns production intake root keys; indexed descendants require an inventory capture.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <returns>The production ordered root keys.</returns>
    public static IReadOnlyList<string> KeysFor(string tenantId, string intakeId)
        => ReadModelProjectConversationProjectionStore.IntakeReadModelKeysFor(tenantId, intakeId);

    /// <summary>Captures indexed fixture rows before erasure and preserves earlier captured descendants.</summary>
    /// <param name="tenantId">The fixture tenant identifier.</param>
    /// <param name="intakeId">The fixture intake identifier.</param>
    /// <param name="cancellationToken">Cancels inventory reads.</param>
    /// <returns>Known descendants followed by production intake roots, excluding shared indexes.</returns>
    public async Task<IReadOnlyList<string>> CaptureKeysAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string> inventory = await _readModels.GetIntakeReadModelKeysAsync(
            tenantId, intakeId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<string> roots = KeysFor(tenantId, intakeId);
        return _capturedKeys.AddOrUpdate((tenantId, intakeId), inventory, (_, previous) =>
            [.. previous.Concat(inventory).Except(roots, StringComparer.Ordinal).Order(StringComparer.Ordinal), .. roots]);
    }

    /// <summary>Checks production roots, indexed fixture rows and previously captured descendants for absence.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <param name="cancellationToken">Cancels the storage reads.</param>
    /// <returns><see langword="true"/> when all inventoried keys are absent; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AreAbsentAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
    {
        HashSet<string> checkedKeys = new(StringComparer.Ordinal);
        _capturedKeys.TryGetValue((tenantId, intakeId), out IReadOnlyList<string>? captured);
        foreach (string key in KeysFor(tenantId, intakeId).Concat(captured ?? []).Distinct(StringComparer.Ordinal))
        {
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                cancellationToken).ConfigureAwait(false);
            if (present)
            {
                return false;
            }

            checkedKeys.Add(key);
        }

        foreach (string key in await CaptureKeysAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false))
        {
            if (!checkedKeys.Add(key))
            {
                continue;
            }

            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName, key, cancellationToken).ConfigureAwait(false);
            if (present)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Checks that roots and inventoried fixture descendants remain absent throughout a polling window.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <param name="window">The sustained-absence observation window.</param>
    /// <param name="cancellationToken">Cancels storage reads and polling delays.</param>
    /// <returns><see langword="true"/> when all inventoried keys remain absent; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemainsAbsentAsync(
        string tenantId,
        string intakeId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        long startedAt = _timeProvider.GetTimestamp();
        do
        {
            if (!await AreAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Delay(_pollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
        while (_timeProvider.GetElapsedTime(startedAt) < window);

        return await AreAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false);
    }
}
