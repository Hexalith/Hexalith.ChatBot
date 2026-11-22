using System.Text.Json;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Governance.Policy;

/// <summary>Reads both approvals and activation from the current tenant's single disclosure policy stream.</summary>
internal sealed class ReviewQueueDisclosurePolicy(IAuthoritativeEventStreamReader reader)
{
    internal const string StreamId = "review-queue-disclosure-policy";

    public async Task<(int Minimum, string Snapshot)?> ReadAsync(string tenant, Audit.ISystemClock clock, CancellationToken token)
    {
        try
        {
            var result = await reader.ReadAsync(new AggregateIdentity(tenant, "chatbot", StreamId), token).ConfigureAwait(false);
            return result.IsAuthoritative ? FromStream(tenant, result.Stream, clock.UtcNow) : null;
        }
        catch (Exception ex) when (ex is IOException or HttpRequestException or Dapr.DaprException or Grpc.Core.RpcException)
        { return null; }
    }

    internal static (int Minimum, string Snapshot)? FromStream(string tenant, AuthoritativeEventStream? stream, DateTimeOffset now)
    {
        if (stream is null || stream.Identity.TenantId != tenant || stream.Identity.Domain != "chatbot" ||
            stream.Identity.AggregateId != StreamId || string.IsNullOrWhiteSpace(stream.ObservationId) || stream.Head < 1 || stream.Events.Count != stream.Head ||
            stream.ObservedAt > now || now - stream.ObservedAt >= TimeSpan.FromMinutes(1)) { return null; }

        TenantPolicyChangePendingApproval? pending = null;
        (int Minimum, string Snapshot)? current = null;
        long sequence = 0;
        long activeVersion = 0;
        try
        {
            foreach (var item in stream.Events)
            {
                if (item.SequenceNumber != ++sequence || item.SerializationFormat != "json") { return null; }
                var type = item.EventTypeName.Split(',', 2)[0];
                if (type == typeof(TenantPolicyChangePendingApproval).FullName)
                {
                    pending = JsonSerializer.Deserialize<TenantPolicyChangePendingApproval>(item.Payload, JsonSerializerOptions.Web);
                    if (pending is null || pending.TenantId != tenant || pending.PolicyChangeId != StreamId ||
                        pending.RequesterActorId != item.UserId)
                    { return null; }
                }
                else if (type == typeof(TenantPolicySnapshotActivated).FullName)
                {
                    var active = JsonSerializer.Deserialize<TenantPolicySnapshotActivated>(item.Payload, JsonSerializerOptions.Web);
                    if (active is null || pending is null || active.TenantId != tenant || active.PolicyChangeId != StreamId ||
                        current is { } prior && (prior.Snapshot != pending.SourcePolicySnapshotId || pending.SourceVersion != activeVersion + 1) ||
                        active.SupersededPolicySnapshotId != pending.SourcePolicySnapshotId || active.ApprovalStatus != TenantPolicyApprovalStatus.Approved ||
                        active.RequesterRef != pending.RequesterRef || string.IsNullOrWhiteSpace(item.UserId) || item.UserId == pending.RequesterActorId ||
                        string.IsNullOrWhiteSpace(active.ApproverRef) || active.RequesterRef == active.ApproverRef ||
                        active.SourceVersion != pending.SourceVersion + 1 || active.ActivatedAtUtc > now ||
                        !TenantPolicySchema.IsSafePolicyToken(active.ActivatedPolicySnapshotId) ||
                        pending.ChangedKnobIds is not [TenantPolicyKnobIds.ReviewQueueMinimum] ||
                        active.ChangedKnobIds is not [TenantPolicyKnobIds.ReviewQueueMinimum] ||
                        active.ChangeSet?.Values is not [{ KnobId: TenantPolicyKnobIds.ReviewQueueMinimum, NumberValue: { } number } value] ||
                        !TenantPolicySchema.Validate(active.ChangeSet).IsValid || pending.ChangeSet?.Values is not [var proposed] || value != proposed)
                    { return null; }

                    activeVersion = active.SourceVersion;
                    current = ((int)number, active.ActivatedPolicySnapshotId);
                    pending = null;
                }
                else if (type == typeof(TenantPolicyChangeRejected).FullName)
                {
                    pending = null;
                }
                else { return null; }
            }
        }
        catch (JsonException) { return null; }
        return current;
    }
}
