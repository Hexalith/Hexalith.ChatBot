using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The bounded authority value the pure <see cref="ConsentLawfulBasisRedactionPolicy"/> consumes (AC2). The
/// server-side <c>ConsentLawfulBasisAuthorizationPolicy</c> projects a <c>ClaimsPrincipal</c> into this view so no
/// <c>ClaimsPrincipal</c> dependency ever crosses into <c>.Contracts</c>. Mirrors <see cref="DeletionErasureAuthorityView"/>.
/// </summary>
public sealed record ConsentLawfulBasisAuthorityView(
    bool HasComplianceScope,
    IReadOnlySet<string> AuthorizedProjectRefs);
