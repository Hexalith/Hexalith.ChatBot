using System.Text.Json;

using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Folds one complete, authoritative grant stream into request-bound evidence.</summary>
internal static class ChatBotGrantSourceEvidence
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Returns no authority for absent, malformed, revoked, expired, or mismatched source history.</summary>
    internal static ChatBotOwnerAuthorityEvidence? FromStream(
        ChatBotOwnerAuthorityRequest request, AuthoritativeEventStream? stream, DateTimeOffset now)
    {
        if (stream is null || stream.Identity.TenantId != request.TenantId || stream.Identity.Domain != "chatbot" ||
            stream.Identity.AggregateId != ChatBotAuthorityGrantKey.For(request.TenantId, request.PrincipalId,
                request.ResourceId, request.Operation, request.Authority, request.ActorClass, request.Origin) ||
            stream.Head <= 0 || stream.Events.Count != stream.Head || stream.ObservedAt > now ||
            string.IsNullOrWhiteSpace(stream.ObservationId))
        {
            return null;
        }

        ChatBotAuthorityGranted? current = null;
        bool revoked = false;
        HashSet<string> issuanceIds = new(StringComparer.Ordinal);
        long sequence = 0;
        foreach (StreamReadEvent item in stream.Events)
        {
            if (item is null || item.SequenceNumber != ++sequence || item.Payload is null ||
                item.SerializationFormat != "json" ||
                item.ProtectionMetadata is { } protection &&
                    (protection.State is not (PayloadProtectionState.Unprotected or PayloadProtectionState.Protected) ||
                        protection.MetadataVersion != EventStorePayloadProtectionMetadata.CurrentMetadataVersion))
            {
                return null;
            }

            try
            {
                string eventType = item.EventTypeName.Split(',', 2)[0].Trim();
                if (eventType == typeof(ChatBotAuthorityGranted).FullName)
                {
                    ChatBotAuthorityGranted? granted = JsonSerializer.Deserialize<ChatBotAuthorityGranted>(item.Payload, JsonOptions);
                    if (granted is null || granted.TenantId != request.TenantId || granted.PrincipalId != request.PrincipalId ||
                        granted.ResourceId != request.ResourceId || granted.Operation != request.Operation ||
                        granted.Authority != request.Authority || granted.ActorClass != request.ActorClass ||
                        granted.Origin != request.Origin || granted.ExpiresAt.Offset != TimeSpan.Zero ||
                        string.IsNullOrWhiteSpace(granted.GrantId) || !issuanceIds.Add(granted.GrantId) ||
                        current is not null && !revoked && current.ExpiresAt > item.Timestamp ||
                        !ValidNestedServiceGrant(granted))
                    {
                        return null;
                    }

                    current = granted;
                    revoked = false;
                }
                else if (eventType == typeof(ChatBotAuthorityRevoked).FullName)
                {
                    ChatBotAuthorityRevoked? removal = JsonSerializer.Deserialize<ChatBotAuthorityRevoked>(item.Payload, JsonOptions);
                    if (removal is null || current is null || revoked || removal.GrantId != current.GrantId)
                    {
                        return null;
                    }

                    revoked = true;
                }
                else if (eventType == typeof(ChatBotAuthorityChangeRejected).FullName)
                {
                    ChatBotAuthorityChangeRejected? rejection = JsonSerializer.Deserialize<ChatBotAuthorityChangeRejected>(item.Payload, JsonOptions);
                    if (rejection is null || rejection.AuthorityKey != stream.Identity.AggregateId ||
                        rejection.Reason is not ("invalid_authority_grant" or "authority_issuance_reused" or
                            "authority_grant_active" or "authority_grant_mismatch"))
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (JsonException)
            {
                return null;
            }
        }

        if (current is null || revoked || current.ExpiresAt <= now)
        {
            return null;
        }

        return new(request, stream.Identity.AggregateId, stream.Head.ToString(System.Globalization.CultureInfo.InvariantCulture),
            stream.ObservedAt, stream.ObservedAt, current.ExpiresAt, true, false, current.ServiceGrant);
    }

    private static bool ValidNestedServiceGrant(ChatBotAuthorityGranted granted)
    {
        if (granted.ServiceGrant is not { } serviceGrant)
        {
            return granted.Authority != "service-grant";
        }

        return granted.Authority == "service-grant" && granted.ActorClass is ("service" or "ai") &&
            granted.PrincipalId == granted.ResourceId && serviceGrant.GrantId == granted.GrantId &&
            serviceGrant.TenantId == granted.TenantId && serviceGrant.ServiceClientId == granted.PrincipalId &&
            serviceGrant.SurfaceOrigin == granted.Origin && serviceGrant.ExpiresAt == granted.ExpiresAt &&
            !serviceGrant.IsRevoked;
    }
}
