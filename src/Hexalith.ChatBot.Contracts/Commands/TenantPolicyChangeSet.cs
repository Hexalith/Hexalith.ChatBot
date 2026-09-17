using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

public sealed record TenantPolicyChangeSet(
    IReadOnlyList<TenantPolicyValue> Values);
