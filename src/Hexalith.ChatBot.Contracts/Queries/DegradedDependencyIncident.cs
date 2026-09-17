using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;

namespace Hexalith.ChatBot.Contracts.Queries;

/// <summary>
/// A metadata-only incident status for a single degraded or failed dependency (NFR41). It states the affected
/// scope and dependency an on-call engineer needs to reach the correct next step, and nothing more: it carries no
/// project name, file metadata, candidate evidence, participant PII, message subject, or audit detail (NFR2).
/// Every string field is a safe aggregate token; <see cref="AffectedScope"/> uses the <c>{scopeKind}:{token}</c>
/// form. It is a read/diagnostic status, never an <c>IChatBotCommand</c> — no write path.
/// </summary>
/// <param name="DependencyId">The affected dependency identity (safe token).</param>
/// <param name="ScopeKind">The resolved narrowest scope kind isolating the dependency.</param>
/// <param name="AffectedScope">The resolved narrowest affected scope, in <c>{scopeKind}:{token}</c> form.</param>
/// <param name="Health">The dependency health; only <see cref="ChatBotHealthStatus.Degraded"/>/<see cref="ChatBotHealthStatus.Failed"/> fire an incident.</param>
/// <param name="DetectedAtUtc">The detection instant (UTC).</param>
/// <param name="DetectionBudgetSeconds">The NFR41 detection budget; the canonical value is <see cref="DegradedDependencyContractValidator.DefaultDetectionBudgetSeconds"/> (300s).</param>
/// <param name="OwnerRole">The responsible owner role for triage (kebab-case safe token).</param>
/// <param name="NextSafeAction">The bounded next safe action affordance (safe token).</param>
/// <param name="ReasonCode">The FR77 catalog reason code for the degradation.</param>
/// <param name="CorrelationId">The correlation identity carried through the spine.</param>
public sealed record DegradedDependencyIncident(
    string DependencyId,
    DependencyScopeKind ScopeKind,
    string AffectedScope,
    ChatBotHealthStatus Health,
    DateTimeOffset DetectedAtUtc,
    int DetectionBudgetSeconds,
    string OwnerRole,
    string NextSafeAction,
    string ReasonCode,
    string CorrelationId);
