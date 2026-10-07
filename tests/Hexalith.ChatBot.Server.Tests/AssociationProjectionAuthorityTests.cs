using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Verifies the local application-channel credential before real HTTP correction projection.</summary>
public sealed class AssociationProjectionAuthorityTests
{
    /// <summary>Only a single matching configured sidecar credential reaches projection and invalidation.</summary>
    [Theory]
    [InlineData("valid")]
    [InlineData("missing")]
    [InlineData("blank")]
    [InlineData("wrong")]
    [InlineData("duplicate")]
    [InlineData("unconfigured")]
    [InlineData("blank-configuration")]
    public async Task CorrectionEventRequiresTrustedApplicationChannel(string credential)
    {
        InMemoryAssociationProjectionStore associations = new();
        InMemoryProjectConversationProjectionStore conversations = new();
        AiActionProposalRecord proposal = new("proposal-alpha", "task-intent", "source-message", "source-item", "requester", "reviewer", [], "Project.AppendConversationMessage", "append-conversation-message",
            ["project:project-alpha"], [], "policy-alpha", 1, "correlation-alpha", "metadata_only", "collaboration_input", "v1", "review-ai-action",
            AssociationId: "association-alpha", EvidenceSnapshotSourceVersion: 1) { StateOwnerAggregateId = "conversation-alpha" };
        await conversations.UpsertAiActionProposalAsync("tenant-alpha", proposal, TestContext.Current.CancellationToken);
        TrustedAuthorityRecordingEventStoreClient sdk = new();
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["APP_API_TOKEN"] = credential == "unconfigured" ? null : credential == "blank-configuration" ? " " : "sidecar-channel-token",
                }).Build());
                services.AddSingleton<IAssociationProjectionStore>(associations);
                services.AddSingleton<IProjectConversationProjectionStore>(conversations);
                services.AddSingleton<IEventStoreGatewayClient>(sdk);
            });
        });
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Post, AssociationProjectionEndpoints.AssociationRoute)
        {
            Content = JsonContent.Create(new
            {
                TenantId = "tenant-alpha", Domain = "chatbot", AggregateId = "association-alpha",
                EventTypeName = AssociationProjectionTranslator.CorrectionAcceptedEventType, SequenceNumber = 2,
                CorrelationId = "correlation-alpha", Timestamp = DateTimeOffset.UtcNow,
                IntakeId = "intake-alpha", SourceMailboxId = "mailbox-alpha", SourceConversationId = "conversation-alpha",
                ProjectId = "project-alpha", CorrectedProjectId = "project-alpha", PriorProjectId = "project-before",
                ThresholdPolicyVersion = "v1", DerivationKernelVersion = "v1", RedactionState = "metadata_only", RetentionClass = "collaboration_input",
                CorrectionId = "correction-alpha", WorkflowInstanceId = "workflow-alpha",
            }),
        };
        if (credential != "missing")
        {
            request.Headers.TryAddWithoutValidation("dapr-api-token", credential == "duplicate" ? ["sidecar-channel-token", "sidecar-channel-token"] : [credential == "wrong" ? "wrong-channel-token" : credential == "blank" ? " " : "sidecar-channel-token"]).ShouldBeTrue();
        }
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(credential == "valid" ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);
        AssociationCandidateView? persisted = await associations.GetAsync("tenant-alpha", "association-alpha", TestContext.Current.CancellationToken);
        if (credential == "valid")
        {
            persisted.ShouldNotBeNull().SourceVersion.ShouldBe(2);
            persisted.CorrectionId.ShouldBe("correction-alpha");
            SubmitCommandRequest submitted = sdk.Submitted.ShouldHaveSingleItem();
            submitted.CommandType.ShouldBe("MarkAiActionProposalInvalidatedByCorrection");
            Dictionary<string, string> extensions = submitted.Extensions.ShouldNotBeNull();
            CommandEnvelope envelope = new(submitted.MessageId, submitted.Tenant, submitted.Domain, submitted.AggregateId, submitted.CommandType,
                JsonSerializer.SerializeToUtf8Bytes(submitted.Payload), submitted.CorrelationId!, null, extensions["actorId"], extensions);
            factory.Services.GetRequiredService<IChatBotAdmissionMarker>().IsValid(envelope).ShouldBeTrue();
        }
        else
        {
            persisted.ShouldBeNull();
            sdk.Submitted.ShouldBeEmpty();
        }
        (await conversations.ReadAiActionProposalsForAssociationAsync("tenant-alpha", "association-alpha", 2, TestContext.Current.CancellationToken)).ShouldHaveSingleItem().ShouldBe(proposal);
    }

    /// <summary>Valid sidecar delivery still acknowledges unrelated events without projection effects.</summary>
    [Fact]
    public async Task ValidSidecarUnknownEventPreservesAcknowledgementBehavior()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["APP_API_TOKEN"] = "sidecar-channel-token" }).Build())));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("dapr-api-token", "sidecar-channel-token");
        using HttpResponseMessage response = await client.PostAsJsonAsync(AssociationProjectionEndpoints.AssociationRoute, new { Domain = "unrelated" }, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
