using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Tests.TrustedAuthority;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.ChatBot.Conformance.Tests;

public sealed class TrustedAuthorityParityTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAY";
    private const string Forbidden = "01ARZ3NDEKTSV4RRFFQ69G5FAZ";
    private const string Missing = "01ARZ3NDEKTSV4RRFFQ69G5FAX";
    private const string OwnerPii = "owner-email@example.test";
    private const string OwnerError = "restricted-owner-error@example.test";
    private const string ForbiddenContent = "restricted-forbidden-record-content";
    private const string ForeignContent = "restricted-foreign-record-content";

    public static TheoryData<string, string, string> RealActors => new()
    {
        { "ui", "human", "tenant-alpha" }, { "api", "human", "tenant-alpha" },
        { "cli", "service", "tenant-alpha" }, { "mcp", "service", "tenant-alpha" },
        { "worker", "service", "tenant-alpha" }, { "mailbox", "service", "tenant-alpha" },
        { "api", "service", "tenant-alpha" }, { "ai", "ai", "tenant-alpha" },
        { "ui", "human", "tenant-beta" }, { "api", "human", "tenant-beta" },
        { "cli", "service", "tenant-beta" }, { "mcp", "service", "tenant-beta" },
        { "worker", "service", "tenant-beta" }, { "mailbox", "service", "tenant-beta" },
        { "api", "service", "tenant-beta" }, { "ai", "ai", "tenant-beta" },
    };

    [Theory]
    [MemberData(nameof(RealActors))]
    public async Task RealHttpActorsShareExactScopeExistenceNeutralityAndMetadataOnlyOutputs(string origin, string actorClass, string tenant)
    {
        TrustedAuthorityClock clock = new();
        bool forbiddenEvidenceObserved = false;
        bool ownerErrorObserved = false;
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence =>
            {
                if (evidence.Request.ResourceId == Forbidden)
                {
                    forbiddenEvidenceObserved = true;
                    return evidence with { EvidenceId = OwnerPii };
                }

                if (evidence.Request.ResourceId == Missing)
                {
                    ownerErrorObserved = true;
                    throw new InvalidOperationException(OwnerError);
                }

                return evidence;
            },
            Allows = request => request.TenantId == tenant && request.PrincipalId == "actor-alpha" &&
                (request.Authority is "identity" or "service-grant" || request.ResourceId == Note),
        };
        CountingGovernedOperationStore store = new()
        {
            View = new(tenant, Note, GovernedOperationView.CurrentSchemaVersion, "synthetic", "v1", "metadata_only", "test", 1, clock.UtcNow, clock.UtcNow),
        };
        string foreignTenant = tenant == "tenant-alpha" ? "tenant-beta" : "tenant-alpha";
        store.AdditionalViews.Add(store.View! with { NoteId = Forbidden, SourceProvenance = ForbiddenContent, RetentionClass = OwnerPii });
        store.AdditionalViews.Add(store.View! with { TenantId = foreignTenant, SourceProvenance = ForeignContent, RetentionClass = OwnerPii });
        store.AdditionalViews.Add(store.View! with { TenantId = foreignTenant, NoteId = Forbidden, SourceProvenance = ForeignContent });
        store.AdditionalViews.ShouldContain(view => view.TenantId == tenant && view.NoteId == Forbidden);
        store.AdditionalViews.ShouldContain(view => view.TenantId == foreignTenant && view.NoteId == Note);
        store.AdditionalViews.ShouldNotContain(view => view.TenantId == tenant && view.NoteId == Missing);
        using TrustedAuthorityOutputCapture capture = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .ConfigureLogging(logging => logging.AddProvider(capture).SetMinimumLevel(LogLevel.Information))
            .ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityHttpStartupFilter(tenant, actorClass));
            services.AddSingleton<Hexalith.ChatBot.Server.Audit.ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IGovernedOperationProjectionStore>(store);
        }));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Hexalith-Surface-Origin", origin);
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        using HttpResponseMessage allowed = await client.GetAsync($"/api/v1/governed-operations/{Note}", TestContext.Current.CancellationToken);
        allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        store.Reads.ShouldBe(1);
        using HttpResponseMessage forbidden = await client.GetAsync($"/api/v1/governed-operations/{Forbidden}", TestContext.Current.CancellationToken);
        using HttpResponseMessage missing = await client.GetAsync($"/api/v1/governed-operations/{Missing}", TestContext.Current.CancellationToken);
        forbidden.StatusCode.ShouldBe(missing.StatusCode);
        string forbiddenBody = await forbidden.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        string missingBody = await missing.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        forbiddenBody.ShouldBe(missingBody);
        store.Reads.ShouldBe(1);
        using HttpResponseMessage commandDenied = await client.PostAsJsonAsync("/api/v1/commands",
            new { commandId = Note, commandType = nameof(RecordGovernedNote), command = new { noteId = Forbidden }, requestSchemaVersion = "v1" }, TestContext.Current.CancellationToken);
        commandDenied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Services.GetRequiredService<InMemoryAuditWriter>().AuthorizationFailures.ShouldNotBeEmpty();
        forbiddenEvidenceObserved.ShouldBeTrue();
        ownerErrorObserved.ShouldBeTrue();
        store.Reads.ShouldBe(1);
        store.Writes.ShouldBe(0);
        capture.Logs.ShouldNotBeEmpty();
        capture.Traces.ShouldNotBeEmpty();
        owner.Requests.ShouldAllBe(request => request.PrincipalId == "actor-alpha" && request.TenantId == tenant && request.ActorClass == actorClass && ChatBotSurfaceOrigins.ToWireValue(request.Origin) == origin);
        string[] outputs =
        [
            forbiddenBody, missingBody,
            await commandDenied.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            await allowed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
            JsonSerializer.Serialize(factory.Services.GetRequiredService<InMemoryAuditWriter>().AuthorizationFailures),
            JsonSerializer.Serialize(factory.Services.GetRequiredService<InMemoryAuditWriter>().Envelopes),
            JsonSerializer.Serialize(factory.Services.GetRequiredService<InMemoryUserFacingMessageTelemetry>().Counts.Select(static entry => new { entry.Key.CatalogVersion, entry.Key.FallbackCode, entry.Value })),
        ];
        foreach (string output in outputs)
        {
            output.ShouldNotContain(OwnerPii);
            output.ShouldNotContain("synthetic-evidence");
            output.ShouldNotContain(Forbidden);
            output.ShouldNotContain(Missing);
            output.ShouldNotContain(tenant == "tenant-alpha" ? "tenant-beta" : "tenant-alpha");
        }

        // Framework request paths contain caller-supplied IDs; restricted owner/record content must never reach any output.
        foreach (string output in outputs.Concat(capture.Logs).Concat(capture.Traces))
        {
            output.ShouldNotContain(OwnerPii);
            output.ShouldNotContain(OwnerError);
            output.ShouldNotContain(ForbiddenContent);
            output.ShouldNotContain(ForeignContent);
        }
    }

    [Theory]
    [MemberData(nameof(RealActors))]
    public async Task ProvenanceNeverChangesActorClassOrHumanAdminEligibility(string origin, string actorClass, string tenant)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        var context = TrustedAuthorityFixture.Context(tenant, actorClass, ChatBotSurfaceOrigins.FromWireValueOrDefault(origin));
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(context,
            nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "change-alpha" }, TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBe(actorClass == "human");
        context.ActorClass.ShouldBe(actorClass);
        ChatBotSurfaceOrigins.ToWireValue(context.Origin).ShouldBe(origin);
    }
}
