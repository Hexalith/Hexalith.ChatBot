using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Association;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.ChatBot.Tests.TrustedAuthority;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Verifies that only exact trusted server-produced envelopes use the admission-marker seam.</summary>
public sealed class TrustedAuthorityInternalProducerTests
{
    /// <summary>Both internal producers mint markers that SDK admission accepts and binds to the exact command.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InternalWorkflowProducerMarkerAdmitsOnlyItsExactEnvelope(bool invalidation)
    {
        TrustedAuthorityClock clock = new();
        SyntheticOwnerAuthorityProvider owner = new(clock) { Allows = static _ => false };
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureServices(services => services.AddSingleton<IChatBotOwnerAuthorityProvider>(owner)));
        using IServiceScope scope = factory.Services.CreateScope();
        IChatBotAdmissionMarker marker = scope.ServiceProvider.GetRequiredService<IChatBotAdmissionMarker>();
        IDomainServiceAdmissionStage stage = scope.ServiceProvider.GetServices<IDomainServiceAdmissionStage>().Single(static stage => stage.Name == "chatbot-command-gateway");
        TrustedAuthorityRecordingEventStoreClient sdk = new();
        if (invalidation)
        {
            InMemoryProjectConversationProjectionStore projection = new();
            AiActionProposalRecord proposal = new("proposal-alpha", "task-intent", "source-message", "source-item", "requester", "reviewer", [], "Project.AppendConversationMessage", "append-conversation-message",
                ["project:project-alpha"], [], "policy-alpha", 1, "correlation-alpha", "metadata_only", "collaboration_input", "v1", "review-ai-action",
                AssociationId: "association-alpha", EvidenceSnapshotSourceVersion: 1) { StateOwnerAggregateId = "conversation-alpha" };
            await projection.UpsertAiActionProposalAsync("tenant-alpha", proposal, TestContext.Current.CancellationToken);
            AssociationCandidateView corrected = new("tenant-alpha", "association-alpha", "intake-alpha", "mailbox-alpha", "conversation-alpha", null, "project-alpha", null,
                LifecycleState.Proposed, AssociationScoringOutcome.CandidatesGenerated, AssociationThresholdBand.Ambiguous, 0.8, [], [], "v1", "v1", "synthetic", "v1", "metadata_only", "collaboration_input", 2,
                "correlation-alpha", clock.UtcNow, clock.UtcNow, CorrectionId: "correction-alpha", WorkflowInstanceId: "workflow-alpha");
            await new AiActionProposalInvalidationCoordinator(projection, sdk, marker).InvalidateAsync(corrected, TestContext.Current.CancellationToken);
        }
        else
        {
            CorrectionPropagationRequest request = new("tenant-alpha", "actor-alpha", "association-alpha", "intake-alpha", "correction-alpha", "workflow-alpha", "project-before", "project-after", 2,
                "correlation-alpha", clock.UtcNow, clock.UtcNow.AddMinutes(1));
            StartMailboxAssociationCorrectionPropagation command = new(request.AssociationId, request.IntakeId, request.CorrectionId, request.WorkflowInstanceId, request.PriorProjectId, request.CorrectedProjectId,
                [], request.SourceVersion, clock.UtcNow, clock.UtcNow.AddMinutes(1), "owner", "none", "v1");
            await new EventStoreCorrectionPropagationCommandWriter(sdk, marker).SubmitAsync(request, nameof(StartMailboxAssociationCorrectionPropagation), command, TestContext.Current.CancellationToken);
        }
        SubmitCommandRequest submitted = sdk.Submitted.ShouldHaveSingleItem();
        Dictionary<string, string> extensions = submitted.Extensions.ShouldNotBeNull();
        extensions.ShouldContainKey(DataProtectionChatBotAdmissionMarker.ExtensionKey);
        extensions["actorType"].ShouldBe("system");
        CommandEnvelope envelope = new(submitted.MessageId, submitted.Tenant, submitted.Domain, submitted.AggregateId, submitted.CommandType, JsonSerializer.SerializeToUtf8Bytes(submitted.Payload),
            submitted.CorrelationId!, null, extensions["actorId"], extensions);
        marker.IsValid(envelope).ShouldBeTrue();
        (await stage.EvaluateAsync(new DomainServiceAdmissionContext(new DomainServiceRequest(envelope, null)), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        owner.Requests.ShouldBeEmpty();
        foreach (CommandEnvelope changed in new[]
        {
            envelope with { TenantId = "tenant-foreign" }, envelope with { AggregateId = "aggregate-foreign" },
            envelope with { Payload = JsonSerializer.SerializeToUtf8Bytes(new { ProjectId = "project-foreign" }) },
            envelope with { Extensions = new Dictionary<string, string>(envelope.Extensions!) { ["actorId"] = "actor-foreign" } },
        })
        {
            marker.IsValid(changed).ShouldBeFalse();
            (await stage.EvaluateAsync(new DomainServiceAdmissionContext(new DomainServiceRequest(changed, null)), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeFalse();
        }
    }
}
