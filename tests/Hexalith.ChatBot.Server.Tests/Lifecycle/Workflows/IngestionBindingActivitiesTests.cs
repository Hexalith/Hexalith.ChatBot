using System.Text;

using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Adapters.Memories;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.ChatBot.Server.Projections;
using Hexalith.Memories.Client.Rest;
using Hexalith.Memories.Contracts.V1;
using Hexalith.Memories.Contracts.V1.DerivedStores;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle.Workflows;

public sealed class IngestionBindingActivitiesTests
{
    [Fact]
    public async Task StartSource_MessageUnavailable_DoesNotScheduleMemoriesIngestion()
    {
        RecordingMemoriesClient memories = new();
        IngestionBindingStartSourceActivity activity = new(
            new FixedMessageSource(new MailboxMessageContentResult(false, "message_not_available")),
            new FixedAttachmentSource(MailboxAttachmentContentResult.Unavailable("attachment_not_available")),
            memories);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            activity.RunAsync(null!, Source(IngestionBindingRecordKind.Message, 0, providerAttachmentId: null)));

        exception.Message.ShouldBe("ingestion_binding_message_unavailable");
        memories.Ingestions.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartSource_AttachmentUnauthorized_DoesNotScheduleMemoriesIngestion()
    {
        RecordingMemoriesClient memories = new();
        IngestionBindingStartSourceActivity activity = new(
            new FixedMessageSource(new MailboxMessageContentResult(true, "available", "body", "text/plain")),
            new FixedAttachmentSource(MailboxAttachmentContentResult.Unauthorized()),
            memories);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            activity.RunAsync(null!, Source(IngestionBindingRecordKind.Attachment, 1, "attachment-1")));

        exception.Message.ShouldBe("ingestion_binding_attachment_unavailable");
        memories.Ingestions.ShouldBeEmpty();
    }

    [Fact]
    public async Task StartSource_AvailableAttachment_UsesProviderZeroBasedOrdinalAndStableSourceIdentity()
    {
        RecordingMemoriesClient memories = new();
        FixedAttachmentSource attachments = new(MailboxAttachmentContentResult.Available("attachment bytes"u8.ToArray(), "application/pdf"));
        IngestionBindingStartSourceActivity activity = new(
            new FixedMessageSource(new MailboxMessageContentResult(false, "unused")),
            attachments,
            memories);
        IngestionBindingSourceRequest source = Source(IngestionBindingRecordKind.Attachment, 1, "attachment-1");

        IngestionBindingSourceOperation first = await activity.RunAsync(null!, source);
        IngestionBindingSourceOperation second = await activity.RunAsync(null!, source);

        first.InstanceId.ShouldBe("instance-1");
        second.InstanceId.ShouldBe("instance-2");
        attachments.Requests.ShouldAllBe(static request => request.Ordinal == 0);
        memories.Ingestions.Count.ShouldBe(2);
        memories.Ingestions.Select(static ingestion => ingestion.SourceUri).Distinct().ShouldHaveSingleItem();
        memories.Ingestions.Select(static ingestion => ingestion.IdempotencyToken).Distinct().ShouldHaveSingleItem();
        memories.Ingestions.ShouldAllBe(static ingestion =>
            ingestion.TenantId == "tenant-a"
            && ingestion.CaseId == "case-prior"
            && ingestion.ContentType == "application/pdf"
            && Encoding.UTF8.GetString(ingestion.Content) == "attachment bytes");
    }

    [Theory]
    [InlineData("other-instance", "tenant-a", "case-prior")]
    [InlineData("instance-1", "tenant-b", "case-prior")]
    [InlineData("instance-1", "tenant-a", "case-other")]
    public async Task GetStatus_AnyAuthorityIdentityMismatch_FailsClosed(
        string returnedInstanceId,
        string returnedTenantId,
        string returnedCaseId)
    {
        RecordingMemoriesClient memories = new()
        {
            Status = new IngestionWorkflowStatus(
                returnedInstanceId,
                returnedTenantId,
                returnedCaseId,
                "Completed",
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch,
                "unit-1",
                MemoryUnitStatus.Indexed,
                null),
        };
        IngestionBindingGetStatusActivity activity = new(memories);
        IngestionBindingSourceOperation operation = new(
            Source(IngestionBindingRecordKind.Message, 0, providerAttachmentId: null),
            "instance-1");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            activity.RunAsync(null!, operation));

        exception.Message.ShouldBe("ingestion_binding_status_identity_mismatch");
    }

    private static IngestionBindingSourceRequest Source(
        IngestionBindingRecordKind kind,
        int ordinal,
        string? providerAttachmentId)
    {
        IngestionBindingRequest request = new(
            "tenant-a",
            "association-1",
            "intake-1",
            "project-1",
            7,
            "correlation-1",
            "workflow-1");
        IngestionBindingResolvedContext context = new(
            "case-prior",
            new ProjectConversationIngestionSource(
                request.TenantId,
                request.AssociatedProjectId,
                request.AssociationId,
                request.IntakeId,
                "mailbox-1",
                "message-1",
                providerAttachmentId is null ? [] : [new(providerAttachmentId, 1, "application/pdf")],
                request.SourceVersion,
                request.CorrelationId));
        return new IngestionBindingSourceRequest(request, context, kind, ordinal, providerAttachmentId, "application/pdf");
    }

    private sealed class FixedMessageSource(MailboxMessageContentResult result) : IMailboxMessageContentSource
    {
        public Task<MailboxMessageContentResult> GetAsync(
            string tenantId,
            string projectId,
            string sourceMessageId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class FixedAttachmentSource(MailboxAttachmentContentResult result) : IMailboxAttachmentContentSource
    {
        public List<MailboxAttachmentContentRequest> Requests { get; } = [];

        public ValueTask<MailboxAttachmentContentResult> FetchAttachmentContentAsync(
            MailboxAttachmentContentRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingMemoriesClient : MemoriesClient
    {
        public RecordingMemoriesClient()
            : base(
                new HttpClient { BaseAddress = new Uri("http://memories/") },
                Options.Create(new MemoriesClientOptions { Endpoint = new Uri("http://memories/") }),
                NullLogger<MemoriesClient>.Instance)
        {
        }

        public List<RecordedIngestion> Ingestions { get; } = [];

        public IngestionWorkflowStatus? Status { get; init; }

        public override Task<string> IngestAsync(
            string tenantId,
            string caseId,
            string sourceUri,
            byte[] content,
            string contentType,
            string ingestedBy,
            IReadOnlyDictionary<string, MetadataField>? metadata,
            string? idempotencyToken,
            CancellationToken ct)
        {
            Ingestions.Add(new RecordedIngestion(tenantId, caseId, sourceUri, content, contentType, idempotencyToken));
            return Task.FromResult($"instance-{Ingestions.Count}");
        }

        public override Task<IngestionWorkflowStatus> GetIngestionWorkflowStatusAsync(
            string instanceId,
            CancellationToken ct)
            => Task.FromResult(Status ?? throw new InvalidOperationException("status_not_configured"));
    }

    private sealed record RecordedIngestion(
        string TenantId,
        string CaseId,
        string SourceUri,
        byte[] Content,
        string ContentType,
        string? IdempotencyToken);
}
