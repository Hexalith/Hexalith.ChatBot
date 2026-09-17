using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The requested set of ChatBot-owned data classes plus the deletion/erasure mode and scope (AC1).
/// <see cref="Mode"/> ∈ <see cref="DeletionErasureModes"/>.
/// </summary>
public sealed record DeletionErasureRequestSpec(
    string Mode,
    IReadOnlyList<string> RequestedDataClassIds,
    DeletionErasureScope Scope);
