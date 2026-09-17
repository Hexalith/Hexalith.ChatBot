namespace Hexalith.ChatBot.Server.Projections;

internal sealed record GovernedControlStateProjectionNotification(
    string TenantId,
    string SubjectClass,
    string SubjectRef,
    string ControlState,
    int? RateLimitBudget,
    string? RateLimitWindow,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset EffectiveAtUtc,
    bool RevocationSensitive,
    GovernedControlDimension Dimension);
