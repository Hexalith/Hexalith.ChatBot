using System.Net;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Separates command transport observations from token acquisition and post-admission failures.</summary>
public sealed class ScopedOutageControlProbeTests
{
    /// <summary>Failures outside the command POST retain their original cause instead of becoming unobserved.</summary>
    [Theory]
    [InlineData("acquisition", false)]
    [InlineData("acquisition", true)]
    [InlineData("durable", false)]
    [InlineData("durable", true)]
    [InlineData("projection", false)]
    [InlineData("projection", true)]
    public async Task NonPostFailuresPropagate(string stage, bool timeout)
    {
        Exception failure = timeout ? new TaskCanceledException() : new HttpRequestException();
        RecoveryDurableStateMessageHandler durableHandler = new()
        {
            OnAggregateRead = (_, _) =>
            {
                if (stage == "durable")
                {
                    throw failure;
                }
            },
        };
        using SequenceHttpMessageHandler commandHandler = new((_, request) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                throw failure;
            }

            using JsonDocument body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            durableHandler.AddGovernedNote(
                RecoveryValidationTopology.ControlTenantRef,
                body.RootElement.GetProperty("command").GetProperty("noteId").GetString()!);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        using HttpClient client = new(commandHandler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(durableHandler);
        AspireScopedOutageOperations operations = Operations(client, durable, _ => stage == "acquisition"
            ? ValueTask.FromException<string>(failure)
            : ValueTask.FromResult("control"));

        Exception? thrown = await Record.ExceptionAsync(() => operations.IsChatBotControlAvailableAsync(
            ScopedOutageDependencies.Graph, TestContext.Current.CancellationToken));

        thrown.ShouldBeSameAs(failure);
        commandHandler.Requests.ShouldBe(stage == "acquisition" ? 0 : stage == "durable" ? 1 : 2);
    }

    /// <summary>Only unanswered command POSTs become metadata-only unobserved outcomes.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostFailuresAreUnobserved(bool timeout)
    {
        using SequenceHttpMessageHandler commandHandler = new((_, _) =>
            throw (timeout ? new TaskCanceledException() : new HttpRequestException()));
        using HttpClient client = new(commandHandler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(new RecoveryDurableStateMessageHandler());

        ScopedOutageControlAvailability result = await Operations(client, durable).IsChatBotControlAvailableAsync(
            ScopedOutageDependencies.Graph, TestContext.Current.CancellationToken);

        result.ShouldBe(new ScopedOutageControlAvailability(false, true, timeout ? "client-timeout" : "transport-Unknown"));
        commandHandler.Requests.ShouldBe(1);
    }

    /// <summary>A real refusal remains observed; accepted commands must pass durable and projection checks.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResponsesPreserveControlOutcome(bool accepted)
    {
        RecoveryDurableStateMessageHandler durableHandler = new();
        using SequenceHttpMessageHandler commandHandler = new((_, request) =>
        {
            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            using JsonDocument body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            durableHandler.AddGovernedNote(
                RecoveryValidationTopology.ControlTenantRef,
                body.RootElement.GetProperty("command").GetProperty("noteId").GetString()!);
            return new HttpResponseMessage(accepted ? HttpStatusCode.Accepted : HttpStatusCode.ServiceUnavailable);
        });
        using HttpClient client = new(commandHandler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(durableHandler);

        ScopedOutageControlAvailability result = await Operations(client, durable).IsChatBotControlAvailableAsync(
            ScopedOutageDependencies.Graph, TestContext.Current.CancellationToken);

        result.ShouldBe(new ScopedOutageControlAvailability(accepted, false, accepted ? null : "status-503"));
        commandHandler.Requests.ShouldBe(accepted ? 2 : 1);
        durableHandler.Requests.Count.ShouldBe(accepted ? 2 : 0);
    }

    /// <summary>A caller-canceled POST propagates cancellation without fabricating a timeout observation.</summary>
    [Fact]
    public async Task CallerCancellationPropagates()
    {
        using CancellationTokenSource caller = new();
        using SequenceHttpMessageHandler commandHandler = new((_, _) =>
        {
            caller.Cancel();
            throw new TaskCanceledException("caller canceled", null, caller.Token);
        });
        using HttpClient client = new(commandHandler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(new RecoveryDurableStateMessageHandler());

        await Should.ThrowAsync<OperationCanceledException>(() => Operations(client, durable).IsChatBotControlAvailableAsync(
            ScopedOutageDependencies.Graph, caller.Token));
        commandHandler.Requests.ShouldBe(1);
    }

    /// <summary>A received refusal is observed even when its unused response body cannot be downloaded.</summary>
    [Fact]
    public async Task ReceivedStatusDoesNotRequireResponseBodyBuffering()
    {
        using FailingRecoveryHttpContent content = new();
        using SequenceHttpMessageHandler handler = new((_, _) => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = content,
        });
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(new RecoveryDurableStateMessageHandler());

        ScopedOutageControlAvailability result = await Operations(client, durable).IsChatBotControlAvailableAsync(
            ScopedOutageDependencies.Graph, TestContext.Current.CancellationToken);

        result.ShouldBe(new ScopedOutageControlAvailability(false, false, "status-503"));
        content.Reads.ShouldBe(0);
    }

    /// <summary>The Identity outage uses the checkpoint bearer without contacting the faulted token endpoint.</summary>
    [Fact]
    public async Task IdentityUsesRetainedBearerWithoutAcquisition()
    {
        using SequenceHttpMessageHandler handler = new((_, request) =>
        {
            request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
            request.Headers.Authorization.Parameter.ShouldBe("control");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });
        using HttpClient client = new(handler) { BaseAddress = new Uri("http://chatbot.test") };
        using EventStoreDurableStateProbe durable = Durable(new RecoveryDurableStateMessageHandler());

        ScopedOutageControlAvailability result = await Operations(client, durable, _ => throw new InvalidOperationException(
            "Identity outage must not acquire a new token.")).IsChatBotControlAvailableAsync(
                ScopedOutageDependencies.Identity, TestContext.Current.CancellationToken);

        result.ShouldBe(new ScopedOutageControlAvailability(false, false, "status-503"));
        handler.Requests.ShouldBe(1);
    }

    private static EventStoreDurableStateProbe Durable(RecoveryDurableStateMessageHandler handler)
        => new(new Uri("http://eventstore.test"), handler, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1));

    private static AspireScopedOutageOperations Operations(
        HttpClient client,
        EventStoreDurableStateProbe durable,
        Func<CancellationToken, ValueTask<string>>? acquire = null)
    {
        InMemoryRecoveryReadModelStore store = new();
        return new AspireScopedOutageOperations(
            new ScopedOutageOperationsTestSeam
            {
                ChatBotClient = client,
                AcquireControlAccessTokenAsync = acquire ?? (_ => ValueTask.FromResult("control")),
            },
            "control", store, store, durable, TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(2));
    }
}
