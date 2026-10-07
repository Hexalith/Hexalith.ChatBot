using System.Net;
using System.Net.Http.Json;
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

/// <summary>Exercises the approved re-review corrections at production authorization boundaries.</summary>
public sealed class TrustedAuthorityReReviewTests
{
    private const string Note = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    /// <summary>A different canonical stream target needs its own exact current owner grant.</summary>
    [Theory]
    [InlineData(nameof(ProposeAIAction), "StateOwnerAggregateId")]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), "StateOwnerAggregateId")]
    [InlineData(nameof(DecideAiActionApproval), "StateOwnerAggregateId")]
    [InlineData(nameof(ExecuteApprovedAIAction), "StateOwnerAggregateId")]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "StateOwnerAggregateId")]
    [InlineData(nameof(RequestOutboundSendApproval), "DraftId")]
    [InlineData(nameof(DecideOutboundApproval), "DraftId")]
    [InlineData(nameof(ExecuteApprovedOutboundDraft), "DraftId")]
    [InlineData(nameof(CancelAiResponseGeneration), "ConversationId")]
    public async Task CanonicalDispatchTargetRequiresItsOwnExactCurrentOwnerEvidence(string operation, string targetProperty)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.ResourceId != "foreign-target" };
        Dictionary<string, object?> payload = new() { ["ProjectId"] = "project-alpha", ["DraftId"] = "draft-alpha", ["ApprovalId"] = "approval-alpha", ["SendId"] = "send-alpha", [targetProperty] = "foreign-target" };
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        ChatBotAuthorityDecision denied = await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), operation, false, payload, TestContext.Current.CancellationToken);
        denied.IsAllowed.ShouldBeFalse();
        denied.EvidenceReferences.ShouldBeEmpty();
        owner.Requests.ShouldContain(request => request.Owner == "ChatBot" && request.ResourceId == "foreign-target" && request.Authority == "operation" && request.RequireCurrent && request.Operation == operation);
        owner.Allows = static _ => true;
        (await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), operation, false, payload, TestContext.Current.CancellationToken)).IsAllowed.ShouldBeTrue();
    }

    /// <summary>Project-prefixed affected references require exact Projects grants in addition to the source grant.</summary>
    [Theory]
    [InlineData(nameof(ProposeAIAction))]
    [InlineData(nameof(ExecuteApprovedAIAction))]
    public async Task AffectedProjectReferenceRequiresItsOwnOwnerEvidence(string operation)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = request => request.Owner != "Projects" || request.ResourceId != "project-forbidden" };
        ChatBotRequestAuthorizer authorizer = TrustedAuthorityFixture.Authorizer(clock, owner);
        object payload = new { ProjectId = "project-source", StateOwnerAggregateId = "conversation-alpha", AffectedResourceReferences = new[] { "project:project-forbidden", "task:typed-reference", "folder:typed-reference" } };
        ChatBotAuthorityDecision denied = await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), operation, false, payload, TestContext.Current.CancellationToken);
        denied.IsAllowed.ShouldBeFalse();
        owner.Requests.ShouldContain(request => request.Owner == "Projects" && request.ResourceId == "project-source");
        owner.Requests.ShouldContain(request => request.Owner == "Projects" && request.ResourceId == "project-forbidden" && request.RequireCurrent && request.Operation == operation);
        owner.Requests.ShouldNotContain(request => request.ResourceId.Contains("typed-reference", StringComparison.Ordinal));
        owner.Allows = static _ => true;
        ChatBotAuthorityDecision allowed = await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), operation, false, payload, TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
        allowed.EvidenceReferences.ShouldContain("Projects:synthetic-evidence:v1");
    }

    /// <summary>Machines must prove requester Parties identity for identity-bearing operations.</summary>
    [Theory]
    [InlineData("service", nameof(ResolveMailboxMessageParticipants))]
    [InlineData("ai", nameof(ResolveMailboxMessageParticipants))]
    [InlineData("service", nameof(CaptureTaskIntent))]
    [InlineData("ai", nameof(CaptureTaskIntent))]
    public async Task PartiesRequiredMachineOperationCannotSkipRequesterIdentity(string actorClass, string operation)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static request => request.Owner != "Parties" || request.ResourceId != "actor-alpha" };
        ChatBotAuthorityDecision result = await TrustedAuthorityFixture.Authorizer(clock, owner).AuthorizeAsync(TrustedAuthorityFixture.Context(actorClass: actorClass), operation, false,
            new { ResolutionId = Note, ProjectId = "project-alpha" }, TestContext.Current.CancellationToken);
        result.IsAllowed.ShouldBeFalse();
        owner.Requests.ShouldContain(request => request.Owner == "Parties" && request.Authority == "identity" && request.ResourceId == "actor-alpha" && request.ActorClass == actorClass && request.RequireCurrent);
    }

    /// <summary>A grant for another tenant cannot authorize reads or command admission.</summary>
    [Theory]
    [InlineData("service")]
    [InlineData("ai")]
    public async Task ForeignTenantOwnerGrantDeniesReadAndProductionAdmission(string actorClass)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = static evidence => evidence.ServiceGrant is { } grant ? evidence with { ServiceGrant = grant with { TenantId = "tenant-beta" } } : evidence };
        CountingGovernedOperationStore store = new();
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: actorClass)), TrustedAuthorityFixture.Authorizer(clock, owner), store);
        QueryEnvelope query = new("tenant-alpha", "chatbot", Note, ChatBotReadQueryTypes.GovernedOperation, JsonSerializer.SerializeToUtf8Bytes(new { NoteId = Note }), "correlation-alpha", "actor-alpha");
        (await handler.ExecuteAsync(query, TestContext.Current.CancellationToken)).ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        store.Reads.ShouldBe(0);
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        InMemoryAuditWriter audit = new();
        using WebApplicationFactory<Program> factory = Factory(clock, owner, idempotency, audit);
        using IServiceScope scope = factory.Services.CreateScope();
        ChatBotCommandAdmissionDecision admitted = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>()
            .AdmitAsync(Submission(actorClass), TestContext.Current.CancellationToken);
        admitted.IsAccepted.ShouldBeFalse();
        admitted.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch);
        idempotency.RecordCount.ShouldBe(0);
        audit.Envelopes.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ServiceClientGrantTenantMismatch);
    }

    /// <summary>Expiry during admission storage or SDK cleanup denies before aggregate effects.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiryInsideDurableAdmissionOrSdkAbortRejectsBeforeProtectedEffect(bool sdkAbort)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => evidence with { ExpiresAt = clock.UtcNow.AddSeconds(1) } };
        InMemoryCoarseIdempotencyStore real = new(clock);
        TrustedAuthorityAdvancingIdempotencyStore store = new(real, step => { if (step == (sdkAbort ? "abort" : "admission")) { clock.UtcNow += TimeSpan.FromSeconds(2); } });
        InMemoryAuditWriter audit = new();
        InMemoryAuthorizationFailureCounter counter = new(clock);
        using WebApplicationFactory<Program> factory = Factory(clock, owner, store, audit).WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<IAuthorizationFailureCounter>(counter);
            if (sdkAbort) { services.AddSingleton<IStartupFilter>(new TrustedAuthorityPrincipalStartupFilter(Relay)); }
        }));
        if (sdkAbort)
        {
            using HttpClient client = factory.CreateClient();
            CommandEnvelope command = new(Note, "tenant-alpha", "chatbot", Note, nameof(RecordGovernedNote), JsonSerializer.SerializeToUtf8Bytes(new RecordGovernedNote(Note)),
                "correlation-alpha", null, "service-account-client-alpha", new() { ["actorType"] = "service", ["serviceClientId"] = "client-alpha" });
            using HttpResponseMessage response = await client.PostAsJsonAsync("/process", new DomainServiceRequest(command, CurrentState: null), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            DomainServiceWireResult result = (await response.Content.ReadFromJsonAsync<DomainServiceWireResult>(cancellationToken: TestContext.Current.CancellationToken))!;
            result.IsRejection.ShouldBeTrue();
            result.Events.ShouldNotContain(static item => item.EventTypeName == typeof(Hexalith.ChatBot.Server.Operations.GovernedNoteRecorded).FullName);
        }
        else
        {
            using IServiceScope scope = factory.Services.CreateScope();
            ChatBotCommandAdmissionDecision result = await scope.ServiceProvider.GetRequiredService<ChatBotCommandAdmissionPipeline>().AdmitAsync(Submission("human"), TestContext.Current.CancellationToken);
            result.IsAccepted.ShouldBeFalse();
            result.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
            audit.Envelopes.ShouldBeEmpty();
        }
        store.Boundaries.ShouldContain("admission");
        store.Boundaries.ShouldContain("abort");
        owner.Requests.ShouldNotBeEmpty();
        real.Records.ShouldBeEmpty();
        audit.AuthorizationFailures.ShouldHaveSingleItem().ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        counter.ReadAndReset().ShouldHaveSingleItem().FailureCount.ShouldBe(1);
    }

    /// <summary>Every closed owner rejection reason is logged without protected values.</summary>
    [Theory]
    [InlineData("absent")]
    [InlineData("request-mismatch")]
    [InlineData("not-allowed")]
    [InlineData("revoked")]
    [InlineData("invalid-evidence-reference")]
    [InlineData("invalid-version-reference")]
    [InlineData("non-utc-timestamp")]
    [InlineData("future-observation")]
    [InlineData("invalid-revocation-order")]
    [InlineData("observation-expired")]
    [InlineData("revocation-expired")]
    [InlineData("evidence-expired")]
    [InlineData("not-current")]
    public async Task AbsentAndRejectedOwnerEvidenceProducesMetadataOnlyReasonDiagnostic(string reason)
    {
        TrustedAuthorityClock clock = new();
        const string restricted = "owner-private@example.test";
        SyntheticOwnerAuthorityProvider owner = new(clock) { Transform = evidence => reason switch
        {
            "request-mismatch" => evidence with { Request = evidence.Request with { ResourceId = restricted } },
            "not-allowed" => evidence with { IsAllowed = false },
            "revoked" => evidence with { IsRevoked = true },
            "invalid-evidence-reference" => evidence with { EvidenceId = restricted },
            "invalid-version-reference" => evidence with { Version = restricted },
            "non-utc-timestamp" => evidence with { ObservedAt = evidence.ObservedAt.ToOffset(TimeSpan.FromHours(1)) },
            "future-observation" => evidence with { ObservedAt = clock.UtcNow.AddSeconds(1) },
            "invalid-revocation-order" => evidence with { RevocationCheckedAt = clock.UtcNow.AddSeconds(-1) },
            "observation-expired" => evidence with { ObservedAt = clock.UtcNow.AddMinutes(-5) },
            "revocation-expired" => evidence with { ObservedAt = clock.UtcNow.AddMinutes(-2), RevocationCheckedAt = clock.UtcNow.AddSeconds(-60) },
            "evidence-expired" => evidence with { ExpiresAt = clock.UtcNow },
            "not-current" => evidence with { ObservedAt = clock.UtcNow.AddSeconds(-1), RevocationCheckedAt = clock.UtcNow.AddSeconds(-1) },
            _ => evidence,
        } };
        TrustedAuthorityCaptureLogger logger = new();
        ChatBotRequestAuthorizer authorizer = new(new(), reason == "absent" ? new UnavailableChatBotOwnerAuthorityProvider() : owner, clock, new ServiceClientGrantProjectionCache(clock), logger);
        (await authorizer.AuthorizeAsync(TrustedAuthorityFixture.Context(), nameof(RecordGovernedNote), false, new RecordGovernedNote(Note), TestContext.Current.CancellationToken)).IsAllowed.ShouldBeFalse();
        string logged = logger.Messages.ShouldHaveSingleItem();
        logged.ShouldBe($"Owner ChatBot evidence rejected for RecordGovernedNote: {reason}");
        logged.ShouldNotContain(restricted);
        logged.ShouldNotContain(Note);
        logged.ShouldNotContain("actor-alpha");
        logged.ShouldNotContain("tenant-alpha");
    }

    private static WebApplicationFactory<Program> Factory(TrustedAuthorityClock clock, SyntheticOwnerAuthorityProvider owner, IIdempotencyStore store, IAuditWriter audit)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton<ISystemClock>(clock);
            services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner);
            services.AddSingleton<IIdempotencyStore>(store);
            services.AddSingleton<IAuditWriter>(audit);
        }));

    private static ChatBotCommandSubmission Submission(string actorClass) => new(TrustedAuthorityFixture.Principal(actorClass: actorClass), new Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest
    {
        CommandId = Note, CommandType = nameof(RecordGovernedNote), Command = new RecordGovernedNote(Note), RequestSchemaVersion = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequestRequestSchemaVersion.V1,
    }, "correlation-alpha", null, ChatBotSurfaceOrigin.Api);

    private static ClaimsPrincipal Relay(string operation) => new(new ClaimsIdentity([
        new(ClaimTypes.NameIdentifier, "workload:eventstore"), new("eventstore:workload", "eventstore"),
        new("eventstore:operation", operation)], "EventStoreWorkload"));
}
