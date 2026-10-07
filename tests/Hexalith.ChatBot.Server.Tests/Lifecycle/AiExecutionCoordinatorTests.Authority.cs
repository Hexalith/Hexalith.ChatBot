using System.Net;
using System.Net.Http.Json;

using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

/// <summary>Verifies retained authority at real durable AI recovery boundaries.</summary>
public sealed partial class AiExecutionCoordinatorTests
{
    /// <summary>Expiry at either read or a failed CAS cannot reset persisted exhausted work.</summary>
    [Theory]
    [InlineData("preliminary-read")]
    [InlineData("cas-read")]
    [InlineData("cas-retry")]
    [InlineData("completed-write")]
    public async Task RecoveryRetainsAuthorityAcrossRealDurableBoundaries(string boundary)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore store = new(durable);
        AiExecutionWorkItem original = Item(Started(1)) with { Status = AiExecutionWorkStatus.Exhausted, AttemptCount = 5, TerminalSubmissionAttemptCount = 3, FailureReason = "attempts-exhausted" };
        await store.UpsertStartedAsync(original, TestContext.Current.CancellationToken);
        durable.SaveAttempts.Clear();
        int reads = 0;
        durable.AfterRead = key =>
        {
            if (key == original.Key && ++reads == (boundary == "preliminary-read" ? 1 : boundary == "cas-read" ? 2 : -1)) { clock.UtcNow += TimeSpan.FromSeconds(2); }
        };
        durable.RejectSave = key =>
        {
            if (key != original.Key || boundary != "cas-retry") { return false; }
            clock.UtcNow += TimeSpan.FromSeconds(2);
            return true;
        };
        durable.AfterSave = key => { if (key == original.Key && boundary == "completed-write") { clock.UtcNow += TimeSpan.FromSeconds(2); } };
        using WebApplicationFactory<Program> factory = RecoveryFactory(clock, owner, store);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/operations/ai-executions/exhausted/recover", new { Key = original.Key }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(boundary == "completed-write" ? HttpStatusCode.OK : HttpStatusCode.NotFound);
        durable.AfterRead = null;
        AiExecutionWorkItem persisted = (await durable.GetAsync<AiExecutionWorkItem>("chatbot-state", original.Key, TestContext.Current.CancellationToken)).Value!;
        if (boundary == "completed-write")
        {
            persisted.Status.ShouldBe(AiExecutionWorkStatus.Pending);
            persisted.AttemptCount.ShouldBe(0);
            persisted.TerminalSubmissionAttemptCount.ShouldBe(0);
            persisted.FailureReason.ShouldBeNull();
            durable.SaveAttempts.ShouldBe([original.Key]);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain("recovered");
        }
        else
        {
            persisted.ShouldBe(original);
            durable.SaveAttempts.Count.ShouldBe(boundary == "cas-retry" ? 1 : 0);
            (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(original.Key);
        }
    }

    /// <summary>Listing expiry stops subsequent persisted reads and prevents row disclosure.</summary>
    [Theory]
    [InlineData("index")]
    [InlineData("item")]
    public async Task ExhaustedListingRetainsAuthorityUntilDisclosure(string boundary)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        DurableReadModelStore durable = new();
        ReadModelAiExecutionWorkStore store = new(durable);
        AiExecutionWorkItem original = Item(Started(1)) with { Status = AiExecutionWorkStatus.Exhausted };
        AiExecutionWorkItem second = Item(Started(2)) with { Status = AiExecutionWorkStatus.Exhausted };
        await store.UpsertStartedAsync(original, TestContext.Current.CancellationToken);
        await store.UpsertStartedAsync(second, TestContext.Current.CancellationToken);
        durable.ReadKeys.Clear();
        durable.AfterRead = key =>
        {
            if (boundary == "index" || key == original.Key || key == second.Key) { clock.UtcNow += TimeSpan.FromSeconds(2); }
        };
        using WebApplicationFactory<Program> factory = RecoveryFactory(clock, owner, store);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/v1/operations/ai-executions/exhausted", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        durable.ReadKeys.Count(key => key == original.Key || key == second.Key).ShouldBe(boundary == "index" ? 0 : 1);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldNotContain(original.Key);
        durable.AfterRead = null;
        (await durable.GetAsync<AiExecutionWorkItem>("chatbot-state", original.Key, TestContext.Current.CancellationToken)).Value.ShouldBe(original);
        (await durable.GetAsync<AiExecutionWorkItem>("chatbot-state", second.Key, TestContext.Current.CancellationToken)).Value.ShouldBe(second);
    }

    private static WebApplicationFactory<Program> RecoveryFactory(TrustedAuthorityClock clock, SyntheticOwnerAuthorityProvider owner, IAiExecutionWorkStore store)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            // Isolate the operator request from the provider worker's independent durable scans.
            services.RemoveAll<IHostedService>();
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(_ => TrustedAuthorityFixture.Principal()));
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IAiExecutionWorkStore>(store);
        }));
}
