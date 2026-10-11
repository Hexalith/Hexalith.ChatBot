using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Reads owner streams only through EventStore's dual-proof workload route.</summary>
internal sealed class ChatBotBoundOwnerStreamGateway(
    HttpClient http,
    IWorkloadAssertionIssuer issuer,
    ChatBotStreamReadProofSource proofSource) : IEventStoreGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc/>
    public async Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        string? proof = proofSource.Issue(request);
        string? assertion = proof is null ? null : await issuer.IssueAsync(
            new WorkloadAssertionRequest("eventstore", "eventstore:stream-read"), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(proof) || string.IsNullOrWhiteSpace(assertion))
        {
            throw new InvalidOperationException("Bound EventStore stream read credentials are unavailable.");
        }

        using HttpRequestMessage message = new(HttpMethod.Post, "api/v1/streams/read/workload")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        message.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        message.Headers.TryAddWithoutValidation("X-Hexalith-Bound-Stream-Read-Proof", proof);
        using HttpResponseMessage response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        StreamReadPage? page = await response.Content.ReadFromJsonAsync<StreamReadPage>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (page is null || page.Tenant != request.Tenant || page.Domain != request.Domain || page.AggregateId != request.AggregateId)
        {
            throw new InvalidOperationException("EventStore returned a mismatched owner stream page.");
        }

        return page;
    }

    /// <summary>Reads only an explicitly bound owner evidence query.</summary>
    public async Task<SubmitQueryResponse> ReadOwnerAuthorityAsync(SubmitQueryRequest query, string principalId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
        var bound = new BoundOwnerQueryRequest(query, principalId);
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(bound, JsonOptions);
        string digest = Convert.ToHexString(SHA256.HashData(canonical));
        string? proof = proofSource.Issue(new StreamReadRequest(query.Tenant, query.Domain, digest));
        string? assertion = proof is null ? null : await issuer.IssueAsync(
            new WorkloadAssertionRequest("eventstore", "eventstore:owner-authority-query"), cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(proof) || string.IsNullOrWhiteSpace(assertion))
        {
            throw new InvalidOperationException("Bound owner query credentials are unavailable.");
        }

        using HttpRequestMessage message = new(HttpMethod.Post, "api/v1/queries/owner-authority/workload")
        {
            Content = JsonContent.Create(bound, options: JsonOptions),
        };
        message.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        message.Headers.TryAddWithoutValidation("X-Hexalith-Bound-Owner-Query-Proof", proof);
        using HttpResponseMessage response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        SubmitQueryResponse? result = await response.Content.ReadFromJsonAsync<SubmitQueryResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (result is null || !result.Success || result.Payload.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Owner query returned no current evidence.");
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        => Task.FromException<SubmitCommandResponse>(new NotSupportedException("Authority reader is read only."));

    /// <inheritdoc/>
    public Task<CommandStatusQueryResponse?> GetCommandStatusAsync(string messageId, CancellationToken cancellationToken = default)
        => Task.FromException<CommandStatusQueryResponse?>(new NotSupportedException("Authority reader is read only."));

    /// <inheritdoc/>
    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => Task.FromException<EventStoreQueryResult>(new NotSupportedException("Authority reader is read only."));

    /// <inheritdoc/>
    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => Task.FromException<EventStoreQueryResult<T>>(new NotSupportedException("Authority reader is read only."));

    private sealed record BoundOwnerQueryRequest(SubmitQueryRequest Query, string PrincipalId);
}
