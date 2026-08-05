using System.Diagnostics;
using System.Net;
using System.Text.Json;

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

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
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
            return false;
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
            GetString(persistedEvent.Value, "EventTypeName")?.EndsWith(
                expectedEventTypeSuffix,
                StringComparison.Ordinal) != true)
        {
            throw new InvalidOperationException($"EventStore event state for aggregate '{actorId}' is incomplete or inconsistent.");
        }

        return true;
    }

    public void Dispose() => _client.Dispose();

    private async Task<JsonElement?> ReadActorStateAsync(
        string actorId,
        string stateKey,
        CancellationToken cancellationToken)
    {
        string relative = $"v1.0/actors/AggregateActor/{Uri.EscapeDataString(actorId)}/state/{Uri.EscapeDataString(stateKey)}";
        using HttpResponseMessage response = await _client
            .GetAsync(relative, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
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

        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

    private static string? GetString(JsonElement element, string propertyName)
        => TryGetProperty(element, propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryGetInt64(JsonElement element, string propertyName, out long value)
    {
        value = default;
        return TryGetProperty(element, propertyName, out JsonElement property) && property.TryGetInt64(out value);
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
