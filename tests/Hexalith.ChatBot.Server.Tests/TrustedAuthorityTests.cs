using System.Reflection;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authentication;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Governance.Admin;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;

namespace Hexalith.ChatBot.Server.Tests;

public sealed class TrustedAuthorityTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    [Fact]
    public async Task CurrentExactOwnerEvidenceAuthorizesWithoutTokenRoles()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(
            TrustedAuthorityFixture.Context(), nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "change-alpha" }, TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeTrue();
        AdminAuthorityEvaluator.HasHumanAdminScope(decision.Principal!, AdminScope.Policy).ShouldBeTrue();
        owner.Requests.ShouldContain(static request => request.Owner == "Tenants" && request.Authority == "TenantOwner");
        owner.Requests.ShouldContain(static request => request.Owner == "Parties" && request.Authority == "identity");
        decision.EvidenceReferences.ShouldAllBe(static reference => reference.StartsWith("ChatBot:synthetic-", StringComparison.Ordinal) || reference.StartsWith("Tenants:synthetic-", StringComparison.Ordinal) || reference.StartsWith("Parties:synthetic-", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ChatBot")]
    [InlineData("Tenants")]
    [InlineData("Parties")]
    public async Task MissingRequiredOwnerDeniesAdministration(string deniedOwner)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.Owner != deniedOwner };
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(
            TrustedAuthorityFixture.Context(), nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "change-alpha" }, TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeFalse();
        decision.EvidenceReferences.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("principal")]
    [InlineData("tenant")]
    [InlineData("resource")]
    [InlineData("operation")]
    [InlineData("version")]
    [InlineData("owner-pii")]
    [InlineData("version-pii")]
    [InlineData("future")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("five-minutes")]
    [InlineData("sixty-seconds")]
    public async Task InvalidOwnerEvidenceDeniesBeforeProtectedRead(string scenario)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => scenario switch
            {
                "owner" => evidence with { Request = evidence.Request with { Owner = "mirror" } },
                "principal" => evidence with { Request = evidence.Request with { PrincipalId = "actor-other" } },
                "tenant" => evidence with { Request = evidence.Request with { TenantId = "tenant-beta" } },
                "resource" => evidence with { Request = evidence.Request with { ResourceId = "resource-other" } },
                "operation" => evidence with { Request = evidence.Request with { Operation = "operation-other" } },
                "version" => evidence with { Version = "" },
                "owner-pii" => evidence with { EvidenceId = "owner-email@example.test" },
                "version-pii" => evidence with { Version = "owner-email@example.test" },
                "future" => evidence with { ObservedAt = clock.UtcNow.AddSeconds(1), RevocationCheckedAt = clock.UtcNow.AddSeconds(1) },
                "expired" => evidence with { ExpiresAt = clock.UtcNow },
                "revoked" => evidence with { IsRevoked = true },
                "five-minutes" => evidence with { ObservedAt = clock.UtcNow.AddMinutes(-5) },
                "sixty-seconds" => evidence with { ObservedAt = clock.UtcNow.AddMinutes(-1), RevocationCheckedAt = clock.UtcNow.AddSeconds(-60) },
                _ => throw new InvalidOperationException(),
            },
        };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        QueryResult result = await handler.ExecuteAsync(Envelope(), TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        store.Reads.ShouldBe(0);
        store.Writes.ShouldBe(0);
    }

    [Fact]
    public async Task SensitiveMutationRevalidatesWhileFreshOrdinaryReadMayUseObservedEvidence()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ObservedAt = clock.UtcNow.AddSeconds(-1), RevocationCheckedAt = clock.UtcNow.AddSeconds(-1) } };
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        (await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), ChatBotReadQueryTypes.GovernedOperation, true, new { NoteId = Note }, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
        (await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(RecordGovernedNote), false, new RecordGovernedNote(Note), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("sub", "actor-other")]
    [InlineData("tenant", "tenant-beta")]
    [InlineData("actor_type", "service")]
    [InlineData("tenant", "")]
    [InlineData("sub", "malformed subject")]
    public void ConflictingOrMalformedAuthenticatedEvidenceDenies(string type, string value)
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        principal.AddIdentity(new ClaimsIdentity([new Claim(type, value)], "authenticated-supplement"));
        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Ui, out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void UnauthenticatedSupplementalEvidenceCannotChangeImmutableBinding()
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        principal.AddIdentity(new ClaimsIdentity([new Claim("sub", "actor-other"), new Claim("tenant", "tenant-beta"), new Claim("actor_type", "service")]));
        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Mcp, out ChatBotRequestContext? context, out _).ShouldBeTrue();
        ((ClaimsIdentity)principal.Identity!).AddClaim(new("sub", "mutated-actor"));
        ((ClaimsIdentity)context!.Principal.Identity!).AddClaim(new("tenant", "mutated-tenant"));
        context.SubjectId.ShouldBe("actor-alpha");
        context.TenantId.ShouldBe("tenant-alpha");
        context.ActorClass.ShouldBe("human");
        context.Origin.ShouldBe(ChatBotSurfaceOrigin.Mcp);
        context.Principal.FindAll("tenant").ShouldBeEmpty();
    }

    [Fact]
    public void HumanOAuthApplicationIdDoesNotClassifyAsMachine()
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        ((ClaimsIdentity)principal.Identity!).AddClaim(new("azp", "web-client"));
        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _).ShouldBeTrue();
        context!.IsMachine.ShouldBeFalse();
        context.ServiceClientId.ShouldBeNull();
    }

    [Theory]
    [InlineData("service")]
    [InlineData("ai")]
    public async Task MachineCannotGainHumanAuthorityThroughRolesOriginOrGlobalAdmin(string actorClass)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal(actorClass: actorClass);
        ((ClaimsIdentity)principal.Identity!).AddClaims([new(ParticipantAuthorizationStage.TenantRoleClaim, "tenant-admin"), new(ParticipantAuthorizationStage.ProjectOwnerClaim, "*"), new("eventstore:global-admin", "true")]);
        AdminAuthorityEvaluator.HasHumanTenantAdmin(principal).ShouldBeFalse();
        foreach (ChatBotSurfaceOrigin origin in Enum.GetValues<ChatBotSurfaceOrigin>())
        {
            ChatBotRequestContextResolver.TryResolve(principal, origin, out ChatBotRequestContext? context, out _).ShouldBeTrue();
            (await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(context!, nameof(SubmitTenantPolicyChange), false, new { PolicyChangeId = "change-alpha" }, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData("tenant-beta", "actor-alpha")]
    [InlineData("tenant-alpha", "actor-other")]
    public async Task ForgedSdkEnvelopeNeverConfersAuthority(string tenant, string user)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        QueryResult result = await handler.ExecuteAsync(Envelope() with { TenantId = tenant, UserId = user, IsGlobalAdmin = true }, TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        owner.Requests.ShouldBeEmpty();
        store.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task UnauthenticatedSdkFlagsDenyWithoutProtectedEffects()
    {
        TrustedAuthorityClock clock = new();
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(new ClaimsPrincipal()), TrustedAuthorityFixture.Authorizer(clock, new SyntheticOwnerAuthorityProvider(clock)), store);
        QueryResult result = await handler.ExecuteAsync(Envelope() with { IsGlobalAdmin = true }, TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        store.Reads.ShouldBe(0);
    }

    [Fact]
    public async Task ExactSyntheticSdkAuthorityAccessesOnlyBoundResource()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.TenantId == "tenant-alpha" && request.PrincipalId == "actor-alpha" && (request.Owner == "Parties" || request.ResourceId == Note) };
        CountingGovernedOperationStore store = new()
        {
            View = new("tenant-alpha", Note, GovernedOperationView.CurrentSchemaVersion, "synthetic", "v1", "metadata_only", "test", 1, clock.UtcNow, clock.UtcNow),
        };
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        (await handler.ExecuteAsync(Envelope(), TestContext.Current.CancellationToken)).Success.ShouldBeTrue();
        store.Reads.ShouldBe(1);
        QueryEnvelope forbidden = Envelope() with { Payload = JsonSerializer.SerializeToUtf8Bytes(new GovernedOperationQuery("01ARZ3NDEKTSV4RRFFQ69G5FAZ", null)) };
        (await handler.ExecuteAsync(forbidden, TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        store.Reads.ShouldBe(1);
    }

    [Fact]
    public async Task UnsupportedProductionOwnerMappingsDeny()
    {
        TrustedAuthorityClock clock = new();
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, new UnavailableChatBotOwnerAuthorityProvider());
        (await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(RecordGovernedNote), false, new RecordGovernedNote(Note), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task KnownServiceGrantRevocationDeniesImmediatelyAndCannotBeRefreshedByToken()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new ChatBotAuthorityCatalog(), owner, clock, cache);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        var grant = await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken);
        grant.ShouldNotBeNull();
        cache.InvalidateRevocation("tenant-alpha", "client-alpha", "api", grant.GrantId);
        typeof(ServiceClientGrantProjectionCache).GetMethod("Upsert", [grant.GetType()]).ShouldBeNull();
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await authorizer.ResolveServiceGrantAsync(TrustedAuthorityFixture.Context("tenant-beta", "service"), nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Fact]
    public async Task RevokedOwnerGrantObservationCreatesTombstoneBeforeLaterAllowedRefresh()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => evidence.ServiceGrant is { } grant ? evidence with { ServiceGrant = grant with { IsRevoked = true } } : evidence,
        };
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new ChatBotAuthorityCatalog(), owner, clock, cache);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        owner.Transform = static evidence => evidence;
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await authorizer.ResolveServiceGrantAsync(TrustedAuthorityFixture.Context("tenant-beta", "service"), nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallerProjectReadFlagCannotReplaceCurrentProjectsAuthority(bool projectReadAuthorized)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.Owner != "Projects" };
        IProjectConversationProjectionStore store = System.Reflection.DispatchProxy.Create<IProjectConversationProjectionStore, ProtectedAccessProbe>();
        Hexalith.ChatBot.Server.Governance.AiMediation.IMailboxMessageContentSource content = System.Reflection.DispatchProxy.Create<Hexalith.ChatBot.Server.Governance.AiMediation.IMailboxMessageContentSource, ProtectedAccessProbe>();
        TaskIntentReviewQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store, content);
        QueryEnvelope envelope = new("tenant-alpha", "chatbot", "intent-alpha", ChatBotReadQueryTypes.TaskIntentReview,
            JsonSerializer.SerializeToUtf8Bytes(new TaskIntentReviewQuery("project-alpha", "intent-alpha", projectReadAuthorized, null)), "correlation-alpha", "actor-alpha") { IsGlobalAdmin = true };
        QueryResult result = await handler.ExecuteAsync(envelope, TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((ProtectedAccessProbe)(object)store).Calls.ShouldBe(0);
        ((ProtectedAccessProbe)(object)content).Calls.ShouldBe(0);
        owner.Requests.ShouldContain(static request => request.Owner == "Projects" && request.ResourceId == "project-alpha");
    }

    [Theory]
    [InlineData("Projects")]
    [InlineData("Parties")]
    public async Task NestedPluralOwnerReferencesRequireEveryExactCurrentGrant(string ownerName)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.ResourceId != "foreign-resource" };
        object scope = ownerName == "Projects" ? new { ProjectScopeRefs = new[] { "project-alpha", "foreign-resource" } } : (object)new { RecipientRefs = new[] { "party-alpha", "foreign-resource" } };
        ChatBotAuthorityDecision decision = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(
            TrustedAuthorityFixture.Context(), nameof(RecordGovernedNote), false, new { NoteId = Note, Nested = scope }, TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeFalse();
        owner.Requests.ShouldContain(request => request.Owner == ownerName && request.ResourceId == "foreign-resource" && request.RequireCurrent);
    }

    [Fact]
    public async Task ForeignRevokedGrantObservationCannotPoisonTheBoundTenant()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => evidence.ServiceGrant is { } grant
                ? evidence with { ServiceGrant = grant with { TenantId = "tenant-beta", IsRevoked = true } } : evidence,
        };
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        owner.Transform = static evidence => evidence;
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Theory]
    [InlineData("null-scopes")]
    [InlineData("null-commands")]
    [InlineData("null-queries")]
    [InlineData("invalid-class")]
    [InlineData("owner-pii")]
    public async Task MalformedOwnerGrantDeniesWithoutThrowingOrCachingAuthority(string scenario)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => evidence.ServiceGrant is { } grant ? evidence with
            {
                ServiceGrant = scenario switch
                {
                    "null-scopes" => grant with { Scopes = null! },
                    "null-commands" => grant with { AllowedCommandNames = null! },
                    "null-queries" => grant with { AllowedQueryNames = null! },
                    "invalid-class" => grant with { ClientClass = (ServiceClientClass)999 },
                    "owner-pii" => grant with { DelegatedUserId = "owner-email@example.test" },
                    _ => throw new InvalidOperationException(),
                },
            } : evidence,
        };
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        (await authorizer.ResolveServiceGrantAsync(TrustedAuthorityFixture.Context(actorClass: "service"), nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Theory]
    [InlineData("SUB", "actor-alpha")]
    [InlineData("EVENTSTORE:TENANT", "tenant-alpha")]
    [InlineData("TENANT", "tenant-alpha")]
    [InlineData("CHATBOT:ACTOR-TYPE", "human")]
    [InlineData("ACTOR_TYPE", "human")]
    [InlineData("CHATBOT:SERVICE-CLIENT-ID", "client-alpha")]
    [InlineData("PREFERRED_USERNAME", "service-account-client-alpha")]
    public void CaseMismatchedAuthenticatedClaimTypesDenyEvenWithExactClaims(string type, string value)
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        ((ClaimsIdentity)principal.Identity!).AddClaim(new(type, value));
        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out _, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("user")]
    public async Task OwnerAuthorizedNormalizedHumanCanCorrectBothBoundProjects(string? actorType)
    {
        ClaimsPrincipal principal = TrustedAuthorityFixture.Principal();
        ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;
        identity.RemoveClaim(identity.FindFirst(ParticipantAuthorizationStage.ActorTypeClaim)!);
        if (actorType is not null)
        {
            identity.AddClaim(new(ParticipantAuthorizationStage.ActorTypeClaim, actorType));
        }

        ChatBotRequestContextResolver.TryResolve(principal, ChatBotSurfaceOrigin.Api, out ChatBotRequestContext? context, out _).ShouldBeTrue();
        context!.ActorClass.ShouldBe("human");
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        CorrectEmailProjectAssociation correction = new(Note, "intake-alpha", "project-prior", "project-target", AssociationCorrectionKind.ProjectReassignment,
            null, "association-prior", "fingerprint-alpha", 1, "v1");
        ChatBotCommandSubmission submission = new(principal, new CommandSubmissionRequest { CommandType = nameof(CorrectEmailProjectAssociation), Command = correction }, "correlation-alpha", null);
        ParticipantAuthorizationStage stage = new(requestAuthorizer: TrustedAuthorityFixture.Authorizer(clock, owner));
        ChatBotAuthorizationResult result = await stage.AuthorizeAsync(submission, new("actor-alpha", principal, RequestContext: context), new("tenant-alpha"), TestContext.Current.CancellationToken);
        result.IsAllowed.ShouldBeTrue();
        owner.Requests.ShouldContain(static request => request.Owner == "Projects" && request.ResourceId == "project-prior" && request.RequireCurrent);
        owner.Requests.ShouldContain(static request => request.Owner == "Projects" && request.ResourceId == "project-target" && request.RequireCurrent);
    }

    [Theory]
    [InlineData(nameof(SubmitTenantPolicyChange), "PolicyChangeId", "RequesterRef")]
    [InlineData(nameof(ApproveTenantPolicyChange), "PolicyChangeId", "ApproverRef")]
    [InlineData(nameof(ResolveMailboxMessageParticipants), "ResolutionId", "ResolvedPartyRef")]
    [InlineData(nameof(ProposeAIAction), "ProjectId", "RecipientReferences")]
    [InlineData(nameof(ExecuteApprovedAIAction), "ProjectId", "RecipientReferences")]
    public async Task DeniedContractPartyReferencesHaveNoProtectedEffects(string operation, string resourceProperty, string referenceProperty)
    {
        object reference = referenceProperty == "RecipientReferences" ? new[] { "party-allowed", "party-forbidden" } : (object)"party-forbidden";
        Dictionary<string, object> payload = new(StringComparer.Ordinal)
        {
            [resourceProperty] = "resource-alpha",
            ["nested"] = new Dictionary<string, object>(StringComparer.Ordinal) { [referenceProperty] = reference },
        };
        await AssertAdmissionDeniedWithoutProtectedEffectsAsync(operation, payload, "Parties", "party-forbidden");
    }

    [Fact]
    public async Task ForbiddenOriginatingFailedEventDeniesRetryBeforeProtectedEffects()
        => await AssertAdmissionDeniedWithoutProtectedEffectsAsync(nameof(RequestFailedWorkflowRetry),
            new RequestFailedWorkflowRetry(Note, "01ARZ3NDEKTSV4RRFFQ69G5FAZ", "workflow", "retry-required", 1, null), "ChatBot", "01ARZ3NDEKTSV4RRFFQ69G5FAZ");

    [Fact]
    public async Task UnresolvedOptionalPartyReferenceDoesNotInventAnIdentity()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        (await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(RecordGovernedNote), false,
            new { NoteId = Note, Nested = new { ResolvedPartyRef = (string?)null } }, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
        owner.Requests.Where(static request => request.Owner == "Parties").ShouldHaveSingleItem().ResourceId.ShouldBe("actor-alpha");
    }

    [Theory]
    [InlineData("expiry", false)]
    [InlineData("sixty-seconds", false)]
    [InlineData("five-minutes", false)]
    [InlineData("expiry", true)]
    public async Task FinalDecisionRevalidatesEarlierEvidenceAfterLaterOwnerOrMachineGrantAwait(string bound, bool machine)
    {
        TrustedAuthorityClock clock = new();
        DateTimeOffset first = clock.UtcNow;
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence =>
            {
                if (evidence.Request.Owner == "ChatBot" && evidence.Request.Authority == "operation")
                {
                    return bound switch
                    {
                        "expiry" => evidence with { ExpiresAt = first.AddSeconds(1) },
                        "five-minutes" => evidence with { ObservedAt = first.AddMinutes(-5).AddSeconds(1) },
                        _ => evidence,
                    };
                }

                clock.UtcNow = first.AddSeconds(bound == "sixty-seconds" ? 60 : 2);
                return evidence with { ObservedAt = clock.UtcNow, RevocationCheckedAt = clock.UtcNow, ExpiresAt = clock.UtcNow.AddMinutes(5) };
            },
        };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: machine ? "service" : "human")), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        (await handler.ExecuteAsync(Envelope(), TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        owner.Requests.Count.ShouldBeGreaterThan(1);
        if (machine)
        {
            owner.Requests.ShouldContain(static request => request.Authority == "service-grant");
        }

        store.Reads.ShouldBe(0);
        store.Writes.ShouldBe(0);
    }

    [Fact]
    public async Task RecentExplicitRevocationTombstonesPriorCachedGrantDuringCurrentFetch()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new ChatBotAuthorityCatalog(), owner, clock, cache);
        ChatBotRequestContext context = TrustedAuthorityFixture.Context(actorClass: "service");
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldNotBeNull();
        owner.Transform = evidence => evidence with { IsAllowed = false, IsRevoked = true, ObservedAt = clock.UtcNow.AddSeconds(-1), RevocationCheckedAt = clock.UtcNow.AddSeconds(-1) };
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
        owner.Transform = static evidence => evidence;
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, false, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await authorizer.ResolveServiceGrantAsync(context, nameof(RecordGovernedNote), false, true, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnerTimeoutDeniesWhileCallerCancellationPropagates(bool callerCanceled)
    {
        TrustedAuthorityClock clock = new();
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = _ =>
            {
                if (callerCanceled)
                {
                    cancellation.Cancel();
                }

                throw new OperationCanceledException("restricted-owner-timeout@example.test", cancellation.Token);
            },
        };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal()), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        if (callerCanceled)
        {
            await Should.ThrowAsync<OperationCanceledException>(() => handler.ExecuteAsync(Envelope(), cancellation.Token));
        }
        else
        {
            (await handler.ExecuteAsync(Envelope(), cancellation.Token)).Success.ShouldBeFalse();
        }

        store.Reads.ShouldBe(0);
        store.Writes.ShouldBe(0);
    }

    [Fact]
    public async Task PiiGrantScopeDeniesBeforeAuditEnvelopeCreation()
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock)
        {
            Transform = evidence => evidence.ServiceGrant is { } grant ? evidence with { ServiceGrant = grant with { Scopes = ["owner-email@example.test"] } } : evidence,
        };
        using WebApplicationFactory<Program> factory = AuthorityFactory(clock, owner);
        using IServiceScope scope = factory.Services.CreateScope();
        ChatBotCommandAdmissionDecision decision = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>().AdmitAsync(
            new(TrustedAuthorityFixture.Principal(actorClass: "service"), new CommandSubmissionRequest { CommandId = Note, CommandType = nameof(RecordGovernedNote), Command = new RecordGovernedNote(Note) }, "correlation-alpha", null), TestContext.Current.CancellationToken);
        decision.IsAccepted.ShouldBeFalse();
        decision.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantOverScoped);
        InMemoryAuditWriter audit = factory.Services.GetRequiredService<InMemoryAuditWriter>();
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem();
        JsonSerializer.Serialize(audit.AuthorizationFailures).ShouldNotContain("owner-email@example.test");
    }

    private static async Task AssertAdmissionDeniedWithoutProtectedEffectsAsync(string operation, object payload, string deniedOwner, string deniedResource)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.Owner != deniedOwner || request.ResourceId != deniedResource };
        IRiskClassifier risk = DispatchProxy.Create<IRiskClassifier, ProtectedAccessProbe>();
        IApprovalGate approval = DispatchProxy.Create<IApprovalGate, ProtectedAccessProbe>();
        IIdempotencyStore idempotency = DispatchProxy.Create<IIdempotencyStore, ProtectedAccessProbe>();
        using WebApplicationFactory<Program> factory = AuthorityFactory(clock, owner, services =>
        {
            services.AddSingleton(risk);
            services.AddSingleton(approval);
            services.AddSingleton(idempotency);
        });
        using IServiceScope scope = factory.Services.CreateScope();
        ChatBotCommandAdmissionDecision decision = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>().AdmitAsync(
            new(TrustedAuthorityFixture.Principal(), new CommandSubmissionRequest { CommandId = Note, CommandType = operation, Command = payload }, "correlation-alpha", null), TestContext.Current.CancellationToken).ConfigureAwait(false);
        decision.IsAccepted.ShouldBeFalse();
        owner.Requests.ShouldContain(request => request.Owner == deniedOwner && request.ResourceId == deniedResource && request.RequireCurrent);
        ((ProtectedAccessProbe)(object)risk).Calls.ShouldBe(0);
        ((ProtectedAccessProbe)(object)approval).Calls.ShouldBe(0);
        ((ProtectedAccessProbe)(object)idempotency).Calls.ShouldBe(0);
        factory.Services.GetRequiredService<InMemoryAuditWriter>().Envelopes.ShouldBeEmpty();
    }

    private static WebApplicationFactory<Program> AuthorityFactory(TrustedAuthorityClock clock, SyntheticOwnerAuthorityProvider owner, Action<IServiceCollection>? configure = null)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            configure?.Invoke(services);
        }));

    private static QueryEnvelope Envelope() => new("tenant-alpha", "chatbot", Note, ChatBotReadQueryTypes.GovernedOperation, JsonSerializer.SerializeToUtf8Bytes(new GovernedOperationQuery(Note, null)), "correlation-alpha", "actor-alpha");
}
