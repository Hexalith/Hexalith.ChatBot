using System.Reflection;

using Hexalith.ChatBot.UI.State.ProjectConversation;

using Microsoft.AspNetCore.SignalR.Client;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

public sealed class ProjectConversationStreamingSubscriberTests
{
    [Fact]
    public async Task ClosedHubShouldAutomaticallyBuildJoinAndNotifyRecoveryWithoutUiActivity()
    {
        FakeHubConnection first = new();
        FakeHubConnection recovered = new();
        QueueHubConnectionFactory factory = new(first, recovered);
        int recoveryNotifications = 0;
        ProjectConversationStreamingSubscriber subscriber = new(
            new ChatBotHubEndpoint(new Uri("https://chatbot.example/base/")),
            factory,
            TimeSpan.FromMilliseconds(1));
        await using var cleanup = subscriber.ConfigureAwait(true);
        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => Task.CompletedTask, () => { recoveryNotifications++; return Task.CompletedTask; });

        await first.TriggerClosedAsync();
        await WaitUntilAsync(() => recovered.StartCount == 1 && recoveryNotifications == 1);

        factory.CreateCount.ShouldBe(2);
        recovered.JoinedTenants.ShouldBe(["tenant-alpha"]);
    }

    [Fact]
    public async Task FailedRejoinShouldDiscardConnectedHubAndAutomaticallyBuildReplacement()
    {
        FakeHubConnection failedRejoin = new(joinOutcomes: [true, false]);
        FakeHubConnection recovered = new();
        QueueHubConnectionFactory factory = new(failedRejoin, recovered);
        int recoveryNotifications = 0;
        ProjectConversationStreamingSubscriber subscriber = new(
            new ChatBotHubEndpoint(new Uri("https://chatbot.example/base/")),
            factory,
            TimeSpan.FromMilliseconds(1));
        await using var cleanup = subscriber.ConfigureAwait(true);
        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => Task.CompletedTask, () => { recoveryNotifications++; return Task.CompletedTask; });

        await failedRejoin.TriggerReconnectedAsync();
        await WaitUntilAsync(() => recovered.StartCount == 1 && recoveryNotifications == 1);

        failedRejoin.DisposeCount.ShouldBe(1);
        factory.CreateCount.ShouldBe(2);
        recovered.JoinedTenants.ShouldBe(["tenant-alpha"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NotificationShouldAwaitAndContainAsynchronousCallbackFailure(bool reconnect)
    {
        FakeHubConnection connection = new();
        ProjectConversationStreamingSubscriber subscriber = new(
            new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), new QueueHubConnectionFactory(connection));
        await using var cleanup = subscriber.ConfigureAwait(true);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource complete = new(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task CallbackAsync()
        {
            entered.SetResult();
            await complete.Task.ConfigureAwait(true);
            throw new ObjectDisposedException("renderer");
        }

        await subscriber.EnsureSubscribedAsync("tenant-alpha", CallbackAsync, CallbackAsync);
        Task notification = reconnect ? connection.TriggerReconnectedAsync() : connection.TriggerChangedAsync("tenant-alpha");
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        notification.IsCompleted.ShouldBeFalse();
        complete.SetResult();
        await notification.ConfigureAwait(true);
        connection.DisposeCount.ShouldBe(0);
    }

    [Fact]
    public async Task ReplacedAndDisposedConnectionsShouldNotNotifyOrDisposeTheCurrentSubscription()
    {
        FakeHubConnection first = new();
        FakeHubConnection current = new();
        QueueHubConnectionFactory factory = new(first, current);
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), factory);
        int changed = 0;
        int reconnected = 0;
        Task ChangedAsync() { changed++; return Task.CompletedTask; }
        Task ReconnectedAsync() { reconnected++; return Task.CompletedTask; }
        await subscriber.EnsureSubscribedAsync("tenant-alpha", ChangedAsync, ReconnectedAsync);
        await subscriber.EnsureSubscribedAsync("tenant-beta", ChangedAsync, ReconnectedAsync);

        await first.TriggerChangedAsync("tenant-beta");
        await first.TriggerReconnectedAsync();
        await current.TriggerChangedAsync("tenant-alpha");
        changed.ShouldBe(0);
        reconnected.ShouldBe(0);
        current.DisposeCount.ShouldBe(0);
        current.JoinedTenants.ShouldBe(["tenant-beta"]);
        await current.TriggerChangedAsync("tenant-beta");
        await current.TriggerReconnectedAsync();
        changed.ShouldBe(1);
        reconnected.ShouldBe(1);

        await subscriber.DisposeAsync();
        await subscriber.DisposeAsync();
        await current.TriggerChangedAsync("tenant-beta");
        await current.TriggerReconnectedAsync();
        await current.TriggerClosedAsync();
        await subscriber.EnsureSubscribedAsync("tenant-gamma", ChangedAsync, ReconnectedAsync);
        changed.ShouldBe(1);
        reconnected.ShouldBe(1);
        current.DisposeCount.ShouldBe(1);
        factory.CreateCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReconnectFinishingAfterDisposalShouldNotNotify(bool joinSucceeds)
    {
        FakeHubConnection connection = new();
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), new QueueHubConnectionFactory(connection));
        int reconnected = 0;
        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => Task.CompletedTask, () => { reconnected++; return Task.CompletedTask; });
        TaskCompletionSource releaseJoin = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.JoinDelay = releaseJoin.Task;
        connection.NextJoinFails = !joinSucceeds;
        Task notification = connection.TriggerReconnectedAsync();
        notification.IsCompleted.ShouldBeFalse();

        await subscriber.DisposeAsync();
        releaseJoin.SetResult();
        await notification.ConfigureAwait(true);
        reconnected.ShouldBe(0);
        connection.DisposeCount.ShouldBe(1);
    }

    [Fact]
    public async Task SameTenantRebindShouldUseTheLatestProjectCallbacks()
    {
        FakeHubConnection connection = new();
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), new QueueHubConnectionFactory(connection));
        await using var cleanup = subscriber.ConfigureAwait(true);
        int oldCalls = 0;
        int currentCalls = 0;
        Task OldAsync() { oldCalls++; return Task.CompletedTask; }
        Task CurrentAsync() { currentCalls++; return Task.CompletedTask; }
        await subscriber.EnsureSubscribedAsync("tenant-alpha", OldAsync, OldAsync);
        await subscriber.EnsureSubscribedAsync("tenant-alpha", CurrentAsync, CurrentAsync);
        await connection.TriggerChangedAsync("tenant-alpha");
        await connection.TriggerReconnectedAsync();
        oldCalls.ShouldBe(0);
        currentCalls.ShouldBe(2);
        connection.StartCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConnectionCreatedDuringDisposalShouldBeDisposedWithoutStarting()
    {
        FakeHubConnection connection = new();
        QueueHubConnectionFactory factory = new(connection);
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), factory);
        ValueTask disposal = default;
        factory.BeforeCreateReturns = () => { disposal = subscriber.DisposeAsync(); };

        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => Task.CompletedTask, () => Task.CompletedTask);
        await disposal.ConfigureAwait(true);
        connection.StartCount.ShouldBe(0);
        connection.DisposeCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentSubscriptionsShouldSerializeStartupAndKeepReplacementOwnership(bool firstStartFails)
    {
        TaskCompletionSource releaseStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeHubConnection first = new() { StartDelay = releaseStart.Task, StartFails = firstStartFails };
        FakeHubConnection current = new();
        QueueHubConnectionFactory factory = new(first, current);
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), factory, TimeSpan.FromMinutes(1));
        await using var cleanup = subscriber.ConfigureAwait(true);
        int oldCalls = 0;
        int currentCalls = 0;
        Task OldAsync() { oldCalls++; return Task.CompletedTask; }
        Task CurrentAsync() { currentCalls++; return Task.CompletedTask; }
        Task firstSubscription = subscriber.EnsureSubscribedAsync("tenant-alpha", OldAsync, OldAsync);
        Task replacement = subscriber.EnsureSubscribedAsync("tenant-beta", CurrentAsync, CurrentAsync);
        factory.CreateCount.ShouldBe(1);
        firstSubscription.IsCompleted.ShouldBeFalse();
        replacement.IsCompleted.ShouldBeFalse();

        releaseStart.SetResult();
        await Task.WhenAll(firstSubscription, replacement).ConfigureAwait(true);
        first.DisposeCount.ShouldBe(1);
        current.DisposeCount.ShouldBe(0);
        current.JoinedTenants.ShouldBe(["tenant-beta"]);
        await first.TriggerReconnectedAsync();
        await current.TriggerChangedAsync("tenant-beta");
        await current.TriggerReconnectedAsync();
        oldCalls.ShouldBe(0);
        currentCalls.ShouldBe(2);
        current.DisposeCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueuedRecoveryShouldUseTheLatestTenantAndProjectCallbacks(bool changeTenant)
    {
        FakeHubConnection first = new();
        FakeHubConnection current = new();
        QueueHubConnectionFactory factory = new(first, current);
        ProjectConversationStreamingSubscriber subscriber = new(new ChatBotHubEndpoint(new Uri("https://chatbot.example/")), factory);
        await using var cleanup = subscriber.ConfigureAwait(true);
        int oldCalls = 0;
        int currentCalls = 0;
        Task OldAsync() { oldCalls++; return Task.CompletedTask; }
        Task CurrentAsync() { currentCalls++; return Task.CompletedTask; }
        await subscriber.EnsureSubscribedAsync("tenant-alpha", OldAsync, OldAsync);
        SemaphoreSlim gate = (SemaphoreSlim)typeof(ProjectConversationStreamingSubscriber)
            .GetField("_subscriptionChanges", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(subscriber)!;
        await gate.WaitAsync(TestContext.Current.CancellationToken);
        string tenant = changeTenant ? "tenant-beta" : "tenant-alpha";
        Task newerSubscription = subscriber.EnsureSubscribedAsync(tenant, CurrentAsync, CurrentAsync);
        Task<bool> recovery = (Task<bool>)typeof(ProjectConversationStreamingSubscriber)
            .GetMethod("RecoverCurrentSubscriptionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(subscriber, null)!;
        newerSubscription.IsCompleted.ShouldBeFalse();
        recovery.IsCompleted.ShouldBeFalse();
        _ = gate.Release();
        await newerSubscription.ConfigureAwait(true);
        (await recovery.ConfigureAwait(true)).ShouldBeTrue();
        FakeHubConnection active = changeTenant ? current : first;
        await active.TriggerChangedAsync(tenant);
        await active.TriggerReconnectedAsync();
        oldCalls.ShouldBe(0);
        currentCalls.ShouldBe(2);
        factory.CreateCount.ShouldBe(changeTenant ? 2 : 1);
        active.DisposeCount.ShouldBe(0);
        active.JoinedTenants.ShouldAllBe(value => value == tenant);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException("The streaming subscriber did not recover in time.");
            }

            await Task.Delay(5, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }
    }

    private sealed class QueueHubConnectionFactory(params FakeHubConnection[] connections)
        : IProjectConversationHubConnectionFactory
    {
        private readonly Queue<FakeHubConnection> _connections = new(connections);

        public int CreateCount { get; private set; }

        public Action? BeforeCreateReturns { get; set; }

        public IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider)
        {
            CreateCount++;
            BeforeCreateReturns?.Invoke();
            return _connections.Dequeue();
        }
    }

    private sealed class FakeHubConnection(IEnumerable<bool>? joinOutcomes = null) : IProjectConversationHubConnection
    {
        private readonly Queue<bool> _joinOutcomes = new(joinOutcomes ?? [true]);
        private Func<string, Task>? _changed;

        public Task? JoinDelay { get; set; }

        public Task? StartDelay { get; set; }

        public bool StartFails { get; set; }

        public bool NextJoinFails { get; set; }

        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;

        public int StartCount { get; private set; }

        public int DisposeCount { get; private set; }

        public List<string> JoinedTenants { get; } = [];

        public event Func<Task>? Closed;

        public event Func<Task>? Reconnected;

        public void RegisterChanged(string methodName, Func<string, Task> callback)
        {
            _changed = callback;
        }

        public async Task StartAsync()
        {
            StartCount++;
            if (StartDelay is { } delay)
            {
                await delay.ConfigureAwait(true);
            }

            if (StartFails)
            {
                throw new InvalidOperationException("simulated startup failure");
            }

            State = HubConnectionState.Connected;
        }

        public async Task InvokeAsync(string methodName, string tenantId)
        {
            if (JoinDelay is { } delay)
            {
                await delay.ConfigureAwait(true);
            }

            bool succeeds = _joinOutcomes.Count == 0 || _joinOutcomes.Dequeue();
            if (!succeeds || NextJoinFails)
            {
                throw new InvalidOperationException("simulated rejoin failure");
            }

            JoinedTenants.Add(tenantId);
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }

        public Task TriggerClosedAsync()
        {
            State = HubConnectionState.Disconnected;
            return Closed?.Invoke() ?? Task.CompletedTask;
        }

        public Task TriggerReconnectedAsync()
        {
            State = HubConnectionState.Connected;
            return Reconnected?.Invoke() ?? Task.CompletedTask;
        }

        public Task TriggerChangedAsync(string tenantId)
            => _changed?.Invoke(tenantId) ?? Task.CompletedTask;
    }
}
