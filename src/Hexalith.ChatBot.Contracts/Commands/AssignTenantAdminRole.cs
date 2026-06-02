using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// Security-sensitive tenant administration role assignment through the governed command spine.
/// </summary>
public sealed record AssignTenantAdminRole(
    string AssignmentId,
    string TargetActorId,
    AdminRole Role,
    string ReasonCode,
    string PolicySnapshotId,
    long SourceVersion) : IChatBotCommand;
