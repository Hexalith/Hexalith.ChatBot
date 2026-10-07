using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Advances a test clock only after the selected real durable boundary returns.</summary>
internal sealed class TrustedAuthorityAdvancingIdempotencyStore(IIdempotencyStore inner, Action<string> after) : IIdempotencyStore
{
    /// <summary>Records the real boundaries reached before injecting clock movement.</summary>
    public List<string> Boundaries { get; } = [];

    /// <inheritdoc/>
    public async ValueTask<CoarseIdempotencyDecision> RecordAdmissionAsync(ChatBotGatewayContext context, CancellationToken token)
    {
        CoarseIdempotencyDecision result = await inner.RecordAdmissionAsync(context, token).ConfigureAwait(false);
        await Task.Yield();
        Boundaries.Add("admission");
        after("admission");
        return result;
    }
    /// <inheritdoc/>
    public async ValueTask<bool> PrepareDispatchAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token)
    {
        bool result = await inner.PrepareDispatchAsync(metadata, outcome, token).ConfigureAwait(false);
        await Task.Yield();
        Boundaries.Add("preparation");
        after("preparation");
        return result;
    }
    /// <inheritdoc/>
    public async ValueTask<bool> BindDispatchTargetAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string target, CancellationToken token)
    {
        bool result = await inner.BindDispatchTargetAsync(metadata, outcome, target, token).ConfigureAwait(false);
        await Task.Yield();
        Boundaries.Add("binding");
        after("binding");
        return result;
    }
    /// <inheritdoc/>
    public ValueTask<bool> ConfirmSdkSubmissionAcceptedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, string target, CancellationToken token)
        => inner.ConfirmSdkSubmissionAcceptedAsync(metadata, outcome, target, token);
    /// <inheritdoc/>
    public ValueTask RecordOutcomeAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => inner.RecordOutcomeAsync(metadata, outcome, token);
    /// <inheritdoc/>
    public async ValueTask AbortAdmissionAsync(CoarseIdempotencyMetadata metadata, CancellationToken token)
    {
        await inner.AbortAdmissionAsync(metadata, token).ConfigureAwait(false);
        await Task.Yield();
        Boundaries.Add("abort");
        after("abort");
    }
    /// <inheritdoc/>
    public ValueTask AbortUndispatchedAsync(CoarseIdempotencyMetadata metadata, CommandSubmissionResponse outcome, CancellationToken token) => inner.AbortUndispatchedAsync(metadata, outcome, token);
}
