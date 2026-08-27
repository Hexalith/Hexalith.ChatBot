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
        await using var cleanup = subscriber.ConfigureAwait(false);
        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => { }, () => recoveryNotifications++);

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
        await using var cleanup = subscriber.ConfigureAwait(false);
        await subscriber.EnsureSubscribedAsync("tenant-alpha", () => { }, () => recoveryNotifications++);

        await failedRejoin.TriggerReconnectedAsync();
        await WaitUntilAsync(() => recovered.StartCount == 1 && recoveryNotifications == 1);

        failedRejoin.DisposeCount.ShouldBe(1);
        factory.CreateCount.ShouldBe(2);
        recovered.JoinedTenants.ShouldBe(["tenant-alpha"]);
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

        public IProjectConversationHubConnection Create(Uri hubUri, Func<Task<string?>>? accessTokenProvider)
        {
            CreateCount++;
            return _connections.Dequeue();
        }
    }

    private sealed class FakeHubConnection(IEnumerable<bool>? joinOutcomes = null) : IProjectConversationHubConnection
    {
        private readonly Queue<bool> _joinOutcomes = new(joinOutcomes ?? [true]);

        public HubConnectionState State { get; private set; } = HubConnectionState.Disconnected;

        public int StartCount { get; private set; }

        public int DisposeCount { get; private set; }

        public List<string> JoinedTenants { get; } = [];

        public event Func<Task>? Closed;

        public event Func<Task>? Reconnected;

        public void RegisterChanged(string methodName, Action<string> callback)
        {
        }

        public Task StartAsync()
        {
            StartCount++;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task InvokeAsync(string methodName, string tenantId)
        {
            bool succeeds = _joinOutcomes.Count == 0 || _joinOutcomes.Dequeue();
            if (!succeeds)
            {
                throw new InvalidOperationException("simulated rejoin failure");
            }

            JoinedTenants.Add(tenantId);
            return Task.CompletedTask;
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
    }
}
