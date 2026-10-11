using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults.Authentication;

namespace Hexalith.ChatBot.Server.Gateway;

/// <summary>Moves ChatBot's admitted EventStore writes onto the signed workload route.</summary>
internal sealed class ChatBotBoundCommandHandler(
    IWorkloadAssertionIssuer issuer,
    ChatBotStreamReadProofSource proofs) : DelegatingHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Method != HttpMethod.Post || !IsPublicCommandPath(request.RequestUri))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        SubmitCommandRequest? command = request.Content is null ? null
            : await request.Content.ReadFromJsonAsync<SubmitCommandRequest>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);
        if (command?.Extensions is not { } extensions ||
            !extensions.TryGetValue("actorId", out string? actorId) ||
            !AuditMetadata.IsSafeStableIdentifier(actorId) ||
            !extensions.TryGetValue(DataProtectionChatBotAdmissionMarker.ExtensionKey, out string? marker) ||
            string.IsNullOrWhiteSpace(marker))
        {
            throw new InvalidOperationException("Bound ChatBot command admission evidence is unavailable.");
        }

        var bound = new BoundWorkloadCommandRequest(command, actorId);
        string? proof = proofs.IssueCommand(bound);
        string? assertion = proof is null ? null : await issuer.IssueAsync(
            new WorkloadAssertionRequest("eventstore", EventStoreGatewayWorkloadOperations.CommandSubmit),
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(proof) || string.IsNullOrWhiteSpace(assertion))
        {
            throw new InvalidOperationException("Bound EventStore command credentials are unavailable.");
        }

        request.Content?.Dispose();
        request.Content = JsonContent.Create(bound, options: JsonOptions);
        request.RequestUri = BoundPath(request.RequestUri!);
        request.Headers.TryAddWithoutValidation(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        request.Headers.TryAddWithoutValidation(BoundWorkloadCommandProof.HeaderName, proof);
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsPublicCommandPath(Uri? uri)
        => uri is not null && string.Equals(
            uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString.TrimStart('/'),
            uri.IsAbsoluteUri ? "/api/v1/commands" : "api/v1/commands",
            StringComparison.Ordinal);

    private static Uri BoundPath(Uri original)
    {
        if (!original.IsAbsoluteUri)
        {
            return new Uri("api/v1/commands/bound/workload", UriKind.Relative);
        }

        UriBuilder builder = new(original) { Path = "/api/v1/commands/bound/workload", Query = string.Empty };
        return builder.Uri;
    }
}
