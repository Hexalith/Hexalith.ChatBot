using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// Finite-token validator for <see cref="DegradedDependencyIncident"/>. It enforces the safe-token posture shared
/// with <see cref="OperationalDashboardContractValidator"/> (ASCII alnum + <c>.-_:@|</c>, ≤200 chars, marker-ban),
/// the degraded/failed health enum, a UTC detection timestamp, a bounded detection budget, a defined non-unknown
/// scope kind, and FR77 reason-code membership. It carries no business logic and never inspects restricted detail.
/// </summary>
public static class DegradedDependencyContractValidator
{
    /// <summary>The NFR41 5-minute "state the affected scope + dependency within 5 minutes" detection budget, in seconds.</summary>
    public const int DefaultDetectionBudgetSeconds = 300;

    public static IReadOnlyList<string> Validate(DegradedDependencyIncident incident)
    {
        ArgumentNullException.ThrowIfNull(incident);

        List<string> errors = [];
        if (incident.Health is not (ChatBotHealthStatus.Degraded or ChatBotHealthStatus.Failed))
        {
            errors.Add("health_invalid");
        }

        if (incident.DetectedAtUtc.Offset != TimeSpan.Zero)
        {
            errors.Add("detected_at_not_utc");
        }

        if (incident.DetectionBudgetSeconds is < 1 or > DefaultDetectionBudgetSeconds)
        {
            errors.Add("detection_budget_invalid");
        }

        if (!Enum.IsDefined(incident.ScopeKind) || incident.ScopeKind == DependencyScopeKind.Unknown)
        {
            errors.Add("scope_kind_invalid");
        }

        if (!IsRequiredSafeToken(incident.DependencyId))
        {
            errors.Add("dependency_id_invalid");
        }

        if (!IsRequiredSafeToken(incident.AffectedScope))
        {
            errors.Add("affected_scope_invalid");
        }

        if (!IsRequiredSafeToken(incident.OwnerRole))
        {
            errors.Add("owner_role_invalid");
        }

        if (!IsRequiredSafeToken(incident.NextSafeAction))
        {
            errors.Add("next_safe_action_invalid");
        }

        if (!IsRequiredSafeToken(incident.CorrelationId))
        {
            errors.Add("correlation_id_invalid");
        }

        if (!IsRequiredSafeToken(incident.ReasonCode) || !ChatBotMessageCodes.All.Contains(incident.ReasonCode))
        {
            errors.Add("reason_code_invalid");
        }

        return errors;
    }

    public static bool IsValid(DegradedDependencyIncident incident)
        => Validate(incident).Count == 0;

    public static bool IsRequiredSafeToken(string? value)
        => !string.IsNullOrWhiteSpace(value) && IsSafeToken(value);

    public static bool IsSafeToken(string? value)
        => value is null ||
            value.Length <= 200 &&
            !ContainsSensitiveMarker(value) &&
            value.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_' or ':' or '@' or '|');

    private static bool ContainsSensitiveMarker(string value)
    {
        string[] markers =
        [
            "secret",
            "password",
            "bearer",
            "token",
            "exception",
            ".txt",
            ".json",
            ".xml",
        ];

        return markers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}
