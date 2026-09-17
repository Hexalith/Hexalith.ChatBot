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
