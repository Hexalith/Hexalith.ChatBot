using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

namespace Hexalith.ChatBot.Server.Observability;

/// <summary>
/// The candidate scope tokens for a degraded/failed dependency, in NFR41 precedence order (narrowest first). Any
/// component the signal genuinely lacks is left <see langword="null"/>; the resolver picks the narrowest present.
/// </summary>
internal sealed record ScopeCandidates(
    string? WorkflowItemRef = null,
    string? OperationRef = null,
    string? CommandSurfaceRef = null,
    string? ServiceClientRef = null,
    string? ProjectRef = null,
    string? MailboxRef = null,
    string? TenantRef = null);
