using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Dapr.Workflow;

using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;
using Hexalith.Memories.Client.Rest;

namespace Hexalith.ChatBot.Server.Adapters.Memories;

/// <summary>Fetches one authorized source payload and starts or rejoins its deterministic Memories ingestion.</summary>
internal sealed class IngestionBindingStartSourceActivity(
    IMailboxMessageContentSource messageContentSource,
    IMailboxAttachmentContentSource attachmentContentSource,
    MemoriesClient memories)
    : WorkflowActivity<IngestionBindingSourceRequest, IngestionBindingSourceOperation>
{
    public override async Task<IngestionBindingSourceOperation> RunAsync(
        WorkflowActivityContext context,
        IngestionBindingSourceRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        (byte[] Content, string ContentType) payload = input.RecordKind switch
        {
            IngestionBindingRecordKind.Message => await GetMessageAsync(input).ConfigureAwait(false),
            IngestionBindingRecordKind.Attachment => await GetAttachmentAsync(input).ConfigureAwait(false),
            _ => throw new InvalidOperationException("ingestion_binding_record_kind_invalid"),
        };

        string identity = IdentityFor(input);
        string instanceId = await memories
            .IngestAsync(
                input.Request.TenantId,
                input.Context.PriorCaseId,
                SourceUriFor(input),
                payload.Content,
                payload.ContentType,
                "hexalith-chatbot",
                metadata: null,
                idempotencyToken: identity,
                CancellationToken.None)
            .ConfigureAwait(false);
        return new IngestionBindingSourceOperation(input, instanceId);
    }

    private async Task<(byte[] Content, string ContentType)> GetMessageAsync(IngestionBindingSourceRequest input)
    {
        MailboxMessageContentResult result = await messageContentSource
            .GetAsync(
                input.Request.TenantId,
                input.Request.AssociatedProjectId,
                input.Context.Source.ProviderMessageId,
                CancellationToken.None)
            .ConfigureAwait(false);
        if (!result.Available || string.IsNullOrEmpty(result.Content) || string.IsNullOrWhiteSpace(result.ContentType))
        {
            throw new InvalidOperationException("ingestion_binding_message_unavailable");
        }

        return (Encoding.UTF8.GetBytes(result.Content), result.ContentType);
    }

    private async Task<(byte[] Content, string ContentType)> GetAttachmentAsync(IngestionBindingSourceRequest input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ProviderAttachmentId);
        MailboxAttachmentContentResult result = await attachmentContentSource
            .FetchAttachmentContentAsync(
                new MailboxAttachmentContentRequest(
                    input.Request.TenantId,
                    input.Request.AssociatedProjectId,
                    input.Request.AssociationId,
                    input.Request.IntakeId,
                    input.Context.Source.SourceMailboxId,
                    input.Context.Source.ProviderMessageId,
                    input.ProviderAttachmentId,
                    input.Ordinal - 1,
                    input.Request.SourceVersion,
                    input.Request.CorrelationId),
                CancellationToken.None)
            .ConfigureAwait(false);
        if (result.Kind is not MailboxAttachmentContentResultKind.Available || result.Content.IsEmpty)
        {
            throw new InvalidOperationException("ingestion_binding_attachment_unavailable");
        }

        string contentType = string.IsNullOrWhiteSpace(result.MediaType)
            ? input.ContentType ?? "application/octet-stream"
            : result.MediaType;
        return (result.Content.ToArray(), contentType);
    }

    private static string IdentityFor(IngestionBindingSourceRequest input)
    {
        string value = string.Join(
            '|',
            input.Request.TenantId,
            input.Request.IntakeId,
            input.RecordKind.ToString(),
            input.Ordinal.ToString(CultureInfo.InvariantCulture));
        return $"chatbot-ingest-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant()}";
    }

    private static string SourceUriFor(IngestionBindingSourceRequest input)
        => $"chatbot://intakes/{Uri.EscapeDataString(input.Request.IntakeId)}/{input.RecordKind.ToString().ToLowerInvariant()}/{input.Ordinal.ToString(CultureInfo.InvariantCulture)}";
}
