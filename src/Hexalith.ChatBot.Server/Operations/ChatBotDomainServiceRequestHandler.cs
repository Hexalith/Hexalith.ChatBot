using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Microsoft.AspNetCore.Http;

namespace Hexalith.ChatBot.Server.Operations;

/// <summary>
/// Inbound domain-service handler invoked by the EventStore aggregate actor (via DAPR service invocation at
/// the <c>process</c> method) for the <c>chatbot</c> domain. It resolves the single registered domain
/// processor — the Pattern-A <see cref="GovernedOperationAggregate"/>, which IS the <c>IDomainProcessor</c> —
/// runs its pure <c>Handle</c>/<c>Apply</c> pipeline, and returns a wire-safe result the actor persists and
/// publishes.
/// </summary>
/// <remarks>
/// Authorization, tenant binding, fail-closed audit and coarse idempotency already ran at the
/// <c>CommandGateway</c> (the single spine) before the command reached EventStore; this endpoint is reachable
/// only by the EventStore app under the deny-by-default DAPR access-control policy, so it does not re-run
/// authorization. <c>Handle</c> is pure and never throws for a business-rule violation (it returns a structured
/// rejection), so the wire result faithfully carries success events or a rejection.
/// </remarks>
internal sealed class ChatBotDomainServiceRequestHandler(IEnumerable<IDomainProcessor> processors)
{
    private readonly IReadOnlyList<IDomainProcessor> _processors = [.. processors];

    /// <summary>Runs the single registered domain processor against the inbound command + replayed state.</summary>
    /// <param name="request">The domain-service request (command envelope + current aggregate state).</param>
    /// <returns>A wire-safe <see cref="DomainServiceWireResult"/>, or a metadata-only problem on misconfiguration.</returns>
    public async Task<IResult> ProcessAsync(DomainServiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_processors.Count == 0)
        {
            return Results.Problem(
                title: "No ChatBot domain processor is registered.",
                statusCode: StatusCodes.Status501NotImplemented);
        }

        if (_processors.Count > 1)
        {
            return Results.Problem(
                title: "Multiple ChatBot domain processors are registered; the dispatcher requires exactly one.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        DomainResult result = await _processors[0]
            .ProcessAsync(request.Command, request.CurrentState)
            .ConfigureAwait(false);

        return Results.Ok(DomainServiceWireResult.FromDomainResult(result));
    }
}
