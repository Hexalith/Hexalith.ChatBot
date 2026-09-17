using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The requested set of ChatBot-owned data classes plus the export scope (AC1).
/// </summary>
public sealed record TenantExportRequestSpec(
    IReadOnlyList<string> RequestedDataClassIds,
    TenantExportScope Scope);
