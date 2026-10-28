using System.Diagnostics;
using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Reads EventStore aggregate state through the owning DAPR sidecar's actor-state API. This keeps recovery
/// assertions on the durable write path and does not depend on an eventually delivered projection.
/// </summary>
internal sealed class EventStoreDurableStateProbe(Uri daprHttpEndpoint) : IDisposable
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan DefaultPresenceWindow = TimeSpan.FromMinutes(1);
    private readonly HttpClient _client = CreateClient(daprHttpEndpoint);
    private readonly TimeSpan _pollInterval = DefaultPollInterval;
    private readonly TimeSpan _presenceWindow = DefaultPresenceWindow;

    /// <summary>Gets the scheduling window for repeated materialization observations.</summary>
    /// <remarks>
    /// The strict final observation has its own request bound. The enclosing live caller's restoration cancellation
    /// supplies the hard overall cap, including that final observation.
    /// </remarks>
    internal TimeSpan PresenceWindow => _presenceWindow;

    /// <summary>Gets the bound for one actor-state HTTP request, including reading its response body.</summary>
    internal TimeSpan RequestTimeout => _client.Timeout;

    /// <summary>Creates a live probe whose materialization and request bounds follow the recovery lane.</summary>
    /// <param name="daprHttpEndpoint">The owning EventStore DAPR sidecar endpoint.</param>
    /// <param name="options">The live recovery lane's configured restoration budget.</param>
    internal EventStoreDurableStateProbe(Uri daprHttpEndpoint, LiveRecoveryValidationOptions options)
        : this(daprHttpEndpoint)
    {
        _presenceWindow = RestorationWindow(options);
        _client.Timeout = _presenceWindow;
    }

    /// <summary>Creates an infrastructure-free probe using the live recovery lane's bounds.</summary>
    /// <param name="daprHttpEndpoint">The owning EventStore DAPR sidecar endpoint.</param>
    /// <param name="handler">The transport used to observe actor-state requests.</param>
    /// <param name="options">The live recovery lane's configured restoration budget.</param>
    internal EventStoreDurableStateProbe(
        Uri daprHttpEndpoint,
        HttpMessageHandler handler,
        LiveRecoveryValidationOptions options)
        : this(daprHttpEndpoint, handler, RestorationWindow(options), DefaultPollInterval)
    {
        _client.Timeout = _presenceWindow;
    }

    internal EventStoreDurableStateProbe(
        Uri daprHttpEndpoint,
        HttpMessageHandler handler,
        TimeSpan presenceWindow,
        TimeSpan pollInterval)
        : this(daprHttpEndpoint)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (presenceWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(presenceWindow));
        }

        if (pollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval));
        }

        _client.Dispose();
        _client = CreateClient(daprHttpEndpoint, handler);
        _presenceWindow = presenceWindow;
        _pollInterval = pollInterval;
    }

    public async Task WaitForMailboxIntakeAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
        => await WaitForAggregateAsync(
            tenantRef,
            intakeRef,
            ".MailboxMessageIntakeCaptured",
            cancellationToken).ConfigureAwait(false);

    /// <summary>Waits for and returns the authoritative persisted envelope for a mailbox-intake aggregate.</summary>
    public async Task<DurableCommitObservation> WaitForMailboxIntakeCommitAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
    {
        await WaitForMailboxIntakeAsync(tenantRef, intakeRef, cancellationToken).ConfigureAwait(false);
        return await ReadMailboxIntakeCommitAsync(tenantRef, intakeRef, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The witnessed mailbox-intake commit disappeared from EventStore actor state.");
    }

    public async Task WaitForGovernedNoteAsync(
        string tenantRef,
        string noteRef,
        CancellationToken cancellationToken)
        => await WaitForAggregateAsync(
            tenantRef,
            noteRef,
            ".GovernedNoteRecorded",
            cancellationToken).ConfigureAwait(false);

    private async Task WaitForAggregateAsync(
        string tenantRef,
        string aggregateRef,
        string expectedEventTypeSuffix,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < _presenceWindow)
        {
            if (await TryIsAggregateCommittedTolerantlyAsync(
                tenantRef,
                aggregateRef,
                expectedEventTypeSuffix,
                cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            TimeSpan remaining = _presenceWindow - timer.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(remaining < _pollInterval ? remaining : _pollInterval, cancellationToken).ConfigureAwait(false);
        }

        // Close the same final-delay race RemainsAggregateAbsentAsync closes, and — unlike the tolerant mid-window
        // polls above — do NOT swallow a persistent inconsistency here. A commit that lands during the last delay
        // must still be observed as present, and a still-inconsistent read at the very end of the window is
        // anomalous rather than merely slow: it must fail closed with its real diagnostic, not a generic timeout.
        if (await IsAggregateCommittedAsync(
            tenantRef,
            aggregateRef,
            expectedEventTypeSuffix,
            cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        throw new TimeoutException(
            $"Aggregate '{tenantRef}:chatbot:{aggregateRef}' did not materialize in EventStore actor state.");
    }

    /// <summary>
    /// Polls <see cref="IsAggregateCommittedAsync"/> while a genuinely transient metadata/event-content mismatch —
    /// the two actor-state keys committing via separate writes on an eventually-consistent read path — is tolerated
    /// as "not yet observed" rather than failing the whole wait closed on its first inconsistent read. One-shot
    /// callers (<see cref="IsMailboxIntakeCommittedAsync"/>, <see cref="IsGovernedNoteCommittedAsync"/>) deliberately
    /// do not get this tolerance: outside a sustained poll, an inconsistent read must fail closed rather than be
    /// silently reinterpreted as absence.
    /// </summary>
    private async Task<bool> TryIsAggregateCommittedTolerantlyAsync(
        string tenantRef,
        string aggregateRef,
        string expectedEventTypeSuffix,
        CancellationToken cancellationToken)
    {
        try
        {
            return await IsAggregateCommittedAsync(
                tenantRef,
                aggregateRef,
                expectedEventTypeSuffix,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Polls for the full <paramref name="window"/> and returns <see langword="true"/> only if the aggregate never
    /// materializes. Absence on an eventually-consistent read path is a claim about a period of time, not about one
    /// instant: a single read after a fixed short sleep reported "no duplicate" for a duplicate that committed a
    /// moment later, which is precisely the breach this probe exists to catch.
    /// </summary>
    public async Task<bool> RemainsAbsentAsync(
        string tenantRef,
        string intakeRef,
        TimeSpan window,
        CancellationToken cancellationToken)
        => await RemainsAggregateAbsentAsync(
            tenantRef,
            intakeRef,
            ".MailboxMessageIntakeCaptured",
            window,
            cancellationToken).ConfigureAwait(false);

    public async Task<bool> RemainsGovernedNoteAbsentAsync(
        string tenantRef,
        string noteRef,
        TimeSpan window,
        CancellationToken cancellationToken)
        => await RemainsAggregateAbsentAsync(
            tenantRef,
            noteRef,
            ".GovernedNoteRecorded",
            window,
            cancellationToken).ConfigureAwait(false);

    private async Task<bool> RemainsAggregateAbsentAsync(
        string tenantRef,
        string aggregateRef,
        string expectedEventTypeSuffix,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), window, "Absence observation requires a positive window.");
        }

        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            // Tolerate a transient metadata/event-content mismatch mid-window: it can be a commit still settling
            // across the two separately-written actor-state keys, which the next iteration (or the final closing
            // read below) will observe once consistent, rather than a reason to abort the whole sustained-absence
            // assertion.
            if (await TryIsAggregateCommittedTolerantlyAsync(
                tenantRef,
                aggregateRef,
                expectedEventTypeSuffix,
                cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        // A commit can land during the final delay after the loop condition was last checked. One final observation
        // closes that boundary instead of turning the sleep itself into proof of absence. Unlike the mid-window
        // polls above, this closing read is NOT tolerant: a still-inconsistent read at the very end of the window is
        // anomalous rather than merely slow, and must fail closed like any other one-shot check.
        return !await IsAggregateCommittedAsync(
            tenantRef,
            aggregateRef,
            expectedEventTypeSuffix,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsMailboxIntakeCommittedAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
        => await IsAggregateCommittedAsync(
            tenantRef,
            intakeRef,
            ".MailboxMessageIntakeCaptured",
            cancellationToken).ConfigureAwait(false);

    /// <summary>Reads a validated mailbox-intake durable envelope, or null only when no aggregate metadata exists.</summary>
    public async Task<DurableCommitObservation?> ReadMailboxIntakeCommitAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
        => await ReadAggregateCommitAsync(
            tenantRef,
            intakeRef,
            ".MailboxMessageIntakeCaptured",
            cancellationToken).ConfigureAwait(false);

    public async Task<bool> IsGovernedNoteCommittedAsync(
        string tenantRef,
        string noteRef,
        CancellationToken cancellationToken)
        => await IsAggregateCommittedAsync(
            tenantRef,
            noteRef,
            ".GovernedNoteRecorded",
            cancellationToken).ConfigureAwait(false);

    private async Task<bool> IsAggregateCommittedAsync(
        string tenantRef,
        string aggregateRef,
        string expectedEventTypeSuffix,
        CancellationToken cancellationToken)
        => await ReadAggregateCommitAsync(
            tenantRef,
            aggregateRef,
            expectedEventTypeSuffix,
            cancellationToken).ConfigureAwait(false) is not null;

    private async Task<DurableCommitObservation?> ReadAggregateCommitAsync(
        string tenantRef,
        string aggregateRef,
        string expectedEventTypeSuffix,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedEventTypeSuffix);

        string actorId = $"{tenantRef}:chatbot:{aggregateRef}";
        JsonElement? metadata = await ReadActorStateAsync(
            actorId,
            $"{actorId}:metadata",
            cancellationToken).ConfigureAwait(false);
        if (metadata is null)
        {
            return null;
        }

        if (!TryGetInt64(metadata.Value, "CurrentSequence", out long currentSequence) || currentSequence < 1)
        {
            throw new InvalidOperationException($"EventStore metadata for aggregate '{actorId}' is incomplete or invalid.");
        }

        JsonElement? persistedEvent = await ReadActorStateAsync(
            actorId,
            $"{actorId}:events:1",
            cancellationToken).ConfigureAwait(false);
        if (persistedEvent is null ||
            !string.Equals(GetString(persistedEvent.Value, "TenantId"), tenantRef, StringComparison.Ordinal) ||
            !string.Equals(GetString(persistedEvent.Value, "Domain"), "chatbot", StringComparison.Ordinal) ||
            !string.Equals(GetString(persistedEvent.Value, "AggregateId"), aggregateRef, StringComparison.Ordinal) ||
            !TryGetInt64(persistedEvent.Value, "SequenceNumber", out long sequenceNumber) ||
            sequenceNumber != 1 || currentSequence < sequenceNumber ||
            GetString(persistedEvent.Value, "MessageId") is not { } eventRef ||
            !RecoveryValidationEvidenceManifest.IsCanonicalUlid(eventRef) ||
            !TryGetDateTimeOffset(persistedEvent.Value, "Timestamp", out DateTimeOffset committedAtUtc) ||
            committedAtUtc.Offset != TimeSpan.Zero ||
            GetString(persistedEvent.Value, "EventTypeName")?.EndsWith(
                expectedEventTypeSuffix,
                StringComparison.Ordinal) != true)
        {
            throw new InvalidOperationException($"EventStore event state for aggregate '{actorId}' is incomplete or inconsistent.");
        }

        return new DurableCommitObservation(tenantRef, aggregateRef, eventRef, sequenceNumber, committedAtUtc);
    }

    public void Dispose() => _client.Dispose();

    private async Task<JsonElement?> ReadActorStateAsync(
        string actorId,
        string stateKey,
        CancellationToken cancellationToken)
    {
        // ResponseHeadersRead ends HttpClient's timeout at the headers. Keep the same independent request budget
        // active while reading the actor-state body as well, linked to any shorter caller restoration deadline.
        using CancellationTokenSource requestDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestDeadline.CancelAfter(RequestTimeout);
        CancellationToken requestToken = requestDeadline.Token;
        string relative = $"v1.0/actors/AggregateActor/{Uri.EscapeDataString(actorId)}/state/{Uri.EscapeDataString(stateKey)}";
        using HttpResponseMessage response = await _client
            .GetAsync(relative, HttpCompletionOption.ResponseHeadersRead, requestToken)
            .ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"EventStore actor-state probe returned HTTP {(int)response.StatusCode} for aggregate '{actorId}'.");
        }

        string content = await response.Content.ReadAsStringAsync(requestToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                $"EventStore actor-state probe returned an empty successful body for aggregate '{actorId}'.");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            if (document.RootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                throw new InvalidOperationException(
                    $"EventStore actor-state probe returned a null successful body for aggregate '{actorId}'.");
            }

            return document.RootElement.Clone();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"EventStore actor-state probe returned malformed JSON for aggregate '{actorId}'.",
                exception);
        }
    }

    private static HttpClient CreateClient(Uri endpoint, HttpMessageHandler? handler = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        HttpClient client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: true);
        client.BaseAddress = new Uri(endpoint.ToString().TrimEnd('/') + '/', UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(10);
        return client;
    }

    /// <summary>Reads the positive restoration budget without requiring destructive-lane activation.</summary>
    private static TimeSpan RestorationWindow(LiveRecoveryValidationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.RestorationTimeout, TimeSpan.Zero);
        return options.RestorationTimeout;
    }

    private static string? GetString(JsonElement element, string propertyName)
        => TryGetProperty(element, propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryGetInt64(JsonElement element, string propertyName, out long value)
    {
        value = default;
        return TryGetProperty(element, propertyName, out JsonElement property) && property.TryGetInt64(out value);
    }

    private static bool TryGetDateTimeOffset(JsonElement element, string propertyName, out DateTimeOffset value)
    {
        value = default;
        return TryGetProperty(element, propertyName, out JsonElement property) &&
            property.ValueKind == JsonValueKind.String &&
            property.TryGetDateTimeOffset(out value);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        string camelCase = $"{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}";
        return element.TryGetProperty(camelCase, out value);
    }
}
