using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Provides named, request-recording EventStore actor-state responses for recovery regressions.</summary>
internal sealed class RecoveryDurableStateMessageHandler : HttpMessageHandler
{
    private const string EventMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private readonly ConcurrentDictionary<string, string> _presentAggregates = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<(string TenantRef, string AggregateRef)> _requests = new();

    /// <summary>Gets every aggregate request in observation order, including repeated polling reads.</summary>
    internal IReadOnlyList<(string TenantRef, string AggregateRef)> Requests => [.. _requests];

    /// <summary>Gets or initializes a named callback invoked before each aggregate-state response.</summary>
    internal Action<string, string>? OnAggregateRead { get; init; }

    /// <summary>Marks a mailbox-intake aggregate present.</summary>
    /// <param name="tenantRef">The aggregate tenant.</param>
    /// <param name="aggregateRef">The aggregate identity.</param>
    internal void AddMailboxIntake(string tenantRef, string aggregateRef)
        => Add(tenantRef, aggregateRef, ".MailboxMessageIntakeCaptured");

    /// <summary>Marks a governed-note aggregate present.</summary>
    /// <param name="tenantRef">The aggregate tenant.</param>
    /// <param name="aggregateRef">The aggregate identity.</param>
    internal void AddGovernedNote(string tenantRef, string aggregateRef)
        => Add(tenantRef, aggregateRef, ".GovernedNoteRecorded");

    /// <summary>Marks an aggregate absent.</summary>
    /// <param name="tenantRef">The aggregate tenant.</param>
    /// <param name="aggregateRef">The aggregate identity.</param>
    internal void Remove(string tenantRef, string aggregateRef)
        => _presentAggregates.TryRemove(Key(tenantRef, aggregateRef), out _);

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (string tenantRef, string aggregateRef, bool metadata) = Parse(request.RequestUri);
        _requests.Enqueue((tenantRef, aggregateRef));
        OnAggregateRead?.Invoke(tenantRef, aggregateRef);
        if (!_presentAggregates.TryGetValue(Key(tenantRef, aggregateRef), out string? eventSuffix))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        string json = metadata
            ? "{\"currentSequence\":1}"
            : $$"""
                {"tenantId":"{{tenantRef}}","domain":"chatbot","aggregateId":"{{aggregateRef}}","sequenceNumber":1,"messageId":"{{EventMessageId}}","timestamp":"2026-08-01T00:00:00Z","eventTypeName":"Hexalith.ChatBot.Contracts.Events{{eventSuffix}}"}
                """;
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }

    private void Add(string tenantRef, string aggregateRef, string eventSuffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateRef);
        _presentAggregates[Key(tenantRef, aggregateRef)] = eventSuffix;
    }

    private static string Key(string tenantRef, string aggregateRef) => $"{tenantRef}\u001f{aggregateRef}";

    private static (string TenantRef, string AggregateRef, bool Metadata) Parse(Uri? requestUri)
    {
        ArgumentNullException.ThrowIfNull(requestUri);
        string[] segments = requestUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 6)
        {
            throw new InvalidOperationException("The durable-state test received an unexpected request path.");
        }

        string actorId = Uri.UnescapeDataString(segments[3]);
        int separator = actorId.IndexOf(":chatbot:", StringComparison.Ordinal);
        if (separator <= 0)
        {
            throw new InvalidOperationException("The durable-state test received an invalid actor identity.");
        }

        string tenantRef = actorId[..separator];
        string aggregateRef = actorId[(separator + ":chatbot:".Length)..];
        string stateKey = Uri.UnescapeDataString(segments[5]);
        return (tenantRef, aggregateRef, stateKey.EndsWith(":metadata", StringComparison.Ordinal));
    }
}
