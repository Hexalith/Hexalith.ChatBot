using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

#pragma warning disable CA2007 // xUnit test methods keep awaits on the xUnit synchronization context.

/// <summary>
/// Story 9.3 (AC1/AC2/AC3): end-to-end coverage of the tenant-scoped, Compliance-gated audit investigation read
/// endpoints — enumerate the WORM chain → ComplianceAuditReadPolicy → metadata-only contracts. Authority denial,
/// unresolved/cross-tenant lookup, and unknown records all collapse to the identical safe-not-found, replay records
/// are excluded from default search, and the read performs no audit-chain append.
/// </summary>
public sealed class ComplianceAuditInvestigationEndpointTests
{
    private const string SearchRoute = "/api/v1/compliance/audit/search";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    private static readonly string SearchBody =
        """
        {
          "queryRef": "audit-query-001",
          "filters": [{ "filterRef": "audit-filter-001", "filterKey": "actor", "valueRef": "actor-alpha" }],
          "fromUtc": "2026-06-01T00:00:00+00:00",
          "toUtc": "2026-06-03T00:00:00+00:00",
          "limit": 100
        }
        """;

    [Fact]
    public async Task SearchShouldReturnTenantRowsExcludingReplayAndForeignTenantWithoutMutatingTheChain()
    {
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        IWormAuditStore store = factory.Services.GetRequiredService<IWormAuditStore>();
        await SeedAsync(store, Envelope("tenant-alpha", "audit-record-001"));
        await SeedAsync(store, Envelope("tenant-alpha", "audit-record-replay") with { ReplayRunId = "replay-run-001" });
        await SeedAsync(store, Envelope("tenant-beta", "audit-record-beta"));

        int chainLengthBefore = store.EnumerateChain("tenant-alpha").Count;

        using HttpResponseMessage response = await client.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        string[] refs = AuditRecordRefs(body);
        refs.ShouldContain("audit-record-001");
        refs.ShouldNotContain("audit-record-replay");
        refs.ShouldNotContain("audit-record-beta");

        // The read path appends nothing to the WORM chain (D4 two-phase audit / NFR49a).
        store.EnumerateChain("tenant-alpha").Count.ShouldBe(chainLengthBefore);

        // Metadata-only floor (NFR2): no raw content tokens in the rendered payload.
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
        body.ShouldNotContain("tenant-beta", Case.Insensitive);
        body.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public async Task SearchShouldPreserveQueryRefAndCorrelationThroughTheMigratedQueryAdapter()
    {
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-001"));

        using HttpResponseMessage response = await client.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        document.RootElement.GetProperty("queryRef").GetString().ShouldBe("audit-query-001");
        document.RootElement.GetProperty("correlationId").GetString().ShouldBe(CorrelationId);
        JsonElement row = document.RootElement.GetProperty("rows").EnumerateArray().Single();
        row.GetProperty("auditRecordRef").GetString().ShouldBe("audit-record-001");
        row.GetProperty("correlationId").GetString().ShouldBe(CorrelationId);
        row.GetProperty("redactionState").GetString().ShouldBe("restricted");
    }

    [Fact]
    public async Task SearchShouldAllowHumanTenantAdminAndDenyAiActorBeforeReturningRows()
    {
        using WebApplicationFactory<Program> tenantAdmin = ComplianceFactory("tenant-alpha", role: "tenant-admin");
        using WebApplicationFactory<Program> aiActor = ComplianceFactory("tenant-alpha", role: "tenant-admin", actorType: ParticipantAuthorizationStage.AiActorValue);
        using HttpClient tenantAdminClient = tenantAdmin.CreateClient();
        using HttpClient aiClient = aiActor.CreateClient();
        await SeedAsync(tenantAdmin.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-tenant-admin"));
        await SeedAsync(aiActor.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-ai-denied"));

        using HttpResponseMessage tenantAdminResponse = await tenantAdminClient.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);
        using HttpResponseMessage aiResponse = await aiClient.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        tenantAdminResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        AuditRecordRefs(await tenantAdminResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("audit-record-tenant-admin");

        aiResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string aiBody = await aiResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        aiBody.ShouldNotContain("audit-record-ai-denied", Case.Insensitive);
        aiBody.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Theory]
    [InlineData("mailbox-admin")]
    [InlineData("operations-admin")]
    [InlineData("policy-admin")]
    public async Task SearchWithoutComplianceScopeShouldDenyFinerAdminRolesBeforeReturningRows(string role)
    {
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha", role: role);
        using HttpClient client = factory.CreateClient();
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", $"audit-record-{role}"));

        using HttpResponseMessage response = await client.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain($"audit-record-{role}", Case.Insensitive);
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task SearchShouldHonorTheNewMessageIdAndSurfaceFiltersThroughTheEndpointRoundTrip()
    {
        // AC1/FR56: the two new filter dimensions must survive the full HTTP path (JSON deserialize → schema validate →
        // enumerate chain → ComplianceAuditReadPolicy.Search → wire model), not only the read-policy unit path.
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        IWormAuditStore store = factory.Services.GetRequiredService<IWormAuditStore>();
        await SeedAsync(store, Envelope("tenant-alpha", "audit-record-ui") with
        {
            SurfaceOrigin = "ui",
            SourceEvidenceRefs = ["project:redacted-ref", "source-message:intake-ui"],
        });
        await SeedAsync(store, Envelope("tenant-alpha", "audit-record-cli") with
        {
            SurfaceOrigin = "cli",
            SourceEvidenceRefs = ["project:redacted-ref", "provider-message:graph-cli"],
        });

        using HttpResponseMessage bySurface = await client.SendAsync(
            SearchRequest("tenant-alpha", FilterBody("audit-filter-surface", "surface", "cli")), TestContext.Current.CancellationToken);
        using HttpResponseMessage byMessageId = await client.SendAsync(
            SearchRequest("tenant-alpha", FilterBody("audit-filter-message", "message-id", "intake-ui")), TestContext.Current.CancellationToken);

        bySurface.StatusCode.ShouldBe(HttpStatusCode.OK);
        byMessageId.StatusCode.ShouldBe(HttpStatusCode.OK);
        string[] surfaceRefs = AuditRecordRefs(await bySurface.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        string[] messageRefs = AuditRecordRefs(await byMessageId.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        surfaceRefs.ShouldBe(["audit-record-cli"]);
        messageRefs.ShouldBe(["audit-record-ui"]);
    }

    [Fact]
    public async Task SearchWithUnknownFilterKeyShouldCollapseToSafeNotFound()
    {
        // An unknown filter key fails the schema gate and must collapse to the identical safe-not-found, never a leak.
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha");
        using HttpClient client = factory.CreateClient();
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-001"));

        using HttpResponseMessage response = await client.SendAsync(
            SearchRequest("tenant-alpha", FilterBody("audit-filter-001", "raw-sql", "select-star")), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("audit-record-001", Case.Insensitive);
    }

    [Fact]
    public async Task SearchWithoutHumanComplianceScopeShouldCollapseToSafeNotFound()
    {
        using WebApplicationFactory<Program> nonAdmin = ComplianceFactory("tenant-alpha", role: "project-contributor");
        using WebApplicationFactory<Program> nonHuman = ComplianceFactory("tenant-alpha", actorType: "service");
        using HttpClient nonAdminClient = nonAdmin.CreateClient();
        using HttpClient nonHumanClient = nonHuman.CreateClient();

        using HttpResponseMessage nonAdminResponse = await nonAdminClient.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);
        using HttpResponseMessage nonHumanResponse = await nonHumanClient.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        nonAdminResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        nonHumanResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        string body = await nonAdminResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using JsonDocument problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("code").GetString().ShouldBe("authorization_denied");
        body.ShouldNotContain("tenant-alpha", Case.Insensitive);
    }

    [Fact]
    public async Task SearchWithUnresolvedTenantShouldCollapseToSafeNotFound()
    {
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha", includeTenant: false);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchShouldRequireAuthentication()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.SendAsync(SearchRequest("tenant-alpha"), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DetailShouldDrivVisibilityFromPerProjectAuthorityAndCollapseUnknownRecordsToSafeNotFound()
    {
        using WebApplicationFactory<Program> withGrant = ComplianceFactory("tenant-alpha", projectOwner: "redacted-ref");
        using WebApplicationFactory<Program> withoutGrant = ComplianceFactory("tenant-alpha");
        using HttpClient grantClient = withGrant.CreateClient();
        using HttpClient noGrantClient = withoutGrant.CreateClient();
        await SeedAsync(withGrant.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-001"));
        await SeedAsync(withoutGrant.Services.GetRequiredService<IWormAuditStore>(), Envelope("tenant-alpha", "audit-record-001"));

        using HttpResponseMessage withAuthority = await grantClient.SendAsync(DetailRequest("audit-record-001"), TestContext.Current.CancellationToken);
        using HttpResponseMessage withoutAuthority = await noGrantClient.SendAsync(DetailRequest("audit-record-001"), TestContext.Current.CancellationToken);
        using HttpResponseMessage unknown = await grantClient.SendAsync(DetailRequest("audit-record-404"), TestContext.Current.CancellationToken);
        using HttpResponseMessage unsafeRef = await grantClient.SendAsync(DetailRequest("raw%20secret"), TestContext.Current.CancellationToken);

        withAuthority.StatusCode.ShouldBe(HttpStatusCode.OK);
        withoutAuthority.StatusCode.ShouldBe(HttpStatusCode.OK);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        unsafeRef.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using JsonDocument available = JsonDocument.Parse(await withAuthority.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using JsonDocument restricted = JsonDocument.Parse(await withoutAuthority.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        available.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("view-metadata");
        restricted.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("request-access");
    }

    [Fact]
    public async Task DetailShouldRequireCurrentAuthorityForEveryReferencedProject()
    {
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha", projectOwner: "project-alpha");
        using HttpClient client = factory.CreateClient();
        AuditEnvelope twoProjects = Envelope("tenant-alpha", "audit-record-two-projects") with
        {
            SourceEvidenceRefs = ["project:project-alpha", "project:project-beta", "source-message:restricted-sentinel"],
        };
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), twoProjects);
        using HttpResponseMessage response = await client.SendAsync(DetailRequest("audit-record-two-projects"), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using JsonDocument detail = JsonDocument.Parse(body);
        detail.RootElement.GetProperty("redactionState").GetString().ShouldBe("escalation-required");
        detail.RootElement.GetProperty("redactionReasonCode").GetString().ShouldBe("restricted-detail");
        detail.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("request-access");
        detail.RootElement.GetProperty("visibleMetadataRefs").EnumerateArray().ShouldBeEmpty();
        body.ShouldNotContain("project-alpha");
        body.ShouldNotContain("project-beta");
        body.ShouldNotContain("restricted-sentinel");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetailRestrictsWhenProjectEvidenceExpiresDuringLaterCheckOrPredatesRequest(bool observedBeforeRequest)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence =>
            {
                if (evidence.Request.Owner != "Projects") { return evidence; }
                if (observedBeforeRequest)
                {
                    return evidence with { ObservedAt = clock.UtcNow.AddSeconds(-1), RevocationCheckedAt = clock.UtcNow.AddSeconds(-1) };
                }

                if (evidence.Request.ResourceId == "project-alpha") { return evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) }; }
                clock.UtcNow += TimeSpan.FromSeconds(2);
                return evidence with { ObservedAt = clock.UtcNow, RevocationCheckedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(5) };
            },
        };
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha").WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
        }));
        using HttpClient client = factory.CreateClient();
        AuditEnvelope envelope = Envelope("tenant-alpha", "audit-record-expiring-projects") with
        {
            SourceEvidenceRefs = ["project:project-alpha", "project:project-beta", "source-message:restricted-sentinel"],
        };
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), envelope);
        using HttpResponseMessage response = await client.SendAsync(DetailRequest(envelope.ResourceId), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using JsonDocument detail = JsonDocument.Parse(body);
        detail.RootElement.GetProperty("redactionState").GetString().ShouldBe("escalation-required");
        detail.RootElement.GetProperty("redactionReasonCode").GetString().ShouldBe("restricted-detail");
        detail.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("request-access");
        detail.RootElement.GetProperty("visibleMetadataRefs").EnumerateArray().ShouldBeEmpty();
        body.ShouldNotContain("project-alpha");
        body.ShouldNotContain("project-beta");
        body.ShouldNotContain("restricted-sentinel");
        owner.Requests.Where(static request => request.Owner == "Projects").Count().ShouldBe(observedBeforeRequest ? 1 : 2);
        owner.Requests.Where(static request => request.Owner == "Projects").ShouldAllBe(static request => request.RequireCurrent);
    }

    /// <summary>
    /// Current project grants do not extend expired compliance or tenant-owner authority, and the remaining project
    /// checks stop at the first lapse.
    /// </summary>
    [Theory]
    [InlineData("ChatBot", 1)]
    [InlineData("ChatBot", 2)]
    [InlineData("Tenants", 1)]
    [InlineData("Tenants", 2)]
    public async Task DetailRestrictsWhenComplianceOwnerEvidenceExpiresDuringProjectChecks(string expiringOwner, int projectCount)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence =>
            {
                if (evidence.Request.Owner == expiringOwner) { return evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) }; }
                if (evidence.Request.Owner == "Projects")
                {
                    clock.UtcNow += TimeSpan.FromSeconds(2);
                    return evidence with { ObservedAt = clock.UtcNow, RevocationCheckedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(5) };
                }
                return evidence;
            },
        };
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha").WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
        }));
        using HttpClient client = factory.CreateClient();
        string[] projectRefs = [.. Enumerable.Range(1, projectCount).Select(static index => $"project:restricted-project-{index}")];
        AuditEnvelope envelope = Envelope("tenant-alpha", "audit-record-expiring-compliance") with { SourceEvidenceRefs = [.. projectRefs, "source-message:restricted-sentinel"] };
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), envelope);
        using HttpResponseMessage response = await client.SendAsync(DetailRequest(envelope.ResourceId), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using JsonDocument detail = JsonDocument.Parse(body);
        detail.RootElement.GetProperty("redactionState").GetString().ShouldBe("escalation-required");
        detail.RootElement.GetProperty("redactionReasonCode").GetString().ShouldBe("restricted-detail");
        detail.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("request-access");
        detail.RootElement.GetProperty("visibleMetadataRefs").EnumerateArray().ShouldBeEmpty();
        body.ShouldNotContain("restricted-project");
        body.ShouldNotContain("restricted-sentinel");
        owner.Requests.ShouldContain(request => request.Owner == expiringOwner);
        owner.Requests.Where(static request => request.Owner == "Projects").ShouldHaveSingleItem().RequireCurrent.ShouldBeTrue();
    }

    /// <summary>
    /// The final-disclosure override restricts detail the handler already decided to release when compliance or
    /// tenant-owner authority lapses after the handler's last authority check.
    /// </summary>
    [Theory]
    [InlineData("ChatBot")]
    [InlineData("Tenants")]
    public async Task DetailOverrideRestrictsReleasedDetailWhenAuthorityLapsesAfterTheHandlerCheck(string expiringOwner)
    {
        // Clock reads after the Projects owner response: evidence validation, the decision instant, the in-loop
        // authority callback, the handler's trailing authority check, then the override's final check.
        const int FinalDisclosureRead = 5;
        (string baseline, int readsAfterProjects) = await SendDetailWithLapseAsync(expiringOwner, lapseOnRead: null);
        using (JsonDocument released = JsonDocument.Parse(baseline))
        {
            released.RootElement.GetProperty("redactionState").GetString().ShouldBe("detail-available");
            released.RootElement.GetProperty("visibleMetadataRefs").EnumerateArray().Select(static item => item.GetString())
                .ShouldContain("project:restricted-project");
        }

        readsAfterProjects.ShouldBe(FinalDisclosureRead);

        (string body, _) = await SendDetailWithLapseAsync(expiringOwner, lapseOnRead: FinalDisclosureRead);
        using JsonDocument detail = JsonDocument.Parse(body);
        detail.RootElement.GetProperty("redactionState").GetString().ShouldBe("escalation-required");
        detail.RootElement.GetProperty("escalationStatus").GetString().ShouldBe("requested");
        detail.RootElement.GetProperty("redactionReasonCode").GetString().ShouldBe("restricted-detail");
        detail.RootElement.GetProperty("safeNextAction").GetString().ShouldBe("request-access");
        detail.RootElement.GetProperty("visibleMetadataRefs").EnumerateArray().ShouldBeEmpty();
        body.ShouldNotContain("restricted-project");
        body.ShouldNotContain("restricted-sentinel");
    }

    private static async Task<(string Body, int ReadsAfterProjects)> SendDetailWithLapseAsync(string expiringOwner, int? lapseOnRead)
    {
        ArmedAdvancingClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence =>
            {
                if (evidence.Request.Owner == expiringOwner) { return evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) }; }
                if (evidence.Request.Owner == "Projects") { clock.Arm(lapseOnRead, TimeSpan.FromSeconds(2)); }
                return evidence;
            },
        };
        using WebApplicationFactory<Program> factory = ComplianceFactory("tenant-alpha").WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
        }));
        using HttpClient client = factory.CreateClient();
        AuditEnvelope envelope = Envelope("tenant-alpha", "audit-record-final-lapse") with { SourceEvidenceRefs = ["project:restricted-project", "source-message:restricted-sentinel"] };
        await SeedAsync(factory.Services.GetRequiredService<IWormAuditStore>(), envelope);
        using HttpResponseMessage response = await client.SendAsync(DetailRequest(envelope.ResourceId), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        owner.Requests.Where(static request => request.Owner == "Projects").ShouldHaveSingleItem();
        return (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), clock.ReadsSinceArmed);
    }

    private static async Task SeedAsync(IWormAuditStore store, AuditEnvelope envelope)
        => await store.AppendAsync(envelope, CancellationToken.None);

    private static string[] AuditRecordRefs(string body)
    {
        using JsonDocument document = JsonDocument.Parse(body);
        return [.. document.RootElement.GetProperty("rows").EnumerateArray().Select(static row => row.GetProperty("auditRecordRef").GetString()!)];
    }

    private static HttpRequestMessage SearchRequest(string tenantId)
        => SearchRequest(tenantId, SearchBody);

    private static HttpRequestMessage SearchRequest(string tenantId, string body)
    {
        HttpRequestMessage request = new(HttpMethod.Post, SearchRoute)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Correlation-Id", CorrelationId);
        request.Headers.Add("X-Test-Tenant", tenantId);
        return request;
    }

    private static string FilterBody(string filterRef, string filterKey, string valueRef)
        => $$"""
            {
              "queryRef": "audit-query-001",
              "filters": [{ "filterRef": "{{filterRef}}", "filterKey": "{{filterKey}}", "valueRef": "{{valueRef}}" }],
              "fromUtc": "2026-06-01T00:00:00+00:00",
              "toUtc": "2026-06-03T00:00:00+00:00",
              "limit": 100
            }
            """;

    private static HttpRequestMessage DetailRequest(string auditRecordRef)
    {
        HttpRequestMessage request = new(HttpMethod.Get, $"/api/v1/compliance/audit/{auditRecordRef}");
        request.Headers.Add("X-Correlation-Id", CorrelationId);
        return request;
    }

    private static AuditEnvelope Envelope(string tenantId, string resourceId)
        => new(
            tenantId,
            "actor-alpha",
            "human",
            "SubmitRetentionConfigurationChange",
            resourceId,
            "allow",
            "pre_commit_gate",
            CorrelationId,
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "policy-snapshot-admin-v1",
            ["admin-scope:compliance", "project:redacted-ref", "source-message:intake-007"],
            null,
            "Received->Proposed",
            "metadata_only",
            "gate_passed",
            AuditCommitPhase.PostCommit,
            "chatbot.audit-envelope.v1",
            null,
            "ui");

    private static WebApplicationFactory<Program> ComplianceFactory(
        string tenantId,
        string? projectOwner = null,
        string role = "compliance-admin",
        string actorType = "human",
        bool includeTenant = true)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                {
                    Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.AddOwners(services);
                    services.AddSingleton<IStartupFilter>(new CompliancePrincipalStartupFilter(tenantId, projectOwner, role, actorType, includeTenant));
                }));

    private sealed class CompliancePrincipalStartupFilter(
        string tenantId,
        string? projectOwner,
        string role,
        string actorType,
        bool includeTenant) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (context, continuation) =>
                {
                    string effectiveTenantId = context.Request.Headers.TryGetValue("X-Test-Tenant", out Microsoft.Extensions.Primitives.StringValues values) && values.Count == 1
                        ? values[0]!
                        : tenantId;
                    List<Claim> claims =
                    [
                        new("sub", "actor-alpha"),
                        new(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                        new(ParticipantAuthorizationStage.TenantRoleClaim, role),
                    ];
                    if (actorType is "ai" or "service")
                    {
                        claims.Add(new Claim(ClaimsServiceClientGrantResolver.ServiceClientIdClaim, "synthetic-machine"));
                    }

                    if (includeTenant)
                    {
                        claims.Add(new Claim("eventstore:tenant", effectiveTenantId));
                    }

                    if (projectOwner is not null)
                    {
                        claims.Add(new Claim(ParticipantAuthorizationStage.ProjectOwnerClaim, projectOwner));
                    }

                    context.User = Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Principal(new ClaimsPrincipal(new ClaimsIdentity(claims, "test")));
                    await continuation().ConfigureAwait(false);
                });
                next(app);
            };
    }

    /// <summary>Counts clock reads after arming and advances once on a chosen read.</summary>
    private sealed class ArmedAdvancingClock : ISystemClock
    {
        private DateTimeOffset _now = new(2026, 10, 7, 10, 0, 0, TimeSpan.Zero);
        private bool _armed;
        private int? _advanceOnRead;
        private TimeSpan _advanceBy;

        public int ReadsSinceArmed { get; private set; }

        public DateTimeOffset UtcNow
        {
            get
            {
                if (_armed && ++ReadsSinceArmed == _advanceOnRead) { _now += _advanceBy; }
                return _now;
            }
        }

        public void Arm(int? advanceOnRead, TimeSpan advanceBy)
        {
            _armed = true;
            ReadsSinceArmed = 0;
            _advanceOnRead = advanceOnRead;
            _advanceBy = advanceBy;
        }
    }
}
#pragma warning restore CA2007
