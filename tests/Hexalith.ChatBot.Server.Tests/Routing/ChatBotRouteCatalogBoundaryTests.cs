using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Routing;
using Hexalith.ChatBot.Server.Tests.TrustedAuthority;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Routing;

/// <summary>Exercises the SDK guards without replacing authority, target resolution, or key availability checks.</summary>
public sealed class ChatBotRouteCatalogBoundaryTests
{
    private const string NoteId = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    /// <summary>Pairs each real receiver with partial, stale, or conflicting caller generation authority.</summary>
    public static TheoryData<string, string> AutomaticAuthorityCases
    {
        get
        {
            TheoryData<string, string> cases = [];
            foreach (string route in new[] { "process", "query", "project" })
            {
                foreach (string failure in new[] { "root-only", "epoch-only", "stale-root", "stale-epoch", "conflicting-root", "conflicting-epoch" })
                {
                    cases.Add(route, failure);
                }
            }
            return cases;
        }
    }

    /// <summary>A usable principal first reaches the real domain boundary, then changing only catalog authority stops it.</summary>
    [Theory]
    [MemberData(nameof(AutomaticAuthorityCases))]
    public async Task AutomaticActivationMustPreserveCallerAuthorityAcrossOwnedReceivers(string route, string failure)
    {
        SystemClock clock = new();
        InMemoryCoarseIdempotencyStore reservations = new(clock);
        InMemoryAuditWriter audit = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static _ => false };
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers(services);
                services.AddSingleton<IIdempotencyStore>(reservations);
                services.AddSingleton<IAuditWriter>(audit);
                services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
                services.AddFixturePrincipalAuthentication().AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(
                    _ => TrustedAuthorityFixture.Principal()));
            }));
        using HttpClient client = factory.CreateClient();
        HttpRouteCatalogFixture fixture = factory.Services.GetRequiredService<HttpRouteCatalogFixture>();
        fixture.AutomaticActivation.ShouldBeTrue();

        using HttpRequestMessage control = Request(route);
        using HttpResponseMessage accepted = await client.SendAsync(control, TestContext.Current.CancellationToken);
        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        switch (route)
        {
            case "process":
                DomainServiceWireResult refusal = (await accepted.Content.ReadFromJsonAsync<DomainServiceWireResult>(
                    cancellationToken: TestContext.Current.CancellationToken)).ShouldNotBeNull();
                refusal.IsRejection.ShouldBeTrue();
                DomainServiceWireEvent rejected = refusal.Events.ShouldHaveSingleItem();
                rejected.EventTypeName.ShouldBe(typeof(ChatBotDomainServiceAdmissionRejected).FullName);
                JsonSerializer.Deserialize<ChatBotDomainServiceAdmissionRejected>(rejected.Payload)!.ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
                audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotRefusalReasonCodes.PolicySnapshotUnavailable);
                break;
            case "query":
                QueryResult query = (await accepted.Content.ReadFromJsonAsync<QueryResult>(
                    cancellationToken: TestContext.Current.CancellationToken)).ShouldNotBeNull();
                query.Success.ShouldBeFalse();
                query.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
                owner.Requests.ShouldNotBeEmpty();
                owner.Requests.ShouldAllBe(request => request.PrincipalId == "actor-alpha" && request.TenantId == "tenant-alpha");
                break;
            case "project":
                ProjectionResponse projection = (await accepted.Content.ReadFromJsonAsync<ProjectionResponse>(
                    cancellationToken: TestContext.Current.CancellationToken)).ShouldNotBeNull();
                projection.ProjectionType.ShouldBe("chatbot");
                projection.State.GetProperty("appliedEventCount").GetInt32().ShouldBe(0);
                projection.State.GetProperty("ignoredEventCount").GetInt32().ShouldBe(1);
                break;
        }

        RouteCatalogGenerationReadback generation = await fixture.ActivateAsync(factory.Services, TestContext.Current.CancellationToken);
        int auditCount = audit.AuthorizationFailures.Count;
        int ownerCount = owner.Requests.Count;
        using HttpRequestMessage invalid = Request(route);
        if (failure != "epoch-only")
        {
            invalid.Headers.Add(RouteCatalogInvocationHandler.RootHeader, failure == "conflicting-root"
                ? [generation.RootDigest, new string('0', 64)]
                : [failure == "stale-root" ? new string('0', 64) : generation.RootDigest]);
        }
        if (failure != "root-only")
        {
            string epoch = generation.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture);
            invalid.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, failure == "conflicting-epoch"
                ? [epoch, epoch] : [(generation.Epoch + (failure == "stale-epoch" ? 1 : 0)).ToString(System.Globalization.CultureInfo.InvariantCulture)]);
        }

        using HttpResponseMessage refused = await client.SendAsync(invalid, TestContext.Current.CancellationToken);
        refused.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        audit.AuthorizationFailures.Count.ShouldBe(auditCount);
        owner.Requests.Count.ShouldBe(ownerCount);
        audit.Envelopes.ShouldBeEmpty();
        reservations.RecordCount.ShouldBe(0);
        (await factory.Services.GetRequiredService<IGovernedOperationProjectionStore>()
            .GetAsync("tenant-alpha", NoteId, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await factory.Services.GetRequiredService<IRouteCatalogStateStore>().ReadAsync(TestContext.Current.CancellationToken)).State.Leases.ShouldBeEmpty();
    }

    /// <summary>Absent or stale generation authority cannot reach ChatBot or reserve an operation.</summary>
    [Theory]
    [InlineData("missing-commit")]
    [InlineData("missing-headers")]
    [InlineData("stale-root")]
    [InlineData("stale-epoch")]
    public async Task InvalidCatalogAuthorityMustRefuseBeforeDomainEffects(string failure)
    {
        InMemoryCoarseIdempotencyStore reservations = new(new SystemClock());
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers(services);
                services.AddSingleton<IIdempotencyStore>(reservations);
            }));
        using HttpClient client = factory.CreateClient();
        HttpRouteCatalogFixture fixture = factory.Services.GetRequiredService<HttpRouteCatalogFixture>();
        fixture.AutomaticActivation = false;
        RouteCatalogGenerationReadback? generation = failure == "missing-commit" ? null
            : await fixture.ActivateAsync(factory.Services, TestContext.Current.CancellationToken);
        CommandEnvelope command = new("01ARZ3NDEKTSV4RRFFQ69G5FAY", "tenant-alpha", "chatbot", "note-001", nameof(RecordGovernedNote),
            JsonSerializer.SerializeToUtf8Bytes(new RecordGovernedNote("note-001")), "correlation-001", null, "actor-alpha", null);
        using HttpRequestMessage request = new(HttpMethod.Post, "/process") { Content = JsonContent.Create(new DomainServiceRequest(command, null)) };
        if (generation is not null && failure != "missing-headers")
        {
            request.Headers.Add(RouteCatalogInvocationHandler.RootHeader, failure == "stale-root" ? new string('0', 64) : generation.RootDigest);
            request.Headers.Add(RouteCatalogInvocationHandler.EpochHeader, (generation.Epoch + (failure == "stale-epoch" ? 1 : 0)).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        reservations.RecordCount.ShouldBe(0);
        (await factory.Services.GetRequiredService<IGovernedOperationProjectionStore>()
            .GetAsync("tenant-alpha", "note-001", TestContext.Current.CancellationToken)).ShouldBeNull();
        (await factory.Services.GetRequiredService<IRouteCatalogStateStore>().ReadAsync(TestContext.Current.CancellationToken)).State.Leases.ShouldBeEmpty();
    }

    /// <summary>Consumer facts are unavailable if the installed SDK provider has no required key or disagrees with the options.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrMismatchedDigestKeysCannotProduceAdmissionBindings(bool mismatched)
    {
        ServiceCollection services = new();
        foreach (IDeclaredIdempotencyIntentAdapter adapter in ChatBotCatalogAdapters.Create()) { services.AddSingleton<IIdempotencyIntentAdapter>(adapter); }
        IdempotencyAdmissionOptions configured = new()
        {
            Enabled = true, ActiveDigestKeyVersion = "configured-v1", DigestKeySource = IdempotencyDigestKeySource.Configuration,
        };
        IdempotencyAdmissionOptions available = configured with
        {
            ActiveDigestKeyVersion = mismatched ? "available-v2" : "configured-v1",
            DigestKeys = mismatched ? new() { ["available-v2"] = Convert.ToBase64String(new byte[32]) } : new(),
        };
        services.AddSingleton<IOptions<IdempotencyAdmissionOptions>>(Options.Create(configured));
        services.AddSingleton<IIdempotencyDigestKeyProvider>(new ConfigurationIdempotencyDigestKeyProvider(Options.Create(available)));
        using ServiceProvider consumer = services.BuildServiceProvider();

        if (mismatched)
        {
            await Should.ThrowAsync<RouteCatalogUnavailableException>(() => RouteCatalogAdmissionObservation.CaptureAsync(consumer, "fixture-consumer", TestContext.Current.CancellationToken));
        }
        else
        {
            await Should.ThrowAsync<InvalidOperationException>(() => RouteCatalogAdmissionObservation.CaptureAsync(consumer, "fixture-consumer", TestContext.Current.CancellationToken));
        }
    }

    /// <summary>A proposed target cannot replace the independently observed owner even when all declaration digests are retained.</summary>
    [Fact]
    public async Task ActualOwnerObservationMustRejectMismatchedTarget()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(SdkRouteFixtureAuthorization.AllowCanonicalRouteHandlers));
        using HttpClient client = factory.CreateClient();
        HttpRouteCatalogFixture fixture = factory.Services.GetRequiredService<HttpRouteCatalogFixture>();
        fixture.AutomaticActivation = false;
        await fixture.ActivateAsync(factory.Services, TestContext.Current.CancellationToken);
        IRouteCatalogStateStore store = factory.Services.GetRequiredService<IRouteCatalogStateStore>();
        RouteCatalogActivation activation = (await store.ReadAsync(TestContext.Current.CancellationToken)).State.Committed.ShouldNotBeNull();
        RouteCatalogEnvelope original = RouteCatalogCodec.Decode(activation.CanonicalCatalog, activation.RootDigest);
        RouteCatalogEnvelope changed = RouteCatalogCodec.Seal(original with
        {
            Routes = original.Routes.Select((route, index) => index == 0 ? route with { AppId = "wrong-owner" } : route).ToArray(),
        });
        RouteCatalogActivation proposal = activation with
        {
            CanonicalCatalog = RouteCatalogCodec.Encode(changed), RootDigest = RouteCatalogCodec.ComputeRootDigest(changed),
        };
        await Should.ThrowAsync<RouteCatalogUnavailableException>(() => factory.Services.GetRequiredService<IRouteCatalogHostObserver>()
            .ObserveAsync(proposal, store.Coordinates!, TestContext.Current.CancellationToken));
        (await store.ReadAsync(TestContext.Current.CancellationToken)).State.Committed!.RootDigest.ShouldBe(activation.RootDigest);
    }

    private static HttpRequestMessage Request(string route)
    {
        object payload = route switch
        {
            "process" => new DomainServiceRequest(new CommandEnvelope(NoteId, "tenant-alpha", "chatbot", "policy-change-001",
                nameof(SubmitEscalationPolicyChange), JsonSerializer.SerializeToUtf8Bytes(new SubmitEscalationPolicyChange("policy-change-001", "source", "proposed", 7,
                    new([]), "test", "requester", "v1", "correlation-001", "old", "new")), "correlation-001", null, "actor-alpha", null), null),
            "query" => new QueryEnvelope("tenant-alpha", "chatbot", NoteId, ChatBotReadQueryTypes.GovernedOperation,
                JsonSerializer.SerializeToUtf8Bytes(new GovernedOperationQuery(NoteId, null)), "correlation-001", "actor-alpha"),
            "project" => new ProjectionRequest("tenant-alpha", "chatbot", NoteId,
                [new ProjectionEventDto("fixture.unsupported", "{}"u8.ToArray(), "json", 1, DateTimeOffset.UnixEpoch, "correlation-001", NoteId)]),
            _ => throw new ArgumentOutOfRangeException(nameof(route)),
        };
        return new(HttpMethod.Post, "/" + route) { Content = JsonContent.Create(payload, payload.GetType()) };
    }
}
