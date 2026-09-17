using System.Security.Cryptography;
using System.Text;

using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// A metadata-only erasure-proof confirmation for one successfully-erased subject/class (AC5). Carries the
/// tenant-scoped <see cref="SubjectLocator"/> + <see cref="Tombstoned"/> tombstone confirmation and the safe KMS
/// <see cref="KeyHandle"/> + <see cref="KeyShredded"/> key-shred confirmation — never raw subject content.
/// </summary>
public sealed record ErasureProofEntry(
    string DataClassId,
    string SubjectLocator,
    bool Tombstoned,
    string KeyHandle,
    bool KeyShredded);
