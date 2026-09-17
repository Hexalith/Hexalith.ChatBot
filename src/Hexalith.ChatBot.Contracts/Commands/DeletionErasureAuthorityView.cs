using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The bounded authority value the pure <see cref="DeletionErasurePlanner"/> consumes (AC2). The server-side
/// <c>DeletionErasureAuthorizationPolicy</c> projects a <c>ClaimsPrincipal</c> into this view so no
/// <c>ClaimsPrincipal</c> dependency ever crosses into <c>.Contracts</c>.
/// </summary>
public sealed record DeletionErasureAuthorityView(
    bool HasComplianceScope,
    IReadOnlySet<string> AuthorizedProjectRefs);
