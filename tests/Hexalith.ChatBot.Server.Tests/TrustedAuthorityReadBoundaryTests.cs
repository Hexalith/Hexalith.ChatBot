using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using System.Reflection;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Server.Queries;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Checks retained read authority using production handlers and real backing stores.</summary>
public sealed class TrustedAuthorityReadBoundaryTests
{
    private const string Operation = "01ARZ3NDEKTSV4RRFFQ69G5FAY";

    /// <summary>An authorized protected read cannot disclose after expiry or client revocation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GovernedReadRechecksAuthorityBeforeFinalDisclosure(bool revoked)
    {
        TrustedAuthorityClock clock = new();
        ServiceClientGrantProjectionCache cache = new(clock);
        SyntheticOwnerAuthorityProvider owner = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), owner, clock, cache);
        InMemoryGovernedOperationProjectionStore real = new();
        await real.SaveAsync(new("tenant-alpha", Operation, GovernedOperationView.CurrentSchemaVersion, "synthetic", "v1", "metadata_only", "test", 1, clock.UtcNow, clock.UtcNow), TestContext.Current.CancellationToken);
        IGovernedOperationProjectionStore store = TrustedAuthorityBoundaryProxy.Create<IGovernedOperationProjectionStore>(real, _ => Lapse(clock, cache, revoked));
        GovernedOperationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, store);
        QueryResult result = await handler.ExecuteAsync(Query(ChatBotReadQueryTypes.GovernedOperation, new { NoteId = Operation }), TestContext.Current.CancellationToken);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        result.PayloadBytes.ShouldBeNull();
        ((TrustedAuthorityBoundaryProxy)(object)store).Calls.ShouldBe(["GetAsync"]);
        (await real.GetAsync("tenant-alpha", Operation, TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    /// <summary>A lapse at either awaited task-intent read prevents later reads or disclosure.</summary>
    [Theory]
    [InlineData("GetTaskIntentAsync", false)]
    [InlineData("GetTaskIntentAsync", true)]
    [InlineData("GetAsync", false)]
    [InlineData("GetAsync", true)]
    public async Task TaskIntentReadRechecksAtEachProtectedBoundary(string boundary, bool revoked)
    {
        TrustedAuthorityClock clock = new();
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), new SyntheticOwnerAuthorityProvider(clock), clock, cache);
        InMemoryProjectConversationProjectionStore real = new();
        await real.UpsertTaskIntentAsync(new("intent-alpha", "tenant-alpha", "project-alpha", "source-message", "actor-alpha", "synthetic", default, [], "v1", 1,
            clock.UtcNow, default, "v1", "test", "synthetic", "metadata_only", "test", 1, "correlation-alpha"), TestContext.Current.CancellationToken);
        void After(string method) { if (method == boundary) { Lapse(clock, cache, revoked); } }
        IProjectConversationProjectionStore store = TrustedAuthorityBoundaryProxy.Create<IProjectConversationProjectionStore>(real, After);
        IMailboxMessageContentSource content = TrustedAuthorityBoundaryProxy.Create<IMailboxMessageContentSource>(new UnavailableMailboxMessageContentSource(), After);
        TaskIntentReviewQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, store, content);
        QueryResult result = await handler.ExecuteAsync(Query(ChatBotReadQueryTypes.TaskIntentReview, new { ProjectId = "project-alpha", TaskIntentId = "intent-alpha" }), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((TrustedAuthorityBoundaryProxy)(object)content).Calls.Count.ShouldBe(boundary == "GetTaskIntentAsync" ? 0 : 1);
    }

    /// <summary>Conversation reads stop before later package reads and refuse disclosure after assembly.</summary>
    [Theory]
    [InlineData("ReadPageAsync", false)]
    [InlineData("ReadPageAsync", true)]
    [InlineData("ReadAiContextPackageItemsAsync", false)]
    [InlineData("ReadAiContextPackageItemsAsync", true)]
    [InlineData("AssembleAsync", false)]
    [InlineData("AssembleAsync", true)]
    public async Task ConversationReadRechecksEachAwaitedBoundary(string boundary, bool revoked)
    {
        TrustedAuthorityClock clock = new();
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), new SyntheticOwnerAuthorityProvider(clock), clock, cache);
        void After(string method) { if (method == boundary) { Lapse(clock, cache, revoked); } }
        IProjectConversationProjectionStore store = TrustedAuthorityBoundaryProxy.Create<IProjectConversationProjectionStore>(new InMemoryProjectConversationProjectionStore(), After);
        IProjectAiContextPackageAssembler assembler = TrustedAuthorityBoundaryProxy.Create<IProjectAiContextPackageAssembler>(new DefaultProjectAiContextPackageAssembler(), After);
        using WebApplicationFactory<Program> factory = new();
        IQueryCursorCodec codec = factory.Services.GetRequiredService<IQueryCursorCodec>();
        ProjectConversationQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, store, assembler, codec);
        QueryResult result = await handler.ExecuteAsync(Query(ChatBotReadQueryTypes.ProjectConversation, new { ProjectId = "project-alpha", PageSize = 10 }), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((TrustedAuthorityBoundaryProxy)(object)store).Calls.Count.ShouldBe(boundary == "ReadPageAsync" ? 1 : 2);
        ((TrustedAuthorityBoundaryProxy)(object)assembler).Calls.Count.ShouldBe(boundary == "AssembleAsync" ? 1 : 0);
    }

    /// <summary>Audit history must not read further or disclose once status/history authority lapses.</summary>
    [Theory]
    [InlineData("TryGetAsync", false)]
    [InlineData("TryGetAsync", true)]
    [InlineData("GetPostCommitEnvelopes", false)]
    [InlineData("GetPostCommitEnvelopes", true)]
    public async Task OperationAuditReadRechecksBeforeHistoryAndDisclosure(string boundary, bool revoked)
    {
        TrustedAuthorityClock clock = new();
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), new SyntheticOwnerAuthorityProvider(clock), clock, cache);
        InMemoryOperationStatusStore real = new();
        await real.UpsertAsync(new("tenant-alpha", Operation, Operation, "correlation-alpha", default, 0, "completed", "committed", [], null, clock.UtcNow, clock.UtcNow), TestContext.Current.CancellationToken);
        void After(string method) { if (method == boundary) { Lapse(clock, cache, revoked); } }
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(real, After);
        IAuditHistoryReader history = TrustedAuthorityBoundaryProxy.Create<IAuditHistoryReader>(new InMemoryAuditWriter(), After);
        OperationAuditHistoryQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, status, history);
        QueryResult result = await handler.ExecuteAsync(Query(ChatBotReadQueryTypes.OperationAuditHistory, new { OperationId = Operation }), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((TrustedAuthorityBoundaryProxy)(object)history).Calls.Count.ShouldBe(boundary == "TryGetAsync" ? 0 : 1);
    }

    /// <summary>Status expiry prevents workflow reads and final status disclosure without altering the stored record.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OperationStatusRechecksBeforeWorkflowAndDisclosure(bool hasWorkflow, bool revoked)
    {
        TrustedAuthorityClock clock = new();
        ServiceClientGrantProjectionCache cache = new(clock);
        ChatBotRequestAuthorizer authorizer = new(new(), new SyntheticOwnerAuthorityProvider(clock), clock, cache);
        InMemoryOperationStatusStore real = new();
        OperationStatusRecord original = new("tenant-alpha", Operation, Operation, "correlation-alpha", default, 0, "completed", "committed", [], null, clock.UtcNow, clock.UtcNow,
            NextRetryAt: hasWorkflow ? clock.UtcNow.AddSeconds(10) : null, WorkflowInstanceId: hasWorkflow ? "workflow-alpha" : null);
        await real.UpsertAsync(original, TestContext.Current.CancellationToken);
        IOperationStatusStore status = TrustedAuthorityBoundaryProxy.Create<IOperationStatusStore>(real, _ => Lapse(clock, cache, revoked));
        ICorrectionPropagationWorkflowRuntime runtime = DispatchProxy.Create<ICorrectionPropagationWorkflowRuntime, ProtectedAccessProbe>();
        OperationStatusQueryHandler handler = new(TrustedAuthorityFixture.Resolver(TrustedAuthorityFixture.Principal(actorClass: "service")), authorizer, status, clock, runtime);
        QueryResult result = await handler.ExecuteAsync(Query(ChatBotReadQueryTypes.OperationStatus, new { OperationId = Operation }), TestContext.Current.CancellationToken);
        result.ErrorMessage.ShouldBe(ChatBotAuthorizationReasonCodes.SafeNotFound);
        ((ProtectedAccessProbe)(object)runtime).Calls.ShouldBe(0);
        (await real.TryGetAsync("tenant-alpha", Operation, TestContext.Current.CancellationToken)).ShouldBe(original);
    }

    private static void Lapse(TrustedAuthorityClock clock, ServiceClientGrantProjectionCache cache, bool revoked)
    {
        if (revoked) { cache.InvalidateRevocation("tenant-alpha", "client-alpha", "api", ""); }
        else { clock.UtcNow += TimeSpan.FromMinutes(6); }
    }

    private static QueryEnvelope Query(string type, object payload)
        => new("tenant-alpha", "chatbot", Operation, type, JsonSerializer.SerializeToUtf8Bytes(payload), "correlation-alpha", "actor-alpha");
}
