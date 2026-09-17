using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record MailboxRoutingRule(
    string RoutingRuleId,
    MailboxRoutingRuleKind Kind,
    string SourceContext,
    string TargetRef,
    int Priority,
    string ReasonCode);
