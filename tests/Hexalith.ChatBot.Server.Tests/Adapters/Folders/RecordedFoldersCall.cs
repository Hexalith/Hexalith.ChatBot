using Hexalith.Folders.Client.Generated;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Records the exact upload submitted to the owner client.</summary>
/// <param name="FolderId">The folder scope.</param>
/// <param name="WorkspaceId">The workspace scope.</param>
/// <param name="IdempotencyKey">The SDK canonical key.</param>
/// <param name="CorrelationId">The request correlation.</param>
/// <param name="TaskId">The mutation task.</param>
/// <param name="Body">The submitted mutation.</param>
public sealed record RecordedFoldersCall(
    string FolderId,
    string WorkspaceId,
    string IdempotencyKey,
    string CorrelationId,
    string TaskId,
    FileMutationRequest Body);
