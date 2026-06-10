using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Cli;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Mcp;

using ModelContextProtocol.Server;

using NSubstitute;

using Shouldly;

namespace Hexalith.ChatBot.Mcp.Tests;

using AssociateEmailToProjectCommand = Hexalith.ChatBot.Contracts.Commands.AssociateEmailToProject;
using CorrectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.CorrectEmailProjectAssociation;
using DecideAiActionApprovalCommand = Hexalith.ChatBot.Contracts.Commands.DecideAiActionApproval;
using DeferEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.DeferEmailProjectAssociation;
using ExecuteApprovedAIActionCommand = Hexalith.ChatBot.Contracts.Commands.ExecuteApprovedAIAction;
using RejectEmailProjectAssociationCommand = Hexalith.ChatBot.Contracts.Commands.RejectEmailProjectAssociation;
using RequestFailedWorkflowRetryCommand = Hexalith.ChatBot.Contracts.Commands.RequestFailedWorkflowRetry;
using ChatBotSurfaceOrigin = Hexalith.ChatBot.Contracts.Enums.ChatBotSurfaceOrigin;

public static class ChatBotMcpServiceTests
{
    private const string AssociationId = "01HX0000000000000000000001";
    private const string IntakeId = "01HX0000000000000000000002";
    private const string ProjectId = "project-123";
    private const string EvidenceFingerprint = "ev-fingerprint";
    private const string CorrelationId = "01HX0000000000000000000003";
    private const string TaskId = "01HX0000000000000000000004";

    [Fact]
    public static void ToolCatalogShouldExposeOnlyGovernedMcpToolsWithExplicitMetadata()
    {
        string[] expectedNames =
        [
            "chatbot.association.status",
            "chatbot.association.associate",
            "chatbot.association.reject",
            "chatbot.association.defer",
            "chatbot.association.correct",
            "chatbot.conversation.get",
            "chatbot.task.review",
            "chatbot.operation.retry",
            "chatbot.approval.decide",
            "chatbot.ai_action.execute",
            "chatbot.operation.status",
            "chatbot.operation.audit",
        ];

        ChatBotMcpToolCatalog.Tools.Select(static tool => tool.Name).ShouldBe(expectedNames, ignoreOrder: false);
        foreach (ChatBotMcpToolMetadata tool in ChatBotMcpToolCatalog.Tools)
        {
            tool.Tags.ShouldContain(ChatBotMcpToolMetadata.ExposureMarker);
            tool.Description.ShouldContain(ChatBotMcpToolMetadata.ExposureMarker);
            tool.ContractName.ShouldNotBe("CaptureMailboxMessageIntake");
            tool.ContractName.ShouldNotBe("SetAssociationConfidenceThresholds");
            tool.ContractName.ShouldNotBe("ExecuteLowRiskAIAssistance");
        }
    }

    [Fact]
    public static void ToolCatalogShouldMapOnlyTheBoundedMcpCommandAndQueryContracts()
    {
        var expected = new Dictionary<string, (string ContractName, bool StateChanging)>(StringComparer.Ordinal)
        {
            ["chatbot.association.status"] = ("GetAssociationRoutingStatus", false),
            ["chatbot.association.associate"] = ("AssociateEmailToProject", true),
            ["chatbot.association.reject"] = ("RejectEmailProjectAssociation", true),
            ["chatbot.association.defer"] = ("DeferEmailProjectAssociation", true),
            ["chatbot.association.correct"] = ("CorrectEmailProjectAssociation", true),
            ["chatbot.conversation.get"] = ("GetProjectConversation", false),
            ["chatbot.task.review"] = ("GetTaskIntentReview", false),
            ["chatbot.operation.retry"] = ("RequestFailedWorkflowRetry", true),
            ["chatbot.approval.decide"] = ("DecideAiActionApproval", true),
            ["chatbot.ai_action.execute"] = ("ExecuteApprovedAIAction", true),
            ["chatbot.operation.status"] = ("GetOperationStatus", false),
            ["chatbot.operation.audit"] = ("GetOperationAuditHistory", false),
        };

        ChatBotMcpToolCatalog.Tools.Count.ShouldBe(expected.Count);
        foreach (ChatBotMcpToolMetadata tool in ChatBotMcpToolCatalog.Tools)
        {
            expected.ContainsKey(tool.Name).ShouldBeTrue(tool.Name);
            tool.ContractName.ShouldBe(expected[tool.Name].ContractName);
            tool.StateChanging.ShouldBe(expected[tool.Name].StateChanging);
            tool.RequiredArguments.ShouldNotBeEmpty(tool.Name);
            tool.RequiredArguments.Distinct(StringComparer.Ordinal).Count().ShouldBe(tool.RequiredArguments.Count);
            tool.OptionalArguments.Distinct(StringComparer.Ordinal).Count().ShouldBe(tool.OptionalArguments.Count);
            tool.OptionalArguments.ShouldContain("correlationId");
            tool.OptionalArguments.ShouldContain("taskId");
            tool.OptionalArguments.ShouldContain("tenant");
        }
    }

    [Fact]
    public static void AttributedMcpToolsShouldCarryStableNamesAndMcpExposedDescriptions()
    {
        var attributed = typeof(ChatBotMcpTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => new
            {
                Method = method,
                Tool = method.GetCustomAttribute<McpServerToolAttribute>(),
                Description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>(),
            })
            .Where(item => item.Tool is not null)
            .ToArray();

        attributed.Select(item => item.Tool!.Name).ShouldBe(ChatBotMcpToolCatalog.Tools.Select(static tool => tool.Name), ignoreOrder: true);
        foreach (var item in attributed)
        {
            item.Description.ShouldNotBeNull(item.Method.Name);
            item.Description!.Description.ShouldContain("mcp-exposed");
        }
    }

    [Fact]
    public static void AttributedMcpToolsShouldDeclareSafeDiscoverySemantics()
    {
        var attributed = typeof(ChatBotMcpTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => new
            {
                Method = method,
                Tool = method.GetCustomAttribute<McpServerToolAttribute>(),
            })
            .Where(item => item.Tool is not null)
            .ToDictionary(item => item.Tool!.Name!, StringComparer.Ordinal);

        foreach (ChatBotMcpToolMetadata metadata in ChatBotMcpToolCatalog.Tools)
        {
            attributed.ContainsKey(metadata.Name).ShouldBeTrue(metadata.Name);
            McpServerToolAttribute tool = attributed[metadata.Name].Tool!;
            tool.OpenWorld.ShouldBeFalse(metadata.Name);
            tool.UseStructuredContent.ShouldBeTrue(metadata.Name);
            tool.ReadOnly.ShouldBe(!metadata.StateChanging, metadata.Name);
            tool.Destructive.ShouldBe(metadata.StateChanging, metadata.Name);
            tool.Idempotent.ShouldBeFalse(metadata.Name);
        }
    }

    [Fact]
    public static void AttributedMcpToolParametersShouldMatchCatalogArgumentContract()
    {
        var attributed = typeof(ChatBotMcpTools)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => new
            {
                Method = method,
                Tool = method.GetCustomAttribute<McpServerToolAttribute>(),
            })
            .Where(item => item.Tool is not null)
            .ToDictionary(item => item.Tool!.Name!, StringComparer.Ordinal);

        foreach (ChatBotMcpToolMetadata metadata in ChatBotMcpToolCatalog.Tools)
        {
            MethodInfo method = attributed[metadata.Name].Method;
            ParameterInfo[] toolParameters = method
                .GetParameters()
                .Where(static parameter => parameter.ParameterType != typeof(CancellationToken))
                .ToArray();

            string[] required = toolParameters
                .Where(static parameter => !parameter.HasDefaultValue)
                .Select(static parameter => parameter.Name!)
                .ToArray();
            string[] optional = toolParameters
                .Where(static parameter => parameter.HasDefaultValue)
                .Select(static parameter => parameter.Name!)
                .ToArray();

            required.ShouldBe(metadata.RequiredArguments, ignoreOrder: true, metadata.Name);
            optional.ShouldBe(metadata.OptionalArguments, ignoreOrder: true, metadata.Name);
        }
    }

    [Theory]
    [InlineData("chatbot.association.associate", typeof(AssociateEmailToProjectCommand))]
    [InlineData("chatbot.association.reject", typeof(RejectEmailProjectAssociationCommand))]
    [InlineData("chatbot.association.defer", typeof(DeferEmailProjectAssociationCommand))]
    [InlineData("chatbot.association.correct", typeof(CorrectEmailProjectAssociationCommand))]
    [InlineData("chatbot.operation.retry", typeof(RequestFailedWorkflowRetryCommand))]
    [InlineData("chatbot.approval.decide", typeof(DecideAiActionApprovalCommand))]
    [InlineData("chatbot.ai_action.execute", typeof(ExecuteApprovedAIActionCommand))]
    public static async Task StateChangingToolsSubmitTypedCommandsWithMcpOrigin(string toolName, Type expectedCommandType)
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(Invocation(toolName, ArgumentsFor(toolName)), TestContext.Current.CancellationToken);

        result.GetProperty("outcome").GetString().ShouldBe("command-accepted");
        result.GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        result.GetProperty("auditStatus").GetString().ShouldBe("reconciling");
        await client.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == expectedCommandType),
            null,
            null,
            ChatBotSurfaceOrigin.Mcp,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public static async Task ReadToolsUseClientFacadeReadMethodsOnly()
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
        var service = new ChatBotMcpService(client);

        await service.InvokeAsync(Invocation("chatbot.association.status", WithTrace(("associationId", AssociationId))), TestContext.Current.CancellationToken);
        await service.InvokeAsync(Invocation("chatbot.conversation.get", WithTrace(("projectId", ProjectId))), TestContext.Current.CancellationToken);
        await service.InvokeAsync(Invocation("chatbot.task.review", WithTrace(("projectId", ProjectId), ("taskIntentId", "task-intent-1"))), TestContext.Current.CancellationToken);
        await service.InvokeAsync(Invocation("chatbot.operation.status", WithTrace(("operationId", AssociationId))), TestContext.Current.CancellationToken);
        await service.InvokeAsync(Invocation("chatbot.operation.audit", WithTrace(("operationId", AssociationId))), TestContext.Current.CancellationToken);

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
    }

    [Theory]
    [InlineData("chatbot.association.stats", "mcp.tool.unknown")]
    [InlineData("chatbot.association.associate", "mcp.argument.missing")]
    public static async Task BoundaryValidationReturnsSafeMetadataOnlyDenials(string toolName, string expectedCode)
    {
        var service = new ChatBotMcpService(Substitute.For<IChatBotClient>());
        IReadOnlyDictionary<string, object?> arguments = toolName == "chatbot.association.associate"
            ? new Dictionary<string, object?>(StringComparer.Ordinal) { ["associationId"] = AssociationId }
            : new Dictionary<string, object?>(StringComparer.Ordinal);

        JsonElement result = await service.InvokeAsync(Invocation(toolName, arguments), TestContext.Current.CancellationToken);

        result.GetProperty("outcome").GetString().ShouldBe("denied");
        result.GetProperty("code").GetString().ShouldBe(expectedCode);
        result.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        result.GetProperty("safeSuggestion").GetString().ShouldNotBeNullOrWhiteSpace();
        result.GetRawText().ShouldNotContain("restricted project");
        result.GetRawText().ShouldNotContain("bearer-token");
        result.GetRawText().ShouldNotContain("raw-claim");
        result.GetRawText().ShouldNotContain("provider-payload");
    }

    [Fact]
    public static async Task InvalidEnumAndUnsupportedArgumentFailClosedWithSafeSuggestion()
    {
        var service = new ChatBotMcpService(Substitute.For<IChatBotClient>());

        JsonElement invalidEnum = await service.InvokeAsync(
            Invocation("chatbot.approval.decide", ArgumentsFor("chatbot.approval.decide", ("decision", "ship-it"))),
            TestContext.Current.CancellationToken);
        JsonElement unsupportedArgument = await service.InvokeAsync(
            Invocation("chatbot.operation.status", WithTrace(("operationId", AssociationId), ("projectName", "restricted project"))),
            TestContext.Current.CancellationToken);

        invalidEnum.GetProperty("code").GetString().ShouldBe("mcp.argument.invalid-enum");
        invalidEnum.GetProperty("safeSuggestion").GetString().ShouldBe("Use approve, reject, request-revision, or cancel.");
        unsupportedArgument.GetProperty("code").GetString().ShouldBe("mcp.argument.unsupported");
        unsupportedArgument.GetRawText().ShouldNotContain("restricted project");
    }

    [Fact]
    public static async Task InvalidNumberAndListArgumentsFailClosedBeforeCommandSubmission()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        var service = new ChatBotMcpService(client);

        JsonElement invalidNumber = await service.InvokeAsync(
            Invocation("chatbot.operation.retry", ArgumentsFor("chatbot.operation.retry", ("expectedFailedSourceVersion", "not-a-number"))),
            TestContext.Current.CancellationToken);
        JsonElement invalidList = await service.InvokeAsync(
            Invocation("chatbot.ai_action.execute", ArgumentsFor("chatbot.ai_action.execute", ("sourceEvidenceReferences", 123))),
            TestContext.Current.CancellationToken);

        invalidNumber.GetProperty("code").GetString().ShouldBe("mcp.argument.invalid-number");
        invalidNumber.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        invalidList.GetProperty("code").GetString().ShouldBe("mcp.argument.invalid-list");
        invalidList.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        await client.DidNotReceive().SubmitAsync(
            Arg.Any<IChatBotCommand>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<ChatBotSurfaceOrigin>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public static async Task JsonObjectArgumentsFailClosedBeforeCommandSubmissionWithoutEchoingPayload()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        using JsonDocument objectPayload = JsonDocument.Parse("""{"projectName":"restricted project","candidateEvidence":"secret"}""");
        using JsonDocument listPayload = JsonDocument.Parse("""["evidence-1",{"fileName":"restricted.pdf"}]""");
        var service = new ChatBotMcpService(client);

        JsonElement invalidString = await service.InvokeAsync(
            Invocation("chatbot.association.associate", ArgumentsFor(
                "chatbot.association.associate",
                ("projectId", objectPayload.RootElement.Clone()))),
            TestContext.Current.CancellationToken);
        JsonElement invalidListMember = await service.InvokeAsync(
            Invocation("chatbot.ai_action.execute", ArgumentsFor(
                "chatbot.ai_action.execute",
                ("sourceEvidenceReferences", listPayload.RootElement.Clone()))),
            TestContext.Current.CancellationToken);

        invalidString.GetProperty("code").GetString().ShouldBe("mcp.argument.invalid-string");
        invalidString.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        invalidString.GetRawText().ShouldNotContain("restricted project");
        invalidString.GetRawText().ShouldNotContain("candidateEvidence");
        invalidListMember.GetProperty("code").GetString().ShouldBe("mcp.argument.invalid-list");
        invalidListMember.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        invalidListMember.GetRawText().ShouldNotContain("restricted.pdf");
        await client.DidNotReceive().SubmitAsync(
            Arg.Any<IChatBotCommand>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<ChatBotSurfaceOrigin>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(401, "stale credential")]
    [InlineData(403, "revoked grant")]
    [InlineData(403, "wrong surface")]
    [InlineData(403, "tenant mismatch")]
    [InlineData(404, "safe not found")]
    public static async Task BackendDenialsRemainMetadataOnly(int statusCode, string scenario)
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.SubmitAsync(
                Arg.Any<IChatBotCommand>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ChatBotSurfaceOrigin>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<CommandSubmissionResponse>>(_ => throw RestrictedPayloadException(statusCode, scenario));
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(Invocation("chatbot.association.reject", ArgumentsFor("chatbot.association.reject")), TestContext.Current.CancellationToken);

        result.GetProperty("outcome").GetString().ShouldBe("denied");
        result.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        result.GetRawText().ShouldNotContain("restricted project");
        result.GetRawText().ShouldNotContain("bearer-token");
        result.GetRawText().ShouldNotContain("raw-claim");
        result.GetRawText().ShouldNotContain("provider-payload");
    }

    [Fact]
    public static async Task TypedBackendProblemDetailsDenialPreservesCatalogMetadataOnly()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.SubmitAsync(
                Arg.Any<IChatBotCommand>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ChatBotSurfaceOrigin>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<CommandSubmissionResponse>>(_ => throw TypedProblemException());
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(
            Invocation("chatbot.association.reject", ArgumentsFor("chatbot.association.reject", ("correlationId", CorrelationId), ("taskId", TaskId))),
            TestContext.Current.CancellationToken);

        result.GetProperty("outcome").GetString().ShouldBe("denied");
        result.GetProperty("category").GetString().ShouldBe("authorization_denied");
        result.GetProperty("code").GetString().ShouldBe("authorization_denied");
        result.GetProperty("message").GetString().ShouldBe("Access is denied.");
        result.GetProperty("correlationId").GetString().ShouldBe(CorrelationId);
        result.GetProperty("taskId").GetString().ShouldBe(TaskId);
        result.GetProperty("clientAction").GetString().ShouldBe("request-access");
        result.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        result.GetRawText().ShouldNotContain("restricted project");
        result.GetRawText().ShouldNotContain("bearer-token");
        result.GetRawText().ShouldNotContain("raw-claim");
        result.GetRawText().ShouldNotContain("provider-payload");
    }

    [Fact]
    public static async Task TenantArgumentIsFilterIntentOnlyAndIsNotForwardedAsAuthority()
    {
        IChatBotClient client = ClientReturningAcceptedCommand();
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(
            Invocation("chatbot.association.associate", ArgumentsFor("chatbot.association.associate", ("tenant", "tenant-alpha"), ("correlationId", CorrelationId), ("taskId", TaskId))),
            TestContext.Current.CancellationToken);

        await client.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == typeof(AssociateEmailToProjectCommand)),
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Mcp,
            Arg.Any<CancellationToken>());
        result.GetRawText().ShouldNotContain("tenant-alpha");
    }

    [Fact]
    public static void OperationStatusFormatterPreservesProjectionPendingPartialSuccess()
    {
        JsonElement result = ChatBotMcpResultFormatter.FormatOperationStatus(OperationStatus());

        result.GetProperty("completionStatus").GetString().ShouldBe("accepted-projection-pending");
        result.GetProperty("auditStatus").GetString().ShouldBe("reconciling");
        result.GetProperty("partialSuccess").GetBoolean().ShouldBeTrue();
        result.GetRawText().ShouldNotContain("\"outcome\":\"success\"", Case.Insensitive);
        result.GetRawText().ShouldNotContain("\"completionStatus\":\"completed\"", Case.Insensitive);
        result.GetRawText().ShouldNotContain("done", Case.Insensitive);
    }

    [Fact]
    public static async Task ReadToolResultsEmitGovernedWireNameEnumsNotRawOrdinals()
    {
        IChatBotClient client = ClientReturningReads();
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(
            Invocation("chatbot.association.status", WithTrace(("associationId", AssociationId))),
            TestContext.Current.CancellationToken);

        // Enums must surface as their governed EnumMember wire names, identical to the operation-status surface,
        // never as version-brittle integer ordinals.
        result.GetProperty("lifecycleState").GetString().ShouldBe("NeedsReview");
        result.GetProperty("redactionState").GetString().ShouldBe("metadata_only");
        result.GetProperty("reasonCodes")[0].GetString().ShouldBe("explicit-project-identifier-matched");
        result.GetProperty("lifecycleState").ValueKind.ShouldBe(JsonValueKind.String);
        result.GetProperty("redactionState").ValueKind.ShouldBe(JsonValueKind.String);
        result.GetRawText().ShouldNotContain("\"redactionState\":0");
        result.GetRawText().ShouldNotContain("\"lifecycleState\":5");
    }

    [Fact]
    public static async Task TransportAndUnexpectedFailuresBecomeMetadataOnlySafeDenials()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.SubmitAsync(
                Arg.Any<IChatBotCommand>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<ChatBotSurfaceOrigin>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<CommandSubmissionResponse>>(_ =>
                throw new System.Net.Http.HttpRequestException("connection refused to chatbot-backend:8080 bearer-token raw-claim"));
        var service = new ChatBotMcpService(client);

        JsonElement result = await service.InvokeAsync(
            Invocation("chatbot.association.reject", ArgumentsFor("chatbot.association.reject")),
            TestContext.Current.CancellationToken);

        result.GetProperty("outcome").GetString().ShouldBe("denied");
        result.GetProperty("detailsVisibility").GetString().ShouldBe("metadata-only");
        result.GetProperty("message").GetString().ShouldBe("Request denied.");
        // The raw transport message (endpoint topology, fabricated secrets) must never reach the MCP client.
        result.GetRawText().ShouldNotContain("chatbot-backend");
        result.GetRawText().ShouldNotContain("bearer-token");
        result.GetRawText().ShouldNotContain("raw-claim");
    }

    [Fact]
    public static async Task CooperativeCancellationPropagatesAndIsNotMaskedAsDenial()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        var cancelled = new CancellationToken(canceled: true);
        _ = client.GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns<Task<AssociationRoutingStatus>>(_ => throw new OperationCanceledException(cancelled));
        var service = new ChatBotMcpService(client);

        await Should.ThrowAsync<OperationCanceledException>(
            () => service.InvokeAsync(
                Invocation("chatbot.association.status", WithTrace(("associationId", AssociationId))),
                cancelled));
    }

    [Fact]
    public static async Task FocusedCliAndMcpParityShouldConstructEquivalentWorkflowCalls()
    {
        await AssertReadParityAsync(
            ["association", "status", "--association-id", AssociationId],
            "chatbot.association.status",
            WithTrace(("associationId", AssociationId)),
            static client => client.Received(1).GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>()));
        await AssertWriteParityAsync(
            AssociationCliArgs("associate"),
            "chatbot.association.associate",
            typeof(AssociateEmailToProjectCommand));
        await AssertWriteParityAsync(
            CommandCliArgs("operation retry"),
            "chatbot.operation.retry",
            typeof(RequestFailedWorkflowRetryCommand));
        await AssertWriteParityAsync(
            CommandCliArgs("approval decide"),
            "chatbot.approval.decide",
            typeof(DecideAiActionApprovalCommand));
        await AssertWriteParityAsync(
            CommandCliArgs("ai-action execute"),
            "chatbot.ai_action.execute",
            typeof(ExecuteApprovedAIActionCommand));
        await AssertReadParityAsync(
            ["operation", "status", "--operation-id", AssociationId],
            "chatbot.operation.status",
            WithTrace(("operationId", AssociationId)),
            static client => client.Received(1).GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>()));
        await AssertReadParityAsync(
            ["operation", "audit", "--operation-id", AssociationId],
            "chatbot.operation.audit",
            WithTrace(("operationId", AssociationId)),
            static client => client.Received(1).GetOperationAuditHistoryAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>()));
    }

    private static async Task AssertWriteParityAsync(string[] cliArgs, string mcpToolName, Type expectedCommandType)
    {
        IChatBotClient cliClient = ClientReturningAcceptedCommand();
        IChatBotClient mcpClient = ClientReturningAcceptedCommand();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            [.. cliArgs, "--correlation-id", CorrelationId, "--task-id", TaskId],
            cliClient,
            output,
            error,
            TestContext.Current.CancellationToken).ConfigureAwait(false);
        JsonElement mcp = await new ChatBotMcpService(mcpClient)
            .InvokeAsync(Invocation(mcpToolName, ArgumentsFor(mcpToolName, ("correlationId", CorrelationId), ("taskId", TaskId))), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        exitCode.ShouldBe(0);
        mcp.GetProperty("outcome").GetString().ShouldBe("command-accepted");
        await cliClient.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == expectedCommandType),
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Cli,
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
        await mcpClient.Received(1).SubmitAsync(
            Arg.Is<IChatBotCommand>(command => command.GetType() == expectedCommandType),
            CorrelationId,
            TaskId,
            ChatBotSurfaceOrigin.Mcp,
            Arg.Any<CancellationToken>()).ConfigureAwait(false);
    }

    private static async Task AssertReadParityAsync(
        string[] cliArgs,
        string mcpToolName,
        IReadOnlyDictionary<string, object?> mcpArguments,
        Func<IChatBotClient, Task> assertRead)
    {
        IChatBotClient cliClient = ClientReturningReads();
        IChatBotClient mcpClient = ClientReturningReads();
        using var output = new StringWriter();
        using var error = new StringWriter();

        int exitCode = await ChatBotCliCommands.InvokeAsync(
            [.. cliArgs, "--correlation-id", CorrelationId, "--task-id", TaskId],
            cliClient,
            output,
            error,
            TestContext.Current.CancellationToken).ConfigureAwait(false);
        _ = await new ChatBotMcpService(mcpClient)
            .InvokeAsync(Invocation(mcpToolName, mcpArguments), TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        exitCode.ShouldBe(0);
        await assertRead(cliClient).ConfigureAwait(false);
        await assertRead(mcpClient).ConfigureAwait(false);
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
                LifecycleState = LifecycleState.Received,
                AcceptedAt = DateTimeOffset.UnixEpoch,
            }));
        return client;
    }

    private static IChatBotClient ClientReturningReads()
    {
        IChatBotClient client = Substitute.For<IChatBotClient>();
        _ = client.GetAssociationRoutingStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AssociationStatus()));
        _ = client.GetOperationStatusAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OperationStatus()));
        _ = client.GetOperationAuditHistoryAsync(AssociationId, CorrelationId, TaskId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(OperationAudit()));
        return client;
    }

    private static ChatBotMcpInvocation Invocation(string toolName, IReadOnlyDictionary<string, object?> arguments)
        => ChatBotMcpInvocation.Create(toolName, arguments);

    private static IReadOnlyDictionary<string, object?> WithTrace(params (string Key, object? Value)[] values)
        => Args([.. values, ("correlationId", CorrelationId), ("taskId", TaskId)]);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] values)
        => values.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, object?> ArgumentsFor(string toolName, params (string Key, object? Value)[] overrides)
    {
        Dictionary<string, object?> values = toolName switch
        {
            "chatbot.association.associate" => new(StringComparer.Ordinal)
            {
                ["associationId"] = AssociationId,
                ["intakeId"] = IntakeId,
                ["projectId"] = ProjectId,
                ["evidenceFingerprint"] = EvidenceFingerprint,
                ["sourceVersion"] = 7L,
                ["schemaVersion"] = "chatbot.association-decision.v1",
            },
            "chatbot.association.reject" => new(StringComparer.Ordinal)
            {
                ["associationId"] = AssociationId,
                ["intakeId"] = IntakeId,
                ["evidenceFingerprint"] = EvidenceFingerprint,
                ["sourceVersion"] = 7L,
                ["schemaVersion"] = "chatbot.association-decision.v1",
            },
            "chatbot.association.defer" => new(StringComparer.Ordinal)
            {
                ["associationId"] = AssociationId,
                ["intakeId"] = IntakeId,
                ["evidenceFingerprint"] = EvidenceFingerprint,
                ["sourceVersion"] = 7L,
                ["schemaVersion"] = "chatbot.association-decision.v1",
            },
            "chatbot.association.correct" => new(StringComparer.Ordinal)
            {
                ["associationId"] = AssociationId,
                ["intakeId"] = IntakeId,
                ["priorProjectId"] = "project-old",
                ["targetProjectId"] = ProjectId,
                ["predecessorAssociationId"] = "assoc-previous",
                ["evidenceFingerprint"] = EvidenceFingerprint,
                ["sourceVersion"] = 7L,
                ["schemaVersion"] = "chatbot.association-correction.v1",
            },
            "chatbot.operation.retry" => new(StringComparer.Ordinal)
            {
                ["retryId"] = "retry-1",
                ["failedEventId"] = "failed-event-1",
                ["failedOperationClass"] = "projection",
                ["failureReasonCode"] = "dependency_degraded",
                ["expectedFailedSourceVersion"] = 9L,
            },
            "chatbot.approval.decide" => new(StringComparer.Ordinal)
            {
                ["projectId"] = ProjectId,
                ["approvalId"] = "approval-1",
                ["proposalId"] = "proposal-1",
                ["sourceMessageId"] = "message-1",
                ["decision"] = "approve",
                ["expectedApprovalSourceVersion"] = 3L,
                ["commandCorrelationId"] = CorrelationId,
                ["decisionId"] = "decision-1",
            },
            "chatbot.ai_action.execute" => new(StringComparer.Ordinal)
            {
                ["projectId"] = ProjectId,
                ["proposalId"] = "proposal-1",
                ["approvalId"] = "approval-1",
                ["taskIntentId"] = "task-intent-1",
                ["sourceMessageId"] = "message-1",
                ["requesterId"] = "requester-1",
                ["commandName"] = "SendProjectReply",
                ["commandAllowlistVersion"] = "allowlist-v1",
                ["expectedApprovalSourceVersion"] = 3L,
                ["expectedProposalSourceVersion"] = 4L,
                ["commandCorrelationId"] = CorrelationId,
                ["executionId"] = "execution-1",
                ["transitionId"] = "transition-1",
                ["sourceEvidenceReferences"] = new[] { "evidence-1" },
                ["affectedResourceReferences"] = new[] { "resource-1" },
                ["recipientReferences"] = new[] { "recipient-1" },
            },
            _ => throw new ArgumentOutOfRangeException(nameof(toolName), toolName, null),
        };

        foreach ((string key, object? value) in overrides)
        {
            values[key] = value;
        }

        return values;
    }

    private static string[] AssociationCliArgs(string verb)
        => verb switch
        {
            "associate" =>
            [
                "association",
                "associate",
                "--association-id", AssociationId,
                "--intake-id", IntakeId,
                "--project-id", ProjectId,
                "--evidence-fingerprint", EvidenceFingerprint,
                "--source-version", "7",
                "--schema-version", "chatbot.association-decision.v1",
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
        };

    private static string[] CommandCliArgs(string command)
        => command switch
        {
            "operation retry" =>
            [
                "operation",
                "retry",
                "--retry-id", "retry-1",
                "--failed-event-id", "failed-event-1",
                "--failed-operation-class", "projection",
                "--failure-reason-code", "dependency_degraded",
                "--expected-failed-source-version", "9",
            ],
            "approval decide" =>
            [
                "approval",
                "decide",
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
                "ai-action",
                "execute",
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

    private static HexalithChatBotApiException RestrictedPayloadException(int statusCode, string scenario)
        => new(
            $"raw server payload containing restricted project for {scenario}",
            statusCode,
            response: "restricted project secret bearer-token raw-claim provider-payload",
            headers: new Dictionary<string, IEnumerable<string>>(),
            innerException: null);

    private static HexalithChatBotApiException<ProblemDetails> TypedProblemException()
        => new(
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

    private static OperationStatus OperationStatus()
        => new()
        {
            OperationId = AssociationId,
            CommandId = "cmd-1",
            CorrelationId = CorrelationId,
            LifecycleState = LifecycleState.Received,
            RetryCount = 1,
            CompletionStatus = OperationCompletionStatus.AcceptedProjectionPending,
            AuditStatus = OperationAuditStatus.Reconciling,
            SafeNextActions = [ChatBotMessageNextAction.RetryLater],
            AcceptedAt = DateTimeOffset.UnixEpoch,
            LastUpdatedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
        };

    private static OperationAuditHistory OperationAudit()
        => new()
        {
            OperationId = AssociationId,
            AuditStatus = OperationAuditStatus.Committed,
        };

    private static AssociationRoutingStatus AssociationStatus()
        => new()
        {
            AssociationId = AssociationId,
            IntakeId = IntakeId,
            LifecycleState = LifecycleState.NeedsReview,
            CorrelationId = CorrelationId,
            SourceVersion = 7,
            ConfidenceScore = 0.87,
            RedactionState = AssociationRoutingStatusRedactionState.Metadata_only,
            ReasonCodes = [AssociationReasonCode.ExplicitProjectIdentifierMatched],
        };

    private static ProjectConversationResponse ProjectConversation()
        => new()
        {
            ProjectId = ProjectId,
            CorrelationId = CorrelationId,
            ConversationState = LifecycleState.Associated,
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
