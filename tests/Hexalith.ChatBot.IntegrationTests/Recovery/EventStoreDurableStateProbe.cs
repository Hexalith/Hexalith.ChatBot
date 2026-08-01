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
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(daprHttpEndpoint.ToString().TrimEnd('/') + '/', UriKind.Absolute),
        Timeout = TimeSpan.FromSeconds(10),
    };

    public async Task WaitForMailboxIntakeAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (await IsMailboxIntakeCommittedAsync(tenantRef, intakeRef, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Mailbox intake aggregate '{tenantRef}:chatbot:{intakeRef}' did not materialize in EventStore actor state.");
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
    {
        Stopwatch timer = Stopwatch.StartNew();
        do
        {
            if (await IsMailboxIntakeCommittedAsync(tenantRef, intakeRef, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (timer.Elapsed < window);

        return true;
    }

    public async Task<bool> IsMailboxIntakeCommittedAsync(
        string tenantRef,
        string intakeRef,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(intakeRef);

        string actorId = $"{tenantRef}:chatbot:{intakeRef}";
        JsonElement? metadata = await ReadActorStateAsync(
            actorId,
            $"{actorId}:metadata",
            cancellationToken).ConfigureAwait(false);
        if (metadata is null || !TryGetInt64(metadata.Value, "CurrentSequence", out long currentSequence) || currentSequence < 1)
        {
            return false;
        }

        JsonElement? persistedEvent = await ReadActorStateAsync(
            actorId,
            $"{actorId}:events:1",
            cancellationToken).ConfigureAwait(false);
        return persistedEvent is not null &&
            string.Equals(GetString(persistedEvent.Value, "TenantId"), tenantRef, StringComparison.Ordinal) &&
            string.Equals(GetString(persistedEvent.Value, "Domain"), "chatbot", StringComparison.Ordinal) &&
            string.Equals(GetString(persistedEvent.Value, "AggregateId"), intakeRef, StringComparison.Ordinal) &&
            GetString(persistedEvent.Value, "EventTypeName")?.EndsWith(
                ".MailboxMessageIntakeCaptured",
                StringComparison.Ordinal) == true;
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
        using JsonDocument document = JsonDocument.Parse(content);
        return document.RootElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            ? null
            : document.RootElement.Clone();
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
