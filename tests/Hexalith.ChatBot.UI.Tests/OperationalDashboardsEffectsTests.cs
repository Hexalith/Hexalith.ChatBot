using Fluxor;
using Hexalith.ChatBot.UI.State.OperationalDashboards;
using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Exercises dashboard load failure handling at its metadata-only read seam.</summary>
public sealed class OperationalDashboardsEffectsTests
{
    /// <summary>Preserves safe status buckets and excludes exception prose.</summary>
    /// <param name="kind">The client failure shape.</param>
    /// <param name="status">The received HTTP status.</param>
    /// <param name="expected">The safe catalog code.</param>
    [Theory]
    [MemberData(nameof(UiTransportFailureCases.Cases), MemberType = typeof(UiTransportFailureCases))]
    public async Task TransportFailureShouldUseStatusCatalogWithoutRawText(string kind, int status, string expected)
    {
        OperationalDashboardsEffects effects = new(() => throw UiTransportFailureCases.Create(kind, status));
        object? dispatched = null;
        IDispatcher dispatcher = new DashboardFailureDispatcher(action => dispatched = action);
        await effects.HandleLoadAsync(dispatcher);
        dispatched.ShouldBeOfType<OperationalDashboardLoadFailedAction>().Error.ShouldBe(expected);
    }
    /// <summary>Explicit cancellation stays a cancellation rather than a dependency failure.</summary>
    /// <param name="cancellationKind">The explicit cancellation source.</param>
    [Theory]
    [InlineData("operation")]
    [InlineData("linked")]
    [InlineData("canceled-task")]
    public async Task ExplicitCancellationShouldRethrowWithoutFailure(string cancellationKind)
    {
        Exception error = UiTransportFailureCases.Cancellation(cancellationKind);
        OperationalDashboardsEffects effects = new(() => throw error);
        object? dispatched = null;
        IDispatcher dispatcher = new DashboardFailureDispatcher(action => dispatched = action);
        await Should.ThrowAsync<OperationCanceledException>(() => effects.HandleLoadAsync(dispatcher)).ConfigureAwait(true);
        dispatched.ShouldBeNull();
    }

}
