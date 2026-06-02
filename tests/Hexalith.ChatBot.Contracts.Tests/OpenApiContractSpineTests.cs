using System.Text.RegularExpressions;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static partial class OpenApiContractSpineTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void ContractSpineShouldDeclareOpenApiFoundation()
    {
        YamlMappingNode root = LoadContract();

        Scalar(root, "openapi").ShouldBe("3.1.0");
        Mapping(root, "info").Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("title");
        Sequence(root, "servers").Children.OfType<YamlMappingNode>().Select(static server => Scalar(server, "url")).ShouldContain("/api/v1");
        ShouldContainAll(
            Mapping(root, "components").Children.Keys.Select(static key => ((YamlScalarNode)key).Value.ShouldNotBeNull()).ToArray(),
            ["securitySchemes", "headers", "parameters", "responses", "schemas"]);
    }

    [Fact]
    public static void ContractSpineShouldExposeCommandSubmissionOperationStatusAndProjectConversation()
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode commandOperation = Operation(root, "/api/v1/commands", "post");
        YamlMappingNode statusOperation = Operation(root, "/api/v1/operations/{operationId}", "get");
        YamlMappingNode conversationOperation = Operation(root, "/api/v1/projects/{projectId}/conversation", "get");

        Scalar(commandOperation, "operationId").ShouldBe("SubmitCommand");
        Scalar(statusOperation, "operationId").ShouldBe("GetOperationStatus");
        Scalar(conversationOperation, "operationId").ShouldBe("GetProjectConversation");
        commandOperation.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value).ShouldContain("x-hexalith-command-submission");
        statusOperation.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value).ShouldContain("x-hexalith-operation-status");
        conversationOperation.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value).ShouldContain("x-hexalith-project-conversation");
        commandOperation.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value).ShouldNotContain("x-hexalith-command-gateway-stage");

        string contractText = File.ReadAllText(ContractPath);
        contractText.ShouldNotContain("CommandGateway", Case.Insensitive);
        contractText.ShouldNotContain("Dapr", Case.Insensitive);
        contractText.ShouldNotContain("EventStore envelope", Case.Insensitive);
    }

    [Fact]
    public static void CommandSubmissionOperationShouldCoverHappyPathAndCriticalFailures()
    {
        YamlMappingNode operation = Operation(LoadContract(), "/api/v1/commands", "post");
        string[] responseCodes = RequiredKeys(Mapping(operation, "responses"));

        ShouldContainAll(responseCodes, ["202", "400", "401", "403", "409", "500"]);
    }

    [Fact]
    public static void ContractSpineShouldDeclareRequiredSharedSchemasHeadersAndResponses()
    {
        YamlMappingNode components = Mapping(LoadContract(), "components");

        ShouldContainAll(RequiredKeys(Mapping(components, "headers")), [
            "CorrelationId",
            "TaskId",
            "RetryAfter",
        ]);

        ShouldContainAll(RequiredKeys(Mapping(components, "responses")), [
            "AcceptedCommand",
            "OperationStatus",
            "ValidationFailure",
            "SafeAuthorizationDenial401",
            "SafeAuthorizationDenial403",
            "Conflict",
            "InternalFailure",
        ]);

        ShouldContainAll(RequiredKeys(Mapping(components, "schemas")), [
            "AcceptedCommand",
            "ProblemDetails",
            "ProblemDetailsDetails",
            "CommandSubmissionRequest",
            "CommandSubmissionResponse",
            "CaptureTaskIntent",
            "OperationStatus",
            "OperationCompletionStatus",
            "OperationAuditStatus",
            "LifecycleState",
            "ChatBotHealthStatus",
            "RiskClass",
            "ActorType",
            "ThresholdBand",
            "ProjectConversationResponse",
            "ProjectConversationItem",
            "ProjectConversationItemClassification",
            "ProjectConversationDetectedIntent",
            "TaskIntentRecord",
            "TaskIntentSourceEvidenceOffset",
            "TaskIntentState",
            "ProjectConversationAiSummaryProvenance",
            "ProjectConversationReviewHistoryEntry",
            "ProjectConversationItemStatusSummary",
            "ProjectConversationItemStatusFacet",
            "ProjectConversationClassificationKind",
            "ProjectConversationDetectedActionKind",
            "ProjectConversationCursorPage",
            "ProjectConversationReadStatus",
            "ProjectConversationItemKind",
            "ProjectConversationActorKind",
            "ApprovalEventKind",
            "ApprovalStatus",
            "ApprovalDecisionKind",
            "ApprovalEvidenceFreshness",
            "DecideAiActionApproval",
            "ExecuteLowRiskAIAssistance",
            "ExecuteApprovedAIAction",
            "CreateOutboundDraft",
            "OutboundDraftContent",
            "ApprovedAiActionExecutionRecord",
            "LowRiskAiAssistanceKind",
            "LowRiskAiAssistanceExecutionRecord",
        ]);
    }

    [Fact]
    public static void ApprovalDecisionSchemaShouldKeepTenantAndAuthorityServerOwned()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode decision = Mapping(schemas, "DecideAiActionApproval");
        string contract = decision.ToString();

        ShouldContainAll(RequiredKeys(Mapping(decision, "properties")), [
            "projectId",
            "approvalId",
            "proposalId",
            "sourceMessageId",
            "decision",
            "expectedApprovalSourceVersion",
            "correlationId",
            "decisionId",
            "rationaleRedactionState",
        ]);
        contract.ShouldNotContain("tenantId", Case.Insensitive);
        contract.ShouldNotContain("actorIdentity", Case.Insensitive);
        contract.ShouldNotContain("authority", Case.Insensitive);
    }

    [Fact]
    public static void LowRiskAiExecutionSchemasShouldBeMetadataOnly()
    {
        string contract = File.ReadAllText(ContractPath);
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        RequiredKeys(Mapping(schemas, "ExecuteLowRiskAIAssistance")).ShouldNotBeEmpty();
        RequiredKeys(Mapping(schemas, "LowRiskAiAssistanceExecutionRecord")).ShouldNotBeEmpty();

        contract.ShouldContain("ExecuteLowRiskAIAssistance");
        contract.ShouldContain("LowRiskAiAssistanceExecutionRecord");
        contract.ShouldContain("summarize-visible-context");
        contract.ShouldNotContain("prompt:");
        contract.ShouldNotContain("completion:");
        contract.ShouldNotContain("providerPayload");
        contract.ShouldNotContain("rawFileContent");
        contract.ShouldNotContain("localPath");
    }

    [Fact]
    public static void ApprovedAiExecutionSchemasShouldBeMetadataOnlyAndServerAuthorityOwned()
    {
        string contract = File.ReadAllText(ContractPath);
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode execution = Mapping(schemas, "ExecuteApprovedAIAction");

        ShouldContainAll(RequiredKeys(Mapping(execution, "properties")), [
            "projectId",
            "proposalId",
            "approvalId",
            "taskIntentId",
            "sourceMessageId",
            "requesterId",
            "commandName",
            "commandAllowlistVersion",
            "expectedApprovalSourceVersion",
            "expectedProposalSourceVersion",
            "correlationId",
            "executionId",
            "transitionId",
        ]);

        contract.ShouldContain("ExecuteApprovedAIAction");
        contract.ShouldContain("ApprovedAiActionExecutionRecord");
        contract.ShouldContain("Project.AppendConversationMessage");
        execution.ToString().ShouldNotContain("tenantId", Case.Insensitive);
        execution.ToString().ShouldNotContain("actorIdentity", Case.Insensitive);
        contract.ShouldNotContain("rawPrompt", Case.Insensitive);
        contract.ShouldNotContain("providerPayload", Case.Insensitive);
        contract.ShouldNotContain("rawFileContent", Case.Insensitive);
        contract.ShouldNotContain("rawEmailBody", Case.Insensitive);
    }

    [Fact]
    public static void LifecycleStateSchemaShouldUseCanonicalValuesOnly()
    {
        YamlMappingNode lifecycleState = Mapping(Mapping(Mapping(LoadContract(), "components"), "schemas"), "LifecycleState");

        Sequence(lifecycleState, "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(
                [
                    "Received",
                    "Proposed",
                    "Associated",
                    "Rejected",
                    "Deferred",
                    "NeedsReview",
                    "Failed",
                    "Skipped",
                    "Corrected",
                    "Correcting",
                    "Correction-delayed",
                ],
                ignoreOrder: false);

        string contractText = lifecycleState.ToString();
        foreach (string legacyState in new[] { "pending", "accepted", "running", "succeeded", "cancelled" })
        {
            Regex.IsMatch(contractText, $"^\\s*- {Regex.Escape(legacyState)}\\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)
                .ShouldBeFalse();
            contractText.ShouldNotContain($"lifecycleState: {legacyState}", Case.Sensitive);
        }
    }

    [Fact]
    public static void ChatBotHealthStatusSchemaShouldUseStableLowercaseValues()
    {
        YamlMappingNode healthStatus = Mapping(Mapping(Mapping(LoadContract(), "components"), "schemas"), "ChatBotHealthStatus");

        Sequence(healthStatus, "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["healthy", "degraded", "failed", "unknown"], ignoreOrder: false);
    }

    [Fact]
    public static void ProjectConversationClassificationSchemasShouldUseStableSafeWireValues()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        Sequence(Mapping(schemas, "ProjectConversationClassificationKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["informational", "actionable"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationDetectedActionKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["request-information", "request-action", "request-decision", "inform-only"], ignoreOrder: false);

        string contract = File.ReadAllText(ContractPath);
        contract.ShouldContain("classification:");
        contract.ShouldContain("aiSummaryProvenance:");
        contract.ShouldContain("reviewHistory:");
        contract.ShouldNotContain("rawEmailBody", Case.Insensitive);
        contract.ShouldNotContain("auditEnvelope", Case.Insensitive);
    }

    [Fact]
    public static void OperationStatusSchemaShouldExposeFr80MetadataAndNeverFalseDone()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode status = Mapping(schemas, "OperationStatus");
        string[] required = Sequence(status, "required").Children.OfType<YamlScalarNode>().Select(static value => value.Value.ShouldNotBeNull()).ToArray();

        ShouldContainAll(
            required,
            [
                "operationId",
                "commandId",
                "correlationId",
                "lifecycleState",
                "retryCount",
                "completionStatus",
                "auditStatus",
                "partialOutputs",
                "safeNextActions",
                "acceptedAt",
                "lastUpdatedAt",
            ]);

        Sequence(Mapping(schemas, "OperationCompletionStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["accepted-projection-pending", "completed", "failed"], ignoreOrder: false);
        Sequence(Mapping(schemas, "OperationAuditStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["committed", "reconciling"], ignoreOrder: false);
    }

    [Fact]
    public static void LocalReferencesShouldResolveWithinContractSpine()
    {
        YamlMappingNode root = LoadContract();
        string text = File.ReadAllText(ContractPath);
        string[] references = LocalReferencePattern().Matches(text).Select(static match => match.Groups["ref"].Value).Distinct(StringComparer.Ordinal).ToArray();

        references.ShouldNotBeEmpty();
        foreach (string reference in references)
        {
            ResolveLocalReference(root, reference).ShouldNotBeNull(reference);
        }
    }

    [Fact]
    public static void ClientControlledInputsShouldNotCarryTenantAuthority()
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode operation = Operation(root, "/api/v1/commands", "post");

        Sequence(operation, "parameters").Children.OfType<YamlMappingNode>()
            .Select(ResolveParameterName)
            .ShouldNotContain(static name => TenantAuthorityPattern().IsMatch(name));

        YamlMappingNode requestSchema = Mapping(Mapping(Mapping(Mapping(operation, "requestBody"), "content"), "application/json"), "schema");
        string schemaName = requestSchema.Children[new YamlScalarNode("$ref")].ToString().Split('/').Last();
        AllSchemaPropertyNames(root, schemaName).ShouldNotContain(static name => TenantAuthorityPattern().IsMatch(name));
    }

    [Fact]
    public static void HexalithExtensionsShouldUseOnlyHexalithPrefix()
    {
        string[] extensionKeys = ExtensionKeyPattern()
            .Matches(File.ReadAllText(ContractPath))
            .Select(static match => match.Groups["key"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        extensionKeys.ShouldNotBeEmpty();
        extensionKeys.ShouldAllBe(static key => key.StartsWith("x-hexalith-", StringComparison.Ordinal));
    }

    [Fact]
    public static void ExamplesShouldConformToContractIdentifierAndProblemStatusShape()
    {
        YamlMappingNode examples = Mapping(Mapping(LoadContract(), "components"), "examples");

        foreach (YamlMappingNode example in examples.Children.Values.OfType<YamlMappingNode>())
        {
            YamlMappingNode value = Mapping(example, "value");
            AssertUlidPropertyIfPresent(value, "commandId");
            AssertUlidPropertyIfPresent(value, "correlationId");
            AssertUlidPropertyIfPresent(value, "taskId");

            if (value.Children.TryGetValue(new YamlScalarNode("status"), out YamlNode? statusNode))
            {
                int status = int.Parse(statusNode.ShouldBeOfType<YamlScalarNode>().Value.ShouldNotBeNull(), System.Globalization.CultureInfo.InvariantCulture);
                status.ShouldBeOneOf(400, 401, 403, 409, 500, 503);
                Scalar(Mapping(value, "details"), "visibility").ShouldBe("metadata_only");
            }
        }
    }

    private static YamlMappingNode LoadContract()
    {
        File.Exists(ContractPath).ShouldBeTrue("The OpenAPI Contract Spine must be checked in at the story-owned path.");
        using StringReader reader = new(File.ReadAllText(ContractPath));
        YamlStream stream = new();
        stream.Load(reader);
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Operation(YamlMappingNode root, string path, string method)
        => Mapping(Mapping(Mapping(root, "paths"), path), method);

    private static YamlMappingNode Mapping(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlMappingNode>();
    }

    private static YamlSequenceNode Sequence(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlSequenceNode>();
    }

    private static string Scalar(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlScalarNode>().Value.ShouldNotBeNull();
    }

    private static string[] RequiredKeys(YamlMappingNode node)
        => node.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value.ShouldNotBeNull()).ToArray();

    private static void AssertUlidPropertyIfPresent(YamlMappingNode node, string propertyName)
    {
        if (!node.Children.TryGetValue(new YamlScalarNode(propertyName), out YamlNode? value))
        {
            return;
        }

        UlidPattern().IsMatch(value.ShouldBeOfType<YamlScalarNode>().Value.ShouldNotBeNull()).ShouldBeTrue(propertyName);
    }

    private static void ShouldContainAll(string[] actual, string[] expected)
    {
        foreach (string item in expected)
        {
            actual.ShouldContain(item);
        }
    }

    private static YamlNode ResolveLocalReference(YamlMappingNode root, string reference)
    {
        reference.StartsWith("#/", StringComparison.Ordinal).ShouldBeTrue(reference);
        YamlNode current = root;
        foreach (string segment in reference[2..].Split('/'))
        {
            current = current.ShouldBeOfType<YamlMappingNode>().Children[new YamlScalarNode(segment)];
        }

        return current;
    }

    private static string ResolveParameterName(YamlMappingNode parameter)
    {
        if (parameter.Children.TryGetValue(new YamlScalarNode("$ref"), out YamlNode? referenceNode))
        {
            string reference = ((YamlScalarNode)referenceNode).Value.ShouldNotBeNull();
            parameter = ResolveLocalReference(LoadContract(), reference).ShouldBeOfType<YamlMappingNode>();
        }

        return Scalar(parameter, "name");
    }

    private static string[] AllSchemaPropertyNames(YamlMappingNode root, string schemaName)
    {
        YamlMappingNode schemas = Mapping(Mapping(root, "components"), "schemas");
        YamlMappingNode schema = Mapping(schemas, schemaName);
        YamlMappingNode properties = Mapping(schema, "properties");
        return properties.Children.Keys.OfType<YamlScalarNode>().Select(static key => key.Value.ShouldNotBeNull()).ToArray();
    }

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }

    [GeneratedRegex(@"['""](?<ref>#/[^'""]+)['""]")]
    private static partial Regex LocalReferencePattern();

    [GeneratedRegex(@"^\s*(?<key>x-[A-Za-z0-9-]+):", RegexOptions.Multiline)]
    private static partial Regex ExtensionKeyPattern();

    [GeneratedRegex("tenant|organization|principal|user", RegexOptions.IgnoreCase)]
    private static partial Regex TenantAuthorityPattern();

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$")]
    private static partial Regex UlidPattern();
}
