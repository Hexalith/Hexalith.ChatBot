using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>A metadata-only owner evidence request; this is an internal seam, not an invented owner contract.</summary>
/// <param name="Owner">The authoritative owner.</param>
/// <param name="PrincipalId">The bound requester.</param>
/// <param name="TenantId">The exact bound tenant.</param>
/// <param name="ResourceId">The exact resource.</param>
/// <param name="Operation">The exact operation.</param>
/// <param name="Authority">The required scoped authority.</param>
/// <param name="ActorClass">The bound class.</param>
/// <param name="Origin">The provenance declaration.</param>
/// <param name="RequireCurrent">Whether evidence must be revalidated for this request.</param>
internal sealed record ChatBotOwnerAuthorityRequest(string Owner, string PrincipalId, string TenantId, string ResourceId, string Operation, string Authority, string ActorClass, ChatBotSurfaceOrigin Origin, bool RequireCurrent);
