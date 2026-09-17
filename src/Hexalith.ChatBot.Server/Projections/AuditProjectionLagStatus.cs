using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Projections;

/// <summary>
/// Metadata-only audit-projection-lag status read model (FR67, M0/M1 fidelity). There is no existing
/// audit-projection-lag read source; this derives a coarse status from the Audit seam's last projected/reconciled
/// checkpoint position versus the latest committed event position. It surfaces a health enum and a coarse lag
/// indicator only — never audit envelope contents, hash-chain detail, redaction keys, or audit reasons. When the
/// checkpoint source is unavailable or the snapshot has expired, it prefers <see cref="ChatBotHealthStatus.Unknown"/>
/// (fail-safe) over a fabricated <see cref="ChatBotHealthStatus.Healthy"/>.
/// </summary>
internal sealed record AuditProjectionLagStatus(
    ChatBotHealthStatus Health,
    string LagIndicator,
    long? LagEvents,
    DateTimeOffset FreshnessTimestampUtc);
