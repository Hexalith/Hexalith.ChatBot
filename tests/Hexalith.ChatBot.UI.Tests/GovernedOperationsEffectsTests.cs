using Fluxor;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.UI.Services;
using Hexalith.ChatBot.UI.State.GovernedOperations;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>
/// Covers the submit effect's failure handling: a cancellation is never swallowed as a generic failure, and a
/// catalog-backed server problem surfaces its distinct (metadata-only) code instead of collapsing to one
/// generic message — while any other failure stays a single safe code with no raw exception text (NFR40).
/// </summary>
public sealed class GovernedOperationsEffectsTests
{
    [Fact]
    public async Task SubmitEffectShouldSurfaceDistinctServerProblemCodesInsteadOfCollapsingToOneMessage()
    {
        GovernedOperationsEffects effects = EffectsThatThrow(
            new HexalithChatBotApiException<ProblemDetails>(
                "Metadata-only authorization denial.",
                403,
                response: null,
                headers: new Dictionary<string, IEnumerable<string>>(),
                result: new ProblemDetails { Code = "refusal_blocked_action" },
                innerException: null));
        RecordingDispatcher dispatcher = new();

        await effects.HandleSubmitAsync(dispatcher);

        GovernedNoteSubmissionFailedAction failure = dispatcher.Actions.OfType<GovernedNoteSubmissionFailedAction>().Single();
        failure.Error.ShouldBe("refusal_blocked_action");
        failure.Error.ShouldNotBe(GovernedOperationsEffects.GenericFailureCode);
    }

    [Fact]
    public async Task SubmitEffectShouldRethrowCancellationAndNeverCollapseItToASubmissionFailure()
    {
        GovernedOperationsEffects effects = EffectsThatThrow(new OperationCanceledException());
        RecordingDispatcher dispatcher = new();

        await Should.ThrowAsync<OperationCanceledException>(() => effects.HandleSubmitAsync(dispatcher));

        dispatcher.Actions.OfType<GovernedNoteSubmissionFailedAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task SubmitEffectShouldCollapseUnknownFailuresToTheGenericSafeCodeWithNoRawText()
    {
        GovernedOperationsEffects effects = EffectsThatThrow(new InvalidOperationException("raw /home/secret exception text"));
        RecordingDispatcher dispatcher = new();

        await effects.HandleSubmitAsync(dispatcher);

        GovernedNoteSubmissionFailedAction failure = dispatcher.Actions.OfType<GovernedNoteSubmissionFailedAction>().Single();
        failure.Error.ShouldBe(GovernedOperationsEffects.GenericFailureCode);
        failure.Error.ShouldNotContain("raw", Case.Insensitive);
        failure.Error.ShouldNotContain("/home/", Case.Insensitive);
        failure.Error.ShouldNotContain("exception", Case.Insensitive);
    }

    private static GovernedOperationsEffects EffectsThatThrow(Exception exception)
        => new(new GovernedOperationService(new ThrowingChatBotClient(exception)));

    private sealed class ThrowingChatBotClient(Exception exception) : IChatBotClient
    {
        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
            => throw exception;

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RecordingDispatcher : IDispatcher
    {
        public List<object> Actions { get; } = [];

        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;

        public void Dispatch(object action)
        {
            Actions.Add(action);
            ActionDispatched?.Invoke(this, new ActionDispatchedEventArgs(action));
        }
    }
}
