using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Governance.AiActor;
using Hexalith.ChatBot.Server.Governance.CommandCapability;
using Hexalith.ChatBot.Server.Governance.Mailbox;
using Hexalith.ChatBot.Server.Governance.Outbound;
using Hexalith.ChatBot.Server.Governance.ServiceClient;
using Hexalith.ChatBot.Server.Operations;

namespace Hexalith.ChatBot.Server.Projections;

internal static class GovernedControlStateProjectionTranslator
{
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    public static GovernedControlStateProjectionNotification? TryCreateNotification(PublishedGovernedOperationEvent? published)
    {
        if (published is null
            || !string.Equals(published.Domain, ChatBotEventStore.DomainName, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(published.TenantId)
            || string.IsNullOrWhiteSpace(published.EventTypeName)
            || published.SequenceNumber <= 0
            || published.Payload is null
            || published.Payload.Length == 0)
        {
            return null;
        }

        return published.EventTypeName switch
        {
            string type when type == typeof(MailboxSourceDisabled).FullName => Disabled<MailboxSourceDisabled>(
                published, GovernedControlSubjectClasses.MailboxSource, static payload => payload.MailboxSourceRef, static payload => Wire(payload.NewState), static payload => payload.DisabledAtUtc),
            string type when type == typeof(MailboxSourceQuarantined).FullName => Disabled<MailboxSourceQuarantined>(
                published, GovernedControlSubjectClasses.MailboxSource, static payload => payload.MailboxSourceRef, static payload => Wire(payload.NewState), static payload => payload.QuarantinedAtUtc),
            string type when type == typeof(MailboxSourceRateLimitConfigured).FullName => RateLimit<MailboxSourceRateLimitConfigured>(
                published, GovernedControlSubjectClasses.MailboxSource, static payload => payload.MailboxSourceRef, static payload => payload.NewBudget, static payload => Wire(payload.Window), static payload => payload.ConfiguredAtUtc),

            string type when type == typeof(ServiceClientDisabled).FullName => Disabled<ServiceClientDisabled>(
                published, GovernedControlSubjectClasses.ServiceClient, static payload => payload.ServiceClientRef, static payload => Wire(payload.NewState), static payload => payload.DisabledAtUtc),
            string type when type == typeof(ServiceClientQuarantined).FullName => Disabled<ServiceClientQuarantined>(
                published, GovernedControlSubjectClasses.ServiceClient, static payload => payload.ServiceClientRef, static payload => Wire(payload.NewState), static payload => payload.QuarantinedAtUtc),
            string type when type == typeof(ServiceClientRateLimitConfigured).FullName => RateLimit<ServiceClientRateLimitConfigured>(
                published, GovernedControlSubjectClasses.ServiceClient, static payload => payload.ServiceClientRef, static payload => payload.NewBudget, static payload => Wire(payload.Window), static payload => payload.ConfiguredAtUtc),

            string type when type == typeof(AiActorDisabled).FullName => Disabled<AiActorDisabled>(
                published, GovernedControlSubjectClasses.AiActor, static payload => payload.AiActorRef, static payload => Wire(payload.NewState), static payload => payload.DisabledAtUtc),
            string type when type == typeof(AiActorQuarantined).FullName => Disabled<AiActorQuarantined>(
                published, GovernedControlSubjectClasses.AiActor, static payload => payload.AiActorRef, static payload => Wire(payload.NewState), static payload => payload.QuarantinedAtUtc),
            string type when type == typeof(AiActorRateLimitConfigured).FullName => RateLimit<AiActorRateLimitConfigured>(
                published, GovernedControlSubjectClasses.AiActor, static payload => payload.AiActorRef, static payload => payload.NewBudget, static payload => Wire(payload.Window), static payload => payload.ConfiguredAtUtc),

            string type when type == typeof(CommandCapabilityDisabled).FullName => Disabled<CommandCapabilityDisabled>(
                published, GovernedControlSubjectClasses.CommandCapability, static payload => payload.CommandCapabilityRef, static payload => Wire(payload.NewState), static payload => payload.DisabledAtUtc),
            string type when type == typeof(CommandCapabilityQuarantined).FullName => Disabled<CommandCapabilityQuarantined>(
                published, GovernedControlSubjectClasses.CommandCapability, static payload => payload.CommandCapabilityRef, static payload => Wire(payload.NewState), static payload => payload.QuarantinedAtUtc),
            string type when type == typeof(CommandCapabilityRateLimitConfigured).FullName => RateLimit<CommandCapabilityRateLimitConfigured>(
                published, GovernedControlSubjectClasses.CommandCapability, static payload => payload.CommandCapabilityRef, static payload => payload.NewBudget, static payload => Wire(payload.Window), static payload => payload.ConfiguredAtUtc),

            string type when type == typeof(OutboundChannelDisabled).FullName => Disabled<OutboundChannelDisabled>(
                published, GovernedControlSubjectClasses.OutboundChannel, static payload => payload.OutboundChannelRef, static payload => Wire(payload.NewState), static payload => payload.DisabledAtUtc),
            string type when type == typeof(OutboundChannelQuarantined).FullName => Disabled<OutboundChannelQuarantined>(
                published, GovernedControlSubjectClasses.OutboundChannel, static payload => payload.OutboundChannelRef, static payload => Wire(payload.NewState), static payload => payload.QuarantinedAtUtc),
            string type when type == typeof(OutboundChannelRateLimitConfigured).FullName => RateLimit<OutboundChannelRateLimitConfigured>(
                published, GovernedControlSubjectClasses.OutboundChannel, static payload => payload.OutboundChannelRef, static payload => payload.NewBudget, static payload => Wire(payload.Window), static payload => payload.ConfiguredAtUtc),
            _ => null,
        };
    }

    private static GovernedControlStateProjectionNotification? Disabled<TPayload>(
        PublishedGovernedOperationEvent published,
        string subjectClass,
        Func<TPayload, string> subjectRef,
        Func<TPayload, string> state,
        Func<TPayload, DateTimeOffset> effectiveAt)
    {
        TPayload? payload = Deserialize<TPayload>(published.Payload);
        if (payload is null || string.IsNullOrWhiteSpace(subjectRef(payload)))
        {
            return null;
        }

        return new GovernedControlStateProjectionNotification(
            published.TenantId!,
            subjectClass,
            subjectRef(payload),
            state(payload),
            null,
            null,
            published.SequenceNumber,
            published.CorrelationId ?? string.Empty,
            effectiveAt(payload),
            RevocationSensitive: true,
            GovernedControlDimension.ControlState);
    }

    private static GovernedControlStateProjectionNotification? RateLimit<TPayload>(
        PublishedGovernedOperationEvent published,
        string subjectClass,
        Func<TPayload, string> subjectRef,
        Func<TPayload, int> budget,
        Func<TPayload, string> window,
        Func<TPayload, DateTimeOffset> effectiveAt)
    {
        TPayload? payload = Deserialize<TPayload>(published.Payload);
        if (payload is null || string.IsNullOrWhiteSpace(subjectRef(payload)))
        {
            return null;
        }

        return new GovernedControlStateProjectionNotification(
            published.TenantId!,
            subjectClass,
            subjectRef(payload),
            GovernedControlStateView.Active,
            budget(payload),
            window(payload),
            published.SequenceNumber,
            published.CorrelationId ?? string.Empty,
            effectiveAt(payload),
            RevocationSensitive: false,
            GovernedControlDimension.RateLimit);
    }

    private static TPayload? Deserialize<TPayload>(byte[]? payload)
        => payload is null || payload.Length == 0
            ? default
            : JsonSerializer.Deserialize<TPayload>(payload, ReadOptions);

    private static string Wire(MailboxSourceControlState state)
        => state switch
        {
            MailboxSourceControlState.Disabled => GovernedControlStateView.Disabled,
            MailboxSourceControlState.Quarantined => GovernedControlStateView.Quarantined,
            _ => GovernedControlStateView.Active,
        };

    private static string Wire(ServiceClientControlState state)
        => state switch
        {
            ServiceClientControlState.Disabled => GovernedControlStateView.Disabled,
            ServiceClientControlState.Quarantined => GovernedControlStateView.Quarantined,
            _ => GovernedControlStateView.Active,
        };

    private static string Wire(AiActorControlState state)
        => state switch
        {
            AiActorControlState.Disabled => GovernedControlStateView.Disabled,
            AiActorControlState.Quarantined => GovernedControlStateView.Quarantined,
            _ => GovernedControlStateView.Active,
        };

    private static string Wire(CommandCapabilityControlState state)
        => state switch
        {
            CommandCapabilityControlState.Disabled => GovernedControlStateView.Disabled,
            CommandCapabilityControlState.Quarantined => GovernedControlStateView.Quarantined,
            _ => GovernedControlStateView.Active,
        };

    private static string Wire(OutboundChannelControlState state)
        => state switch
        {
            OutboundChannelControlState.Disabled => GovernedControlStateView.Disabled,
            OutboundChannelControlState.Quarantined => GovernedControlStateView.Quarantined,
            _ => GovernedControlStateView.Active,
        };

    private static string Wire(MailboxRateLimitWindow _) => GovernedControlStateView.RollingHour;

    private static string Wire(ServiceClientRateLimitWindow _) => GovernedControlStateView.RollingHour;

    private static string Wire(AiActorRateLimitWindow _) => GovernedControlStateView.RollingHour;

    private static string Wire(CommandCapabilityRateLimitWindow _) => GovernedControlStateView.RollingHour;

    private static string Wire(OutboundChannelRateLimitWindow _) => GovernedControlStateView.RollingHour;
}
