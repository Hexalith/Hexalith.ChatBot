namespace Hexalith.ChatBot.Server.Projections;

internal sealed record GovernedControlStateView(
    string TenantId,
    string SubjectClass,
    string SubjectRef,
    string ControlState,
    int? RateLimitBudget,
    string? RateLimitWindow,
    long SourceVersion,
    string CorrelationId,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset LastUpdatedAtUtc,
    bool RevocationSensitive,
    IReadOnlyList<DateTimeOffset>? AdmittedAtUtc = null)
{
    public const string Active = "active";
    public const string Disabled = "disabled";
    public const string Quarantined = "quarantined";
    public const string RollingHour = "rolling-hour";

    public IReadOnlyList<DateTimeOffset> RecentAdmittedAtUtc => AdmittedAtUtc ?? [];

    public static string KeyFor(string tenantId, string subjectClass, string subjectRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectClass);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectRef);
        return $"{tenantId}:governed-control:{subjectClass}:{subjectRef}";
    }
}

internal static class GovernedControlSubjectClasses
{
    public const string MailboxSource = "mailbox-source";
    public const string ServiceClient = "service-client";
    public const string AiActor = "ai-actor";
    public const string CommandCapability = "command-capability";
    public const string OutboundChannel = "outbound-channel";
}
