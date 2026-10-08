using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Lifecycle.AiExecution;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

public sealed class TrustedAuthorityReviewTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    [Theory]
    [InlineData("human")]
    [InlineData("service")]
    [InlineData("ai")]
    public async Task IssuedPrincipalRemovesEveryCallerAuthorityLabel(string actor)
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal(actorClass: actor);
        ((ClaimsIdentity)principal.Identity!).AddClaims([
            new("requester_authority_class", "tenant-admin"), new("chatbot:mailbox-owner", "mailbox-alpha"),
            new("chatbot:project-scope", "*:outbound-draft"), new("chatbot:tenant-outbound-policy", "draft-only"),
            new("eventstore:global-admin", "true"), new("chatbot:delegated-user-id", "party-unverified")]);
        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _).ShouldBeTrue();
        TrustedAuthorityClock clock = new();
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, new SyntheticOwnerAuthorityProvider(clock))
            .AuthorizeAsync(context!, nameof(RecordGovernedNote), false, new RecordGovernedNote(Note), TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeTrue();
        decision.Principal!.Claims.Select(static claim => claim.Type).ShouldAllBe(static type => new[] { "sub", ClaimTypes.NameIdentifier, "eventstore:tenant", "chatbot:actor-type", "chatbot:service-client-id" }.Contains(type));
        decision.Principal.Context.ActorClass.ShouldBe(actor);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ProductionMachineAdmissionAndAuditUseOwnerGrantInsteadOfCallerLabels(bool fakeTokenGrant, bool longReferences)
    {
        TrustedAuthorityClock clock = new();
        string evidenceId = longReferences ? new string('e', 200) : "machine-owner-evidence";
        string evidenceVersion = longReferences ? new string('v', 200) : "owner-version";
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence.ServiceGrant is { } grant
            ? evidence with { EvidenceId = evidenceId, Version = evidenceVersion, ServiceGrant = grant with { GrantId = "owner-only-v2", Scopes = ["owner-exact"] } } : evidence };
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IIdempotencyStore>(new InMemoryCoarseIdempotencyStore(clock));
            services.AddSingleton<IAuditWriter>(audit);
        }));
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal(actorClass: "service");
        if (fakeTokenGrant)
        {
            ((ClaimsIdentity)principal.Identity!).AddClaims([new(ClaimsServiceClientGrantResolver.GrantIdClaim, "token-forged-grant"),
                new(ClaimsServiceClientGrantResolver.GrantScopeClaim, "*"), new(ClaimsServiceClientGrantResolver.GrantCommandClaim, "*")]);
        }
        using IServiceScope scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IServiceClientGrantResolver>().ShouldBeOfType<ClaimsServiceClientGrantResolver>();
        ChatBotCommandAdmissionDecision decision = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>().AdmitAsync(
            new ChatBotCommandSubmission(principal, new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
            {
                CommandId = Note, CommandType = nameof(RecordGovernedNote), Command = new RecordGovernedNote(Note),
                RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
            }, "correlation-alpha", null, ChatBotSurfaceOrigin.Api), TestContext.Current.CancellationToken);
        decision.IsAccepted.ShouldBeTrue(decision.ReasonCode);
        decision.Context!.ServiceClientGrantEvidence!.GrantId.ShouldBe("owner-only-v2");
        decision.Context.AuthorityEvidenceReferences!.ShouldContain($"ChatBot:{evidenceId}:{evidenceVersion}");
        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        envelope.ActorType.ShouldBe("service");
        envelope.SourceEvidenceRefs.ShouldContain("grant:owner-only-v2");
        envelope.SourceEvidenceRefs.ShouldContain($"ChatBot:{evidenceId}:{evidenceVersion}");
        envelope.SourceEvidenceRefs.ShouldContain("grant-scope:owner-exact");
        string.Join('|', envelope.SourceEvidenceRefs).ShouldNotContain("token-forged-grant");
        decision.Context.Actor.Principal.Claims.ShouldNotContain(static claim => claim.Type == ClaimsServiceClientGrantResolver.GrantScopeClaim);
    }

    [Fact]
    public async Task HumanProjectAdmissionPersistsIdentityAndProjectOwnerReferences()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with
        {
            EvidenceId = $"{evidence.Request.Owner.ToLowerInvariant()}-owner-evidence",
            Version = $"{evidence.Request.Owner.ToLowerInvariant()}-v3",
        } };
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IIdempotencyStore>(new InMemoryCoarseIdempotencyStore(clock));
            services.AddSingleton<IAuditWriter>(audit);
        }));
        AssociateEmailToProject command = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
            "project-001",
            AssociationDecisionKind.Associate,
            "Reviewed safe metadata.",
            "hash-project",
            1,
            "chatbot.association-decision-command.v1");
        using IServiceScope scope = factory.Services.CreateScope();
        ChatBotCommandAdmissionDecision decision = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>().AdmitAsync(
            new ChatBotCommandSubmission(TrustedAuthorityFixture.Principal(), new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
            {
                CommandId = Note, CommandType = nameof(AssociateEmailToProject), Command = command,
                RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
            }, "correlation-alpha", null, ChatBotSurfaceOrigin.Api), TestContext.Current.CancellationToken);
        decision.IsAccepted.ShouldBeTrue(decision.ReasonCode);
        owner.Requests.ShouldContain(static request => request.Owner == "Parties" && request.ResourceId == "actor-alpha");
        owner.Requests.ShouldContain(static request => request.Owner == "Projects" && request.ResourceId == "project-001");
        AuditEnvelope envelope = audit.Envelopes.ShouldHaveSingleItem();
        envelope.SourceEvidenceRefs.ShouldContain("Parties:parties-owner-evidence:parties-v3");
        envelope.SourceEvidenceRefs.ShouldContain("Projects:projects-owner-evidence:projects-v3");
    }

    [Theory]
    [InlineData("USER", false)]
    [InlineData("invented", false)]
    [InlineData("", false)]
    [InlineData(null, true)]
    public void QueryOriginDeclarationUsesApiDefaultAndRejectsMalformedTokens(string? origin, bool allowed)
    {
        Dictionary<string, object?> payload = new() { ["noteId"] = Note };
        if (origin is not null) { payload["surfaceOrigin"] = origin; }
        QueryEnvelope query = Query() with { UserId = "service-account-client-alpha", Payload = JsonSerializer.SerializeToUtf8Bytes(payload) };
        ChatBotRequestContext? context = TrustedAuthorityFixture.Resolver(Relay("domain-service:query")).ResolveQuery(query);
        (context is not null).ShouldBe(allowed);
        if (allowed) { context!.Origin.ShouldBe(ChatBotSurfaceOrigin.Api); }
    }

    [Theory]
    [InlineData("{\"surfaceOrigin\":null}")]
    [InlineData("{\"surfaceOrigin\":42}")]
    [InlineData("{\"SurfaceOrigin\":\"api\"}")]
    [InlineData("{\"surfaceOrigin\":\"api\",\"surfaceOrigin\":\"worker\"}")]
    public void MalformedInternalOriginDeclarationCannotBindRelay(string payload)
        => TrustedAuthorityFixture.Resolver(Relay("domain-service:query")).ResolveQuery(Query() with
        { UserId = "service-account-client-alpha", Payload = System.Text.Encoding.UTF8.GetBytes(payload) }).ShouldBeNull();

    [Fact]
    public async Task SyntheticOwnerPersonasAreBoundBySubjectDespiteConstructionOrder()
    {
        ClaimsPrincipal owner = TrustedAuthorityFixture.Principal(subject: "policy-holder");
        ((ClaimsIdentity)owner.Identity!).AddClaim(new(ParticipantAuthorizationStage.TenantRoleClaim, "policy-admin"));
        RegressionAuthorityFixture.Principal(owner);
        ClaimsPrincipal other = TrustedAuthorityFixture.Principal(subject: "operations-holder");
        ((ClaimsIdentity)other.Identity!).AddClaim(new(ParticipantAuthorizationStage.TenantRoleClaim, "operations-admin"));
        RegressionAuthorityFixture.Principal(other);
        ChatBotRequestContextResolver.TryResolve(owner, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? first, out _).ShouldBeTrue();
        ChatBotRequestContextResolver.TryResolve(other, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? second, out _).ShouldBeTrue();
        TrustedAuthorityClock clock = new();
        ChatBotRequestAuthorizer authorizer = RegressionAuthorityFixture.Authorizer(clock);
        ChatBotAuthorityDecision allowed = await authorizer.AuthorizeAsync(first!, nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "policy-alpha" }, TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
        allowed.Principal!.AdminScope.ShouldBe("policy");
        allowed.Principal.HasAdminScope("operate").ShouldBeFalse();
        allowed.Principal.Claims.Where(static claim => claim.Type == ParticipantAuthorizationStage.TenantRoleClaim).Select(static claim => claim.Value).ShouldBe(["policy-admin"]);
        (await authorizer.AuthorizeAsync(second!, nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "policy-alpha" }, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task DeclaredRelayOriginCannotBroadenExactMachineGrant()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = static evidence => evidence.ServiceGrant is { } grant
            ? evidence with { ServiceGrant = grant with { SurfaceOrigin = ChatBotSurfaceOrigin.Api } } : evidence };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(Relay("domain-service:query")), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        QueryEnvelope query = Query() with { UserId = "service-account-client-alpha", Payload = JsonSerializer.SerializeToUtf8Bytes(new { noteId = Note, surfaceOrigin = "worker" }) };
        (await handler.ExecuteAsync(query, TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        store.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task MachineWithNoAllowedQueriesDeniesBeforeProtectedRead()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence.ServiceGrant is { } grant ? evidence with { ServiceGrant = grant with { AllowedQueryNames = [] } } : evidence };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        (await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        store.Reads.ShouldBe(0);
    }

    /// <summary>Cleared permission lists cannot hide a valid revocation of previously cached operations.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokedGrantWithEmptyPermissionsImmediatelyInvalidatesCachedQueries(bool evidenceRevoked)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), owner, clock, cache);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        (await authorizer.ResolveServiceGrantAsync(context, ChatBotReadQueryTypes.GovernedOperation, true, false, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        ChatBotOwnerAuthorityRequest cached = owner.Requests.Single();
        owner.Transform = evidence => evidence.ServiceGrant is { } grant
            ? evidence with { IsAllowed = !evidenceRevoked, IsRevoked = evidenceRevoked,
                ServiceGrant = grant with { IsRevoked = !evidenceRevoked, Scopes = [], AllowedCommandNames = [], AllowedQueryNames = [] } }
            : evidence;
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        cache.IsRevoked(cached, "synthetic-grant-v1").ShouldBeTrue();
        owner.Transform = static evidence => evidence;
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, store);
        (await handler.ExecuteAsync(Query(), TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        store.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task ClientWideRevocationDeniesCachedReadThenOnlyNewerOwnerGrantReopensIt()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), owner, clock, cache);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        owner.Transform = static evidence => evidence with { IsAllowed = false, IsRevoked = true, ServiceGrant = null };
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        owner.Transform = static evidence => evidence;
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldBeNull();
        clock.UtcNow = clock.UtcNow.AddSeconds(1);
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Fact]
    public async Task CurrentMachineGrantIsNotRetainedUnderUnreadCurrentCacheKey()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), owner, clock, cache);
        (await authorizer.ResolveServiceGrantAsync(TrustedAuthorityFixture.Context(actorClass: "service"), nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        cache.TryGetEvidence(owner.Requests.Single()).ShouldBeNull();
    }

    [Theory]
    [InlineData("AssigneeRef")]
    [InlineData("ReviewerRef")]
    [InlineData("PreviousAssigneeRef")]
    public async Task QueuePartyReferencesRequireExactCurrentPartiesEvidence(string property)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static request => request.ResourceId != "party-denied" };
        var payload = new Dictionary<string, object?> { ["OperationId"] = Note, [property] = "party-denied" };
        (await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(ExecuteAdminQueueOperation), false, payload, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
        owner.Requests.ShouldContain(static request => request.Owner == "Parties" && request.ResourceId == "party-denied" && request.RequireCurrent);
        payload[property] = null;
        (await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(ExecuteAdminQueueOperation), false, payload, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("worker", true)]
    [InlineData("invented", false)]
    public async Task WorkloadAuthenticatedSdkRelayBindsServiceAccountAndClosedOrigin(string? origin, bool allowed)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        CountingGovernedOperationStore store = new() { View = new("tenant-alpha", Note, GovernedOperationView.CurrentSchemaVersion, "synthetic", "v1", "metadata_only", "test", 1, clock.UtcNow, clock.UtcNow) };
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IIdempotencyStore>(new InMemoryCoarseIdempotencyStore(clock));
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IGovernedOperationProjectionStore>(store);
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(Relay));
        }));
        using HttpClient client = factory.CreateClient();
        Dictionary<string, object?> queryPayload = new() { ["noteId"] = Note };
        if (origin is not null) { queryPayload["surfaceOrigin"] = origin; }
        QueryEnvelope query = Query() with { UserId = "service-account-client-alpha", OriginalActorId = "service-account-client-alpha", AuthenticatedWorkloadId = "client-alpha", Payload = JsonSerializer.SerializeToUtf8Bytes(queryPayload) };
        using HttpResponseMessage response = await client.PostAsJsonAsync("/query", query, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        QueryResult result = (await response.Content.ReadFromJsonAsync<QueryResult>(cancellationToken: TestContext.Current.CancellationToken))!;
        result.Success.ShouldBe(allowed);
        store.Reads.ShouldBe(allowed ? 1 : 0);
        if (allowed)
        {
            owner.Requests.ShouldAllBe(request => request.PrincipalId == query.UserId && request.TenantId == "tenant-alpha" && request.ActorClass == "service" && ChatBotSurfaceOrigins.ToWireValue(request.Origin) == (origin ?? "api"));
            Dictionary<string, string> extensions = new() { ["actorType"] = "service", ["serviceClientId"] = "client-alpha", ["surfaceOrigin"] = origin ?? "api" };
            CommandEnvelope command = new(Note, "tenant-alpha", "chatbot", Note, nameof(RecordGovernedNote), JsonSerializer.SerializeToUtf8Bytes(new RecordGovernedNote(Note)), "correlation-alpha", null, query.UserId, extensions);
            using HttpResponseMessage process = await client.PostAsJsonAsync("/process", new DomainServiceRequest(command, CurrentState: null), TestContext.Current.CancellationToken);
            process.StatusCode.ShouldBe(HttpStatusCode.OK, await process.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            DomainServiceWireResult processed = (await process.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
            processed.IsRejection.ShouldBeFalse();
            DomainServiceWireEvent recorded = processed.Events.ShouldHaveSingleItem();
            recorded.EventTypeName.ShouldBe(typeof(Hexalith.ChatBot.Server.Operations.GovernedNoteRecorded).FullName);
            using JsonDocument body = JsonDocument.Parse(recorded.Payload);
            body.RootElement.GetProperty("NoteId").GetString().ShouldBe(Note);
        }
    }

    [Theory]
    [InlineData("surfaceOrigin", null)]
    [InlineData("SurfaceOrigin", "api")]
    [InlineData("surfaceOrigin", "invented")]
    [InlineData("actorType", "human")]
    [InlineData("serviceClientId", "client-other")]
    public void CommandRelayRejectsMalformedOrConflictingInternalDeclarations(string field, string? value)
    {
        CommandEnvelope command = new(Note, "tenant-alpha", "chatbot", Note, nameof(RecordGovernedNote), [], "correlation-alpha", null,
            "service-account-client-alpha", new() { [field] = value! });
        TrustedAuthorityFixture.Resolver(Relay("domain-service:process")).ResolveCommand(command).ShouldBeNull();
    }

    [Theory]
    [InlineData("domain-service:process")]
    [InlineData("domain-service:query")]
    public void OpaqueRelayActorCannotGainHumanAuthorityFromOAuthClientOrExtensions(string operation)
    {
        ClaimsPrincipal relay = Relay(operation);
        ChatBotRequestContextResolver resolver = TrustedAuthorityFixture.Resolver(relay);
        QueryEnvelope query = Query() with { OriginalActorId = "actor-alpha", AuthenticatedWorkloadId = "oauth-web-client" };
        resolver.ResolveQuery(query).ShouldBeNull();
        CommandEnvelope command = new(Note, "tenant-alpha", "chatbot", Note, nameof(RecordGovernedNote), [], "correlation-alpha", null, "opaque-machine-subject", new() { ["actorType"] = "human", ["serviceClientId"] = "opaque-client" });
        resolver.ResolveCommand(command).ShouldBeNull();
    }

    [Theory]
    [InlineData("domain-service:process")]
    [InlineData("wrong-operation")]
    public async Task WrongWorkloadOperationCannotBindRelayedRead(string operation)
    {
        TrustedAuthorityClock clock = new();
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(Relay(operation)), TrustedAuthorityFixture.Authorizer(clock, new SyntheticOwnerAuthorityProvider(clock)), store);
        (await handler.ExecuteAsync(Query() with { UserId = "service-account-client-alpha" }, TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        store.Reads.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecoveryDeniesBeforeAnyStoreAccessWithoutCurrentOperationsOwner(bool authenticated)
    {
        TrustedAuthorityClock clock = new();
        IAiExecutionWorkStore store = DispatchProxy.Create<IAiExecutionWorkStore, ProtectedAccessProbe>();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(new SyntheticOwnerAuthorityProvider(clock) { Allows = static _ => false });
            services.AddSingleton(store);
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(_ => authenticated ? TrustedAuthorityFixture.Principal() : new ClaimsPrincipal()));
            foreach (ServiceDescriptor descriptor in services.Where(static descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToArray()) { services.Remove(descriptor); }
        }));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage list = await client.GetAsync("/api/v1/operations/ai-executions/exhausted", TestContext.Current.CancellationToken);
        using HttpResponseMessage recover = await client.PostAsJsonAsync("/api/v1/operations/ai-executions/exhausted/recover", new { key = "foreign-key" }, TestContext.Current.CancellationToken);
        list.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        recover.StatusCode.ShouldBe(list.StatusCode);
        (await recover.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldBe(await list.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        ((ProtectedAccessProbe)(object)store).Calls.ShouldBe(0);
    }

    [Theory]
    [InlineData("{\"noteId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAY\",\"NoteId\":\"forbidden-note\"}")]
    [InlineData("{\"NoteId\":\"forbidden-note\",\"noteId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAY\"}")]
    [InlineData("{\"noteId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAY\",\"noteId\":\"01ARZ3NDEKTSV4RRFFQ69G5FAY\"}")]
    public async Task OriginalDuplicateScopeEvidenceDeniesBeforeProtectedReads(string payload)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static request => request.ResourceId == Note };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        QueryResult result = await handler.ExecuteAsync(Query() with { Payload = System.Text.Encoding.UTF8.GetBytes(payload) }, TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        store.Reads.ShouldBe(0);
        owner.Requests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WorkloadSdkCannotEmitUnderMismatchedOrUnsupportedAggregate(bool unsupported)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IIdempotencyStore>(idempotency);
            services.AddSingleton<IAuditWriter>(audit);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(Relay));
        }));
        using HttpClient client = factory.CreateClient();
        CommandEnvelope command = new(Note, "tenant-alpha", "chatbot", "forbidden-aggregate",
            unsupported ? nameof(ExecuteAdminQueueOperation) : nameof(RecordGovernedNote),
            JsonSerializer.SerializeToUtf8Bytes(unsupported ? (object)new { OperationId = Note } : new RecordGovernedNote(Note)),
            "correlation-alpha", null, "service-account-client-alpha", new() { ["actorType"] = "service", ["serviceClientId"] = "client-alpha" });
        using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(command, CurrentState: null), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult result = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldNotContain(static item => item.EventTypeName == typeof(Hexalith.ChatBot.Server.Operations.GovernedNoteRecorded).FullName);
        owner.Requests.ShouldBeEmpty();
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
    }

    [Fact]
    public async Task OwnerExpiryDuringRiskAwaitDeniesBeforeDurableAdmissionAndAllowsFreshRetry()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        InMemoryAuditWriter audit = new();
        bool advance = true;
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IIdempotencyStore>(idempotency);
            services.AddSingleton<IAuditWriter>(audit);
            services.AddSingleton<IRiskClassifier>(new TrustedAuthorityAdvancingRiskClassifier(() => { if (advance) { clock.UtcNow += TimeSpan.FromSeconds(2); } }));
        }));
        using IServiceScope scope = factory.Services.CreateScope();
        ChatBotCommandAdmissionPipeline admission = scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>();
        ChatBotCommandSubmission submission = new(TrustedAuthorityFixture.Principal(), new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
        {
            CommandId = Note, CommandType = nameof(RecordGovernedNote), Command = new RecordGovernedNote(Note),
            RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
        }, "correlation-alpha", null, ChatBotSurfaceOrigin.Api);
        ChatBotCommandAdmissionDecision expired = await admission.AdmitAsync(submission, TestContext.Current.CancellationToken);
        expired.IsAccepted.ShouldBeFalse();
        expired.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        advance = false;
        (await admission.AdmitAsync(submission, TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        idempotency.RecordCount.ShouldBe(1);
        audit.Envelopes.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"key\":null}")]
    [InlineData("{\"key\":\"\"}")]
    public async Task RecoveryMissingNullOrEmptyKeyReturnsSafeNotFoundWithoutStoreCalls(string payload)
    {
        TrustedAuthorityClock clock = new();
        IAiExecutionWorkStore store = DispatchProxy.Create<IAiExecutionWorkStore, ProtectedAccessProbe>();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(new SyntheticOwnerAuthorityProvider(clock));
            services.AddSingleton(store);
            services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(_ => TrustedAuthorityFixture.Principal()));
            foreach (ServiceDescriptor descriptor in services.Where(static descriptor => descriptor.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)).ToArray()) { services.Remove(descriptor); }
        }));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsync("/api/v1/operations/ai-executions/exhausted/recover",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).ShouldContain(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((ProtectedAccessProbe)(object)store).Calls.ShouldBe(0);
    }

    private static QueryEnvelope Query() => new("tenant-alpha", "chatbot", Note, ChatBotReadQueryTypes.GovernedOperation, JsonSerializer.SerializeToUtf8Bytes(new { noteId = Note }), "correlation-alpha", "actor-alpha");

    private static ClaimsPrincipal Relay(string operation) => new(new ClaimsIdentity([
        new(ClaimTypes.NameIdentifier, "workload:eventstore"), new("eventstore:workload", "eventstore"), new("eventstore:operation", operation)], "EventStoreWorkload"));
}
