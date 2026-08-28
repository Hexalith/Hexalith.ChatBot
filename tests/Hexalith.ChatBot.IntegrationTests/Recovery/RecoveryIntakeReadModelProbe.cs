using System.Diagnostics;

using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Owns the canonical recovery-harness keys and absence checks for intake read models.</summary>
internal sealed class RecoveryIntakeReadModelProbe
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(500);
    private readonly IReadModelConditionalEraser _readModelEraser;
    private readonly TimeSpan _pollInterval;

    /// <summary>Initializes a probe with the production polling cadence.</summary>
    /// <param name="readModelEraser">The read-model channel used for authoritative presence checks.</param>
    public RecoveryIntakeReadModelProbe(IReadModelConditionalEraser readModelEraser)
        : this(readModelEraser, DefaultPollInterval)
    {
    }

    /// <summary>Initializes a probe with a test-specific polling cadence.</summary>
    /// <param name="readModelEraser">The read-model channel used for authoritative presence checks.</param>
    /// <param name="pollInterval">The delay between sustained-absence observations.</param>
    internal RecoveryIntakeReadModelProbe(
        IReadModelConditionalEraser readModelEraser,
        TimeSpan pollInterval)
    {
        ArgumentNullException.ThrowIfNull(readModelEraser);
        ArgumentOutOfRangeException.ThrowIfLessThan(pollInterval, TimeSpan.Zero);

        _readModelEraser = readModelEraser;
        _pollInterval = pollInterval;
    }

    /// <summary>Gets the polling cadence used by sustained-absence checks.</summary>
    internal TimeSpan PollInterval => _pollInterval;

    /// <summary>Returns the canonical intake read-model keys in cleanup and verification order.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <returns>The canonical ordered read-model keys.</returns>
    public static IReadOnlyList<string> KeysFor(string tenantId, string intakeId)
        =>
        [
            ProjectConversationSourceEmailView.KeyFor(tenantId, intakeId),
            ProjectConversationAttachmentSetView.KeyFor(tenantId, intakeId),
            $"{tenantId}:project-conversation:{intakeId}:attachments",
        ];

    /// <summary>Checks once whether every canonical intake read model is absent.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <param name="cancellationToken">Cancels the storage reads.</param>
    /// <returns><see langword="true"/> when all canonical keys are absent; otherwise <see langword="false"/>.</returns>
    public async Task<bool> AreAbsentAsync(
        string tenantId,
        string intakeId,
        CancellationToken cancellationToken)
    {
        foreach (string key in KeysFor(tenantId, intakeId))
        {
            (bool present, _) = await _readModelEraser.TryReadEtagAsync(
                ChatBotReadModelStoreNames.StateStoreName,
                key,
                cancellationToken).ConfigureAwait(false);
            if (present)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Checks that every canonical intake read model remains absent throughout a polling window.</summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="intakeId">The intake identifier.</param>
    /// <param name="window">The sustained-absence observation window.</param>
    /// <param name="cancellationToken">Cancels storage reads and polling delays.</param>
    /// <returns><see langword="true"/> when all canonical keys remain absent; otherwise <see langword="false"/>.</returns>
    public async Task<bool> RemainsAbsentAsync(
        string tenantId,
        string intakeId,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (!await AreAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        return await AreAbsentAsync(tenantId, intakeId, cancellationToken).ConfigureAwait(false);
    }
}
