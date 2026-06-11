using System.Security.Claims;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.Admin;

namespace Hexalith.ChatBot.Server.Notifications;

/// <summary>
/// A candidate recipient for routing resolution: a metadata-safe reference plus the principal carrying the
/// authority claims used to resolve scope and per-resource authority.
/// </summary>
internal sealed record NotificationRecipientCandidate(string RecipientRef, ClaimsPrincipal Principal);

/// <summary>
/// Server-side routing/recipient-resolution engine (FR72, NFR2). Given a notify-worthy state event and the
/// configured <c>(state-class × scope)</c> routing map, it produces the metadata-only delivery set, reusing the
/// existing authority path (<see cref="AdminAuthorityEvaluator"/> for scope-based recipients and the per-project
/// project-owner authority check used across the gateway/queue/compliance read policies) rather than a new bespoke
/// authority check. Recipients lacking per-item authority are downgraded to a safe redacted form indistinguishable
/// from safe-not-found; no resource-existence leakage.
/// </summary>
internal static class NotificationRoutingResolver
{
    public static IReadOnlyList<NotificationDelivery> Resolve(
        NotificationStateEvent stateEvent,
        NotificationRoutingChangeSet routing,
        IReadOnlyList<NotificationRecipientCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(stateEvent);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(candidates);

        // Trust boundary: only a schema-valid routing map produces deliveries (fail-closed on undeclared values).
        if (!NotificationRoutingSchema.Validate(routing).IsValid)
        {
            return [];
        }

        List<NotificationDelivery> deliveries = [];
        foreach (NotificationRoutingEntry entry in routing.Entries)
        {
            if (entry.StateClass != stateEvent.StateClass)
            {
                continue;
            }

            foreach (NotificationRecipientCandidate candidate in candidates)
            {
                // Audience: only the configured human recipient role hears about this (state-class × scope) route.
                if (!AdminAuthorityEvaluator.HasHumanRole(candidate.Principal, entry.RecipientRole) ||
                    !AdminAuthorityEvaluator.HasHumanAdminScope(candidate.Principal, entry.Scope))
                {
                    continue;
                }

                NotificationContentVisibility visibility = ResolveVisibility(stateEvent, candidate.Principal);
                deliveries.Add(new NotificationDelivery(
                    entry.StateClass,
                    entry.Channel,
                    entry.RecipientRole,
                    entry.Scope,
                    candidate.RecipientRef,
                    stateEvent.TenantRef,
                    visibility is NotificationContentVisibility.ItemContext ? stateEvent.ItemRef : null,
                    stateEvent.QueueRef,
                    stateEvent.ReasonCode,
                    stateEvent.CorrelationId,
                    visibility,
                    stateEvent.RaisedAtUtc.ToUniversalTime()));
            }
        }

        return deliveries;
    }

    private static NotificationContentVisibility ResolveVisibility(NotificationStateEvent stateEvent, ClaimsPrincipal principal)
    {
        // Aggregate/see-only events never carry item-specific context.
        if (string.IsNullOrWhiteSpace(stateEvent.ItemProjectRef))
        {
            return NotificationContentVisibility.MetadataRedacted;
        }

        // Item-specific context is delivered only to recipients with per-resource authority over that item;
        // everyone else receives the safe metadata-only/redacted form (NFR2 redaction discipline).
        return AdminAuthorityEvaluator.HasProjectAuthority(principal, stateEvent.ItemProjectRef)
            ? NotificationContentVisibility.ItemContext
            : NotificationContentVisibility.MetadataRedacted;
    }
}
