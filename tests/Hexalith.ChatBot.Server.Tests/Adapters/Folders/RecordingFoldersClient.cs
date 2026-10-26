using System.Reflection;

using Hexalith.Folders.Client.Generated;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Folders;

/// <summary>Records the adapter contract; its accepted commands do not emulate native owner admission.</summary>
public class RecordingFoldersClient : DispatchProxy
{
    /// <summary>Gets the ordered owner calls.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>Gets the cancellation token received by each owner call.</summary>
    public List<CancellationToken> CancellationTokens { get; } = [];

    /// <summary>Gets upload requests.</summary>
    public List<RecordedFoldersCall> Requests { get; } = [];

    /// <summary>Gets scoped file observations.</summary>
    public List<(string FolderId, string WorkspaceId, string CorrelationId, string TaskId, ReadConsistencyClass Consistency, FileMetadataRequest Body)> MetadataRequests { get; } = [];

    /// <summary>Gets scoped lock observations.</summary>
    public List<(string FolderId, string WorkspaceId, string CorrelationId, ReadConsistencyClass Consistency)> LockRequests { get; } = [];

    /// <summary>Gets or sets the owner failure.</summary>
    public Exception? Failure { get; set; }

    /// <summary>Gets or sets the operation that fails.</summary>
    public string FailureAt { get; set; } = "AddFileAsync";

    /// <summary>Gets or sets the replay outcome explicitly supplied by the owner fixture.</summary>
    public bool IdempotentReplay { get; set; }

    /// <summary>Gets or sets a metadata response transformation.</summary>
    public Action<FileMetadataResult>? ChangeMetadata { get; set; }

    /// <summary>Gets or sets a lock response transformation.</summary>
    public Action<WorkspaceLockStatus>? ChangeLock { get; set; }

    /// <summary>Gets or sets an action immediately before the owner response.</summary>
    public Action? BeforeResponse { get; set; }

    /// <inheritdoc />
    protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(targetMethod);
        CancellationToken cancellationToken = (CancellationToken)args[^1]!;
        CancellationTokens.Add(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add(targetMethod.Name);
        string folderId = (string)args[0]!;
        string workspaceId = (string)args[1]!;

        switch (targetMethod.Name)
        {
            case "GetFolderFileMetadataAsync" when args.Length == 7:
                FileMetadataRequest query = (FileMetadataRequest)args[5]!;
                MetadataRequests.Add((folderId, workspaceId, (string)args[2]!, (string)args[3]!, (ReadConsistencyClass)args[4]!, query));
                PathMetadata path = query.Paths.Single();
                FileMetadataResult metadata = new()
                {
                    Items = [new FileMetadataItem
                    {
                        FileId = "owner-file-incarnation",
                        Version = 17,
                        Kind = FileMetadataItemKind.File,
                        Redaction = FileMetadataItemRedaction.Not_redacted,
                        Path = new VisiblePathMetadata
                        {
                            NormalizedPath = path.NormalizedPath,
                            DisplayName = path.DisplayName,
                            PathPolicyClass = VisiblePathMetadataPathPolicyClass.Metadata_only,
                            UnicodeNormalization = path.UnicodeNormalization,
                        },
                    }],
                    Freshness = new FreshnessMetadata { ReadConsistency = ReadConsistencyClass.Snapshot_per_task, ObservedAt = DateTimeOffset.UtcNow },
                    Limits = new ContextQueryLimitMetadata { ActualCount = 1 },
                };
                ChangeMetadata?.Invoke(metadata);
                Respond(targetMethod.Name);
                return Task.FromResult(metadata);

            case "GetWorkspaceLockAsync" when args.Length == 5:
                LockRequests.Add((folderId, workspaceId, (string)args[2]!, (ReadConsistencyClass)args[3]!));
                WorkspaceLockStatus workspaceLock = new()
                {
                    WorkspaceReference = new RedactableDiagnosticIdentifier
                    {
                        Value = workspaceId,
                        Redaction = new RedactionMetadata { Visibility = RedactionMetadataVisibility.Metadata_only },
                    },
                    LockState = LockState.Locked,
                    Lease = new LockLeaseMetadata
                    {
                        LockId = "owner-lock",
                        LockFence = 29,
                        HolderRef = MetadataRequests[^1].TaskId,
                        LeaseStatus = LockLeaseStatus.Active,
                        AcquiredAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                        EffectiveAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
                    },
                    Freshness = new FreshnessMetadata { ReadConsistency = ReadConsistencyClass.Read_your_writes, ObservedAt = DateTimeOffset.UtcNow },
                };
                ChangeLock?.Invoke(workspaceLock);
                Respond(targetMethod.Name);
                return Task.FromResult(workspaceLock);

            case "AddFileAsync" when args.Length == 7:
                Requests.Add(new RecordedFoldersCall(folderId, workspaceId, (string)args[2]!, (string)args[3]!, (string)args[4]!, (FileMutationRequest)args[5]!));
                Respond(targetMethod.Name);
                return Task.FromResult(new AcceptedCommand
                {
                    AcceptedAt = DateTimeOffset.UtcNow,
                    CorrelationId = (string)args[3]!,
                    TaskId = (string)args[4]!,
                    Status = AcceptedCommandStatus.Accepted,
                    IdempotentReplay = IdempotentReplay,
                });

            default:
                throw new InvalidOperationException("Unexpected Folders client operation.");
        }
    }

    private void Respond(string method)
    {
        BeforeResponse?.Invoke();
        if (method == FailureAt && Failure is not null)
        {
            throw Failure;
        }
    }
}
