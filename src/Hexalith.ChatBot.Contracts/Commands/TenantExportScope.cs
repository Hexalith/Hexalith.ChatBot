using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The bounded export scope (AC1/AC2). An empty <see cref="ProjectScopeRefs"/> is a tenant-wide request; a
/// non-empty list is project-bounded and every member must be covered by the requester's per-project authority.
/// </summary>
public sealed record TenantExportScope(
    string TenantRef,
    IReadOnlyList<string> ProjectScopeRefs);
