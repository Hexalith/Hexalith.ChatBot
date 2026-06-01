using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Cli;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using NSubstitute;

using Shouldly;

namespace Hexalith.ChatBot.Cli.Tests;

using AssociateEmailToProjectCommand = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using CorrectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using DecideAiActionApprovalCommand = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DeferEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ExecuteApprovedAIActionCommand = Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction;
using GeneratedAssociationCandidate = Hexalith.ChatBot.Client.Generated.AssociationCandidate;
using GeneratedAssociationEvidenceReference = Hexalith.ChatBot.Client.Generated.AssociationEvidenceReference;
using GeneratedAssociationReasonCode = Hexalith.ChatBot.Client.Generated.AssociationReasonCode;
using GeneratedLifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState;
using GeneratedOperationAuditStatus = Hexalith.ChatBot.Client.Generated.OperationAuditStatus;
using GeneratedOperationCompletionStatus = Hexalith.ChatBot.Client.Generated.OperationCompletionStatus;
using RejectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using RequestFailedWorkflowRetryCommand = Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry;

public static class ChatBotCliCommandTests
{
    private const string AssociationId = "01HX0000000000000000000001";
    private const string IntakeId = "01HX0000000000000000000002";
    private const string ProjectId = "project-123";
    private const string EvidenceFingerprint = "ev-fingerprint";
    private const string CorrelationId = "01HX0000000000000000000003";
    private const string TaskId = "01HX0000000000000000000004";

    [Theory]
    [InlineData("associate", typeof(AssociateEmailToProjectCommand))]
    [InlineData("reject", typeof(RejectEmailProjectAssociationCommand))]
    [InlineData("defer", typeof(DeferEmailProjectAssociationCommand))]
    [InlineData("correct", typeof(CorrectEmailProjectAssociationCommand))]
    public static async Task AssociationStateChangingCommandsSubmitTypedCommandsWithCliOrigin(
        string verb,
        Type expectedCommandType)
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            AssociationArgs(verb),
            client,
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        await client.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == expectedCommandType),
            null,
            null,
            ChatBotSurfaceOrigin.Cli,
            Arg.Any<CancellationToken>());
        error.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("operation retry", typeof(RequestFailedWorkflowRetryCommand))]
    [InlineData("approval decide", typeof(DecideAiActionApprovalCommand))]
    [InlineData("ai-action execute", typeof(ExecuteApprovedAIActionCommand))]
    public static async Task NonAssociationStateChangingCommandsSubmitTypedCommandsWithCliOrigin(
        string command,
        Type expectedCommandType)
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            CommandArgs(command),
            client,
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        await client.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(submitted => submitted.GetType() == expectedCommandType),
            null,
            null,
            ChatBotSurfaceOrigin.Cli,
            Arg.Any<CancellationToken>());
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public static async Task ReadCommandsUseClientFacadeReadMethodsOnly()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AssociationStatus()));
        _ = client.GetProjectConversationAsync(ProjectId, null, 25, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProjectConversation()));
        _ = client.GetTaskIntentReviewAsync(ProjectId, "task-intent-1", CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TaskReview()));
        _ = client.GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OperationStatus()));
        _ = client.GetOperationAuditHistoryAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OperationAudit()));

        await InvokeReadAsync(["association", "status", "--association-id", AssociationId]);
        await InvokeReadAsync(["conversation", "--project-id", ProjectId]);
        await InvokeReadAsync(["task", "review", "--project-id", ProjectId, "--task-intent-id", "task-intent-1"]);
        await InvokeReadAsync(["operation", "status", "--operation-id", AssociationId]);
        await InvokeReadAsync(["operation", "audit", "--operation-id", AssociationId]);

        await client.Received(1).GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>());
        await client.Received(1).GetProjectConversationAsync(ProjectId, null, 25, CorrelationId, TaskId, Arg.Any<CancellationToken>());
        await client.Received(1).GetTaskIntentReviewAsync(ProjectId, "task-intent-1", CorrelationId, TaskId, Arg.Any<CancellationToken>());
        await client.Received(1).GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>());
        await client.Received(1).GetOperationAuditHistoryAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>());
        await client.DidNotReceive().SubmitAsync(
            Arg.Any<IChatBotCommand>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<ChatBotSurfaceOrigin>(),
            Arg.Any<CancellationToken>());

        async Task InvokeReadAsync(string[] args)
        {
            using var output = new StringWriter();
            using var error = new StringWriter();
            string[] fullArgs = [.. args, "--correlation-id", CorrelationId, "--task-id", TaskId];
            int exitCode = await ChatBotCliCommands.InvokeAsync(fullArgs, client, output, error, CancellationToken.None)
                .ConfigureAwait(false);
            exitCode.ShouldBe(0);
        }
    }

    [Fact]
    public static async Task TenantOptionIsDisplayIntentOnlyAndIsNotForwardedAsAuthority()
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        _ = client.GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OperationStatus()));
        using var writeOutput = new StringWriter();
        using var writeError = new StringWriter();
        using var readOutput = new StringWriter();
        using var readError = new StringWriter();

        int writeExitCode = await ChatBotCliCommands.InvokeAsync(
            [.. AssociationArgs("associate"), "--tenant", "tenant-alpha", "--correlation-id", CorrelationId, "--task-id", TaskId],
            client,
            writeOutput,
            writeError,
            CancellationToken.None);
        int readExitCode = await ChatBotCliCommands.InvokeAsync(
            ["operation", "status", "--operation-id", AssociationId, "--tenant", "tenant-alpha", "--correlation-id", CorrelationId, "--task-id", TaskId],
            client,
            readOutput,
            readError,
            CancellationToken.None);

        writeExitCode.ShouldBe(0);
        readExitCode.ShouldBe(0);
        await client.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == typeof(AssociateEmailToProjectCommand)),
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Cli,
            Arg.Any<CancellationToken>());
        await client.Received(1).GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>());
        writeOutput.ToString().ShouldNotContain("tenant-alpha");
        readOutput.ToString().ShouldNotContain("tenant-alpha");
        writeError.ToString().ShouldBeEmpty();
        readError.ToString().ShouldBeEmpty();
    }

    [Fact]
    public static async Task AcceptedCommandJsonOutputReportsPartialSuccessWorkflowMetadata()
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            [.. AssociationArgs("associate"), "--json"],
            client,
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(0);
        using JsonDocument document = JsonDocument.Parse(output.ToString());
        JsonElement root = document.RootElement;
        root.GetProperty("outcome").GetString().ShouldBe("command-accepted");
        root.GetProperty("operationId").GetString().ShouldBe("op-1");
        root.GetProperty("commandId").GetString().ShouldBe("cmd-1");
        root.GetProperty("correlationId").GetString().ShouldBe("corr-1");
        root.GetProperty("taskId").GetString().ShouldBe("op-1");
        root.GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        root.GetProperty("auditStatus").GetString().ShouldBe("reconciling");
        root.GetProperty("retryCount").GetInt32().ShouldBe(0);
        root.GetProperty("safeNextActions").EnumerateArray().Select(static action => action.GetString()).ShouldBe(["operation status", "operation audit"]);
        output.ToString().ShouldNotContain("outcome: success", Case.Insensitive);
        output.ToString().ShouldNotContain("done", Case.Insensitive);
        error.ToString().ShouldBeEmpty();
    }

    [Fact]
    public static async Task CliInvocationRedactsSafeDenialPayloadsAtCommandBoundary()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.SubmitAsync(
                Arg.Any<IChatBotCommand>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ChatBotSurfaceOrigin>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<CommandSubmissionResponse>>(_ => throw RestrictedPayloadException(403, "wrong surface"));
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            AssociationArgs("reject"),
            client,
            output,
            error,
            CancellationToken.None);

        exitCode.ShouldBe(1);
        output.ToString().ShouldBeEmpty();
        string errorText = error.ToString();
        errorText.ShouldContain("outcome: denied");
        errorText.ShouldContain("reason-code: authorization-denied");
        errorText.ShouldContain("redaction-state: metadata-only");
        errorText.ShouldNotContain("restricted project");
        errorText.ShouldNotContain("bearer-token");
        errorText.ShouldNotContain("raw-claim");
        errorText.ShouldNotContain("provider-payload");
        await client.Received(1).SubmitAsync(
            Arg.Any<IChatBotCommand>(),
            null,
            null,
            ChatBotSurfaceOrigin.Cli,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public static void SafeDenialFormatterPreservesTypedCatalogProblemMetadataOnly()
    {
        var exception = new HexalithChatBotApiException<ProblemDetails>(
            "raw server payload containing restricted project",
            403,
            response: "restricted project secret bearer-token raw-claim provider-payload",
            headers: new Dictionary<string, IEnumerable<string>>(),
            result: new ProblemDetails
            {
                Status = 403,
                Category = ProblemDetailsCategory.Authorization_denied,
                Code = "authorization_denied",
                Message = "Access is denied.",
                CorrelationId = CorrelationId,
                TaskId = TaskId,
                Retryable = false,
                ClientAction = ProblemDetailsClientAction.RequestAccess,
                Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
            },
            innerException: null);

        string text = ChatBotCliOutputFormatter.FormatSafeDenial(exception, json: false);

        text.ShouldContain("category: authorization_denied");
        text.ShouldContain("reason-code: authorization_denied");
        text.ShouldContain("redaction-state: metadata-only");
        text.ShouldContain("safe-next-action: request-access");
        text.ShouldContain($"correlation-id: {CorrelationId}");
        text.ShouldContain($"task-id: {TaskId}");
        text.ShouldNotContain("restricted project");
        text.ShouldNotContain("bearer-token");
        text.ShouldNotContain("raw-claim");
        text.ShouldNotContain("provider-payload");
    }

    [Fact]
    public static void OperationStatusFormatterReportsPartialSuccessWithoutFalseTerminalSuccess()
    {
        string text = ChatBotCliOutputFormatter.FormatOperationStatus(OperationStatus(), json: false);

        text.ShouldContain("completion-status: accepted-projection-pending");
        text.ShouldContain("audit-status: reconciling");
        text.ShouldContain("partial-success: accepted by backend; projection reconciliation is pending");
        text.ShouldNotContain("outcome: success", Case.Insensitive);
        text.ShouldNotContain("done", Case.Insensitive);
    }

    [Fact]
    public static void OperationStatusFormatterIncludesTerminalAndFailureReasonFieldsWhenPresent()
    {
        OperationStatus status = OperationStatus();
        status.CompletionStatus = GeneratedOperationCompletionStatus.Failed;
        status.AuditStatus = GeneratedOperationAuditStatus.Committed;
        status.TerminalReason = ChatBotMessageCode.Dependency_degraded;
        status.FailureReasonCode = ChatBotMessageCode.Dependency_degraded;
        status.TerminalReasonCode = ChatBotMessageCode.Audit_unavailable;

        string text = ChatBotCliOutputFormatter.FormatOperationStatus(status, json: false);

        text.ShouldContain("completion-status: failed");
        text.ShouldContain("audit-status: committed");
        text.ShouldContain("terminal-reason: dependency_degraded");
        text.ShouldContain("failure-reason-code: dependency_degraded");
        text.ShouldContain("terminal-reason-code: audit_unavailable");
    }

    [Theory]
    [InlineData(401, "stale credential")]
    [InlineData(403, "revoked grant")]
    [InlineData(403, "wrong surface")]
    [InlineData(403, "tenant mismatch")]
    [InlineData(404, "safe not found")]
    public static void SafeDenialFormatterDoesNotPrintRestrictedPayloads(int statusCode, string scenario)
    {
        HexalithChatBotApiException exception = RestrictedPayloadException(statusCode, scenario);

        string text = ChatBotCliOutputFormatter.FormatSafeDenial(exception, json: false);

        text.ShouldContain("outcome: denied");
        text.ShouldContain("redaction-state: metadata-only");
        text.ShouldNotContain("restricted project");
        text.ShouldNotContain("bearer-token");
        text.ShouldNotContain("raw-claim");
        text.ShouldNotContain("provider-payload");
    }

    [Fact]
    public static void ValidationDenialFormatterDoesNotPrintRawCommandPayload()
    {
        string text = ChatBotCliOutputFormatter.FormatSafeDenial(
            new ArgumentException("raw command json with client_secret"),
            json: false);

        text.ShouldContain("reason-code: validation-error");
        text.ShouldContain("safe-next-action: correct-request");
        text.ShouldNotContain("client_secret");
        text.ShouldNotContain("raw command json");
    }

    [Fact]
    public static void AssociationStatusJsonPreservesClientCandidateFieldsUsedByUi()
    {
        AssociationRoutingStatus source = AssociationStatus();

        string json = ChatBotCliOutputFormatter.FormatAssociationStatus(source, json: true);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement candidate = document.RootElement.GetProperty("candidates")[0];

        document.RootElement.GetProperty("associationId").GetString().ShouldBe(source.AssociationId);
        document.RootElement.GetProperty("correlationId").GetString().ShouldBe(source.CorrelationId);
        candidate.GetProperty("projectId").GetString().ShouldBe(source.Candidates.Single().ProjectId);
        candidate.GetProperty("displayName").GetString().ShouldBe(source.Candidates.Single().DisplayName);
        candidate.GetProperty("confidenceScore").GetDouble().ShouldBe(source.Candidates.Single().ConfidenceScore);
        candidate.GetProperty("rank").GetInt32().ShouldBe(source.Candidates.Single().Rank);
    }

    [Fact]
    public static void AssociationStatusTextPreservesReasonEvidenceAndRedactionMetadata()
    {
        string text = ChatBotCliOutputFormatter.FormatAssociationStatus(AssociationStatus(), json: false);

        text.ShouldContain("reason-codes: explicit-project-identifier-matched");
        text.ShouldContain("next-action-reason-codes: association_ambiguous_routed");
        text.ShouldContain("candidate-reason-codes: rank=1 values=explicit-project-identifier-matched");
        text.ShouldContain("candidate-evidence: rank=1 reference=evidence-ref-1 fingerprint=evidence-fingerprint-1 kind=mailbox-subject redaction-state=metadata_only");
        text.ShouldContain("visibility-state=available");
    }

    private static IChatBotClient ClientReturningAcceptedCommand()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.SubmitAsync(
                Arg.Any<IChatBotCommand>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ChatBotSurfaceOrigin>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "cmd-1",
                CorrelationId = "corr-1",
                TaskId = "op-1",
                LifecycleState = GeneratedLifecycleState.Received,
                AcceptedAt = DateTimeOffset.UnixEpoch,
            }));
        return client;
    }

    private static HexalithChatBotApiException RestrictedPayloadException(int statusCode, string scenario)
        => new(
            $"raw server payload for {scenario}",
            statusCode,
            "restricted project secret bearer-token raw-claim provider-payload",
            new Dictionary<string, IEnumerable<string>>(),
            null);

    private static string[] AssociationArgs(string verb)
        => verb switch
        {
            "associate" =>
            [
                "association", "associate",
                "--association-id", AssociationId,
                "--intake-id", IntakeId,
                "--project-id", ProjectId,
                "--evidence-fingerprint", EvidenceFingerprint,
                "--source-version", "7",
            ],
            "reject" =>
            [
                "association", "reject",
                "--association-id", AssociationId,
                "--intake-id", IntakeId,
                "--evidence-fingerprint", EvidenceFingerprint,
                "--source-version", "7",
            ],
            "defer" =>
            [
                "association", "defer",
                "--association-id", AssociationId,
                "--intake-id", IntakeId,
                "--evidence-fingerprint", EvidenceFingerprint,
                "--source-version", "7",
            ],
            "correct" =>
            [
                "association", "correct",
                "--association-id", AssociationId,
                "--intake-id", IntakeId,
                "--prior-project-id", "project-old",
                "--target-project-id", ProjectId,
                "--predecessor-association-id", "assoc-old",
                "--evidence-fingerprint", EvidenceFingerprint,
                "--source-version", "7",
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
        };

    private static string[] CommandArgs(string command)
        => command switch
        {
            "operation retry" =>
            [
                "operation", "retry",
                "--retry-id", "retry-1",
                "--failed-event-id", "failed-1",
                "--failed-operation-class", "association",
                "--failure-reason-code", "dependency_degraded",
                "--expected-failed-source-version", "2",
            ],
            "approval decide" =>
            [
                "approval", "decide",
                "--project-id", ProjectId,
                "--approval-id", "approval-1",
                "--proposal-id", "proposal-1",
                "--source-message-id", "message-1",
                "--decision", "approve",
                "--expected-approval-source-version", "3",
                "--command-correlation-id", CorrelationId,
                "--decision-id", "decision-1",
            ],
            "ai-action execute" =>
            [
                "ai-action", "execute",
                "--project-id", ProjectId,
                "--proposal-id", "proposal-1",
                "--approval-id", "approval-1",
                "--task-intent-id", "task-intent-1",
                "--source-message-id", "message-1",
                "--requester-id", "requester-1",
                "--command-name", "SendProjectReply",
                "--command-allowlist-version", "allowlist-v1",
                "--expected-approval-source-version", "3",
                "--expected-proposal-source-version", "4",
                "--command-correlation-id", CorrelationId,
                "--execution-id", "execution-1",
                "--transition-id", "transition-1",
                "--source-evidence", "evidence-1",
                "--affected-resource", "resource-1",
                "--recipient", "recipient-1",
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
        };

    private static OperationStatus OperationStatus()
        => new()
        {
            OperationId = AssociationId,
            CommandId = "cmd-1",
            CorrelationId = CorrelationId,
            LifecycleState = GeneratedLifecycleState.Received,
            RetryCount = 1,
            CompletionStatus = GeneratedOperationCompletionStatus.AcceptedProjectionPending,
            AuditStatus = GeneratedOperationAuditStatus.Reconciling,
            SafeNextActions = [ChatBotMessageNextAction.RetryLater],
            AcceptedAt = DateTimeOffset.UnixEpoch,
            LastUpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
        };

    private static OperationAuditHistory OperationAudit()
        => new()
        {
            OperationId = AssociationId,
            AuditStatus = GeneratedOperationAuditStatus.Committed,
        };

    private static AssociationRoutingStatus AssociationStatus()
        => new()
        {
            AssociationId = AssociationId,
            IntakeId = IntakeId,
            LifecycleState = GeneratedLifecycleState.NeedsReview,
            CorrelationId = CorrelationId,
            SourceVersion = 7,
            ConfidenceScore = 0.87,
            RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
            Candidates =
            [
                new GeneratedAssociationCandidate
                {
                    ProjectId = ProjectId,
                    DisplayName = "Safe Project",
                    ConfidenceScore = 0.87,
                    Rank = 1,
                    ReasonCodes = [GeneratedAssociationReasonCode.ExplicitProjectIdentifierMatched],
                    EvidenceRefs =
                    [
                        new GeneratedAssociationEvidenceReference
                        {
                            EvidenceReference = "evidence-ref-1",
                            EvidenceFingerprint = "evidence-fingerprint-1",
                            EvidenceKind = "mailbox-subject",
                            RedactionState = AssociationEvidenceReferenceRedactionState.Metadata_only,
                            VisibilityState = AssociationEvidenceReferenceVisibilityState.Available,
                            FreshnessState = AssociationEvidenceReferenceFreshnessState.Fresh,
                        },
                    ],
                    RequiredEvidenceComplete = true,
                },
            ],
            ReasonCodes = [GeneratedAssociationReasonCode.ExplicitProjectIdentifierMatched],
            NextActionReasonCodes = [ChatBotMessageCode.Association_ambiguous_routed],
        };

    private static ProjectConversationResponse ProjectConversation()
        => new()
        {
            ProjectId = ProjectId,
            CorrelationId = CorrelationId,
            ConversationState = GeneratedLifecycleState.Associated,
            RedactionState = ProjectConversationResponseRedactionState.Metadata_only,
        };

    private static TaskIntentReview TaskReview()
        => new()
        {
            ProjectId = ProjectId,
            TaskIntentId = "task-intent-1",
            Available = true,
            ReasonCode = "available",
            CorrelationId = CorrelationId,
            RedactionState = TaskIntentReviewRedactionState.Metadata_only,
        };
}
