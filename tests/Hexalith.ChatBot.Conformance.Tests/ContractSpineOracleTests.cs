using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Idempotency;
using Hexalith.ChatBot.Server.Gateway.Redaction;
using Hexalith.ChatBot.Server.Gateway.Status;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Lifecycle.StateModel;
using Hexalith.ChatBot.Server.Lifecycle.Workflows;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Conformance.Tests;

public static class ContractSpineOracleTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");
    private static readonly string OraclePath = Path.Combine(RepositoryRoot, "tests", "fixtures", "story-1-2-contract-spine-oracle.json");

    [Fact]
    public static async Task RuntimeGatewayAndStatusOutcomesShouldMatchSafeOracle()
    {
        using JsonDocument fixture = JsonDocument.Parse(File.ReadAllText(OraclePath));
        JsonElement expected = fixture.RootElement.GetProperty("runtimeOutcomes");
        JsonElement expectedAccepted = expected.GetProperty("accepted");
        DateTimeOffset now = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        FixedClock clock = new(now);
        CountingDispatcher dispatcher = new(clock);
        InMemoryOperationStatusStore statuses = new();
        InMemoryCoarseIdempotencyStore idempotency = new(clock);
        CommandGateway gateway = new(
            new ClaimsAuthenticationStage(),
            new ClaimsTenantBindingStage(),
            new PassThroughAuthorizationStage(),
            new PassThroughRiskClassifier(),
            new PassThroughApprovalGate(),
            idempotency,
            new InMemoryAuditWriter(),
            new InMemoryAuditReplayIntentQueue(),
            new InMemoryOperatorAlertSink(),
            statuses,
            clock,
            new CommandSubmissionLifecycleTransitionGuard(),
            dispatcher,
            new ChatBotProblemDetailsFactory(new CoarseUserFacingRedactionStage(), new InMemoryUserFacingMessageTelemetry()),
            new OracleAllowlist(),
            requestAuthorizer: Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Authorizer(clock));
        ClaimsPrincipal principal = Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Principal(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "actor-alpha"), new Claim("eventstore:tenant", "tenant-alpha")], "oracle")));
        ChatBotCommandSubmission submission = Submission(principal, "allowed-resource");

        ChatBotGatewayResult accepted = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        ChatBotGatewayResult replay = await gateway.SubmitAsync(submission, TestContext.Current.CancellationToken);
        ChatBotGatewayResult conflict = await gateway.SubmitAsync(
            Submission(principal, "different-resource"), TestContext.Current.CancellationToken);
        ChatBotGatewayResult denied = await gateway.SubmitAsync(
            Submission(Hexalith.ChatBot.Tests.TrustedAuthority.RegressionAuthorityFixture.Principal(new ClaimsPrincipal(new ClaimsIdentity())), "allowed-resource"),
            TestContext.Current.CancellationToken);

        dispatcher.Count.ShouldBe(1);
        accepted.IsAccepted.ShouldBeTrue();
        replay.IsAccepted.ShouldBeTrue();
        conflict.IsAccepted.ShouldBeFalse();
        denied.IsAccepted.ShouldBeFalse();

        using JsonDocument acceptedJson = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(accepted.Accepted));
        using JsonDocument replayJson = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(replay.Accepted));
        AssertSafeOutcome(acceptedJson.RootElement, expectedAccepted);
        AssertSafeOutcome(replayJson.RootElement, expectedAccepted, includesPriorOutcome: true);
        AssertSafeOutcome(replayJson.RootElement.GetProperty("priorOutcome"), expectedAccepted);

        using JsonDocument conflictJson = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(conflict.Problem));
        using JsonDocument deniedJson = JsonDocument.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(denied.Problem));
        AssertSafeProblem(conflictJson.RootElement, expected.GetProperty("conflict"));
        AssertSafeProblem(deniedJson.RootElement, expected.GetProperty("denied"));
        conflictJson.RootElement.GetRawText().ShouldNotContain("allowed-resource", Case.Insensitive);
        conflictJson.RootElement.GetRawText().ShouldNotContain("different-resource", Case.Insensitive);
        deniedJson.RootElement.GetRawText().ShouldNotContain("allowed-resource", Case.Insensitive);

        OperationStatusRecord status = (await statuses.TryGetAsync(
            "tenant-alpha", expectedAccepted.GetProperty("operationId").GetString()!, TestContext.Current.CancellationToken))!;
        JsonElement statusJson = OperationStatusHttpResults.ToJsonElement(status, now);
        statusJson.GetProperty("operationId").GetString().ShouldBe(expectedAccepted.GetProperty("operationId").GetString());
        foreach (JsonProperty property in expected.GetProperty("pending").EnumerateObject())
        {
            statusJson.GetProperty(property.Name).GetRawText().ShouldBe(property.Value.GetRawText());
        }

        AssertSafeOutcome(statusJson.GetProperty("priorOutcome"), expectedAccepted);

        OperationStatusWorkflowStatusSink sink = new(statuses, clock);
        CorrectionPropagationRequest correction = new(
            "tenant-alpha", "actor-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY", "correction-1", "wf-1", "project-001", "project-002", 3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW", now, now.AddMinutes(10),
            OperationId: expectedAccepted.GetProperty("operationId").GetString()!);
        await sink.ReportAsync(correction, CorrectionPropagationWorkflowStatuses.Delayed, 1,
            "vector_reindex_failed", TestContext.Current.CancellationToken);
        OperationStatusRecord failed = (await statuses.TryGetAsync(
            "tenant-alpha", expectedAccepted.GetProperty("operationId").GetString()!, TestContext.Current.CancellationToken))!;
        JsonElement failedJson = OperationStatusHttpResults.ToJsonElement(failed, now);
        foreach (string field in new[] { "reasonCode", "workflowLastFailureCode" })
        {
            failedJson.GetProperty(field).GetString().ShouldBe(expected.GetProperty("workflowFailure").GetProperty(field).GetString());
        }

        AssertSafeOutcome(failedJson.GetProperty("priorOutcome"), expectedAccepted);
    }

    [Fact]
    public static void StoryTwelveOracleShouldTrackCurrentCommandSubmissionContract()
    {
        using JsonDocument oracle = JsonDocument.Parse(File.ReadAllText(OraclePath));
        YamlMappingNode root = LoadContract();
        JsonElement operationOracle = oracle.RootElement.GetProperty("operation");

        oracle.RootElement.GetProperty("story").GetString().ShouldBe("1.2");
        operationOracle.GetProperty("path").GetString().ShouldBe("/api/v1/commands");
        operationOracle.GetProperty("method").GetString().ShouldBe("post");
        operationOracle.GetProperty("successStatus").GetInt32().ShouldBe(202);

        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), operationOracle.GetProperty("path").GetString().ShouldNotBeNull()), "post");
        Scalar(operation, "operationId").ShouldBe(operationOracle.GetProperty("operationId").GetString());

        YamlMappingNode extension = Mapping(operation, "x-hexalith-command-submission");
        JsonElement adapterInputShape = oracle.RootElement.GetProperty("adapterInputShape");
        string[] adapterContract = Sequence(extension, "adapterContract").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull()).ToArray();
        adapterContract.ShouldBe(
            adapterInputShape.GetProperty("adapterContract").EnumerateArray().Select(static item => item.GetString().ShouldNotBeNull()).ToArray(),
            ignoreOrder: false);

        // Every published adapter entry point must name a real typed-facade submission method (and every facade
        // submission method must be published), so adding or renaming one without updating the spine fails here.
        string[] facadeSubmissionMethods = typeof(Hexalith.ChatBot.Client.IChatBotClient).GetMethods()
            .Where(static method => method.Name.StartsWith("Submit", StringComparison.Ordinal))
            .Select(static method => $"IChatBotClient.{method.Name}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        adapterContract.Order(StringComparer.Ordinal).ToArray().ShouldBe(facadeSubmissionMethods);
        Scalar(extension, "commandMarker").ShouldBe(adapterInputShape.GetProperty("commandMarker").GetString());

        YamlMappingNode requestSchema = Mapping(Mapping(Mapping(root, "components"), "schemas"), adapterInputShape.GetProperty("requestSchema").GetString().ShouldNotBeNull());
        string[] requestRequired = Sequence(requestSchema, "required").Children.OfType<YamlScalarNode>().Select(static node => node.Value.ShouldNotBeNull()).ToArray();
        string[] requestProperties = Mapping(requestSchema, "properties").Children.Keys.OfType<YamlScalarNode>().Select(static node => node.Value.ShouldNotBeNull()).ToArray();

        string[] oracleRequired = adapterInputShape.GetProperty("requiredFields").EnumerateArray().Select(static field => field.GetString().ShouldNotBeNull()).ToArray();
        requestRequired.ShouldBe(oracleRequired, ignoreOrder: false);

        foreach (string forbidden in adapterInputShape.GetProperty("forbiddenAuthorityFields").EnumerateArray().Select(static field => field.GetString().ShouldNotBeNull()))
        {
            requestProperties.ShouldNotContain(forbidden);
        }
    }

    [Fact]
    public static void StoryTwelveOracleShouldTrackMetadataOnlyFailureCategories()
    {
        using JsonDocument oracle = JsonDocument.Parse(File.ReadAllText(OraclePath));
        YamlMappingNode root = LoadContract();
        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), "/api/v1/commands"), "post");

        string[] oracleCategories = oracle.RootElement.GetProperty("metadataOnlyFailureCategories")
            .EnumerateArray()
            .Select(static category => category.GetString().ShouldNotBeNull())
            .ToArray();

        string[] extensionCategories = Sequence(operation, "x-hexalith-canonical-error-categories")
            .Children
            .OfType<YamlScalarNode>()
            .Select(static category => category.Value.ShouldNotBeNull())
            .ToArray();

        string[] problemCategories = Sequence(Mapping(Mapping(Mapping(Mapping(root, "components"), "schemas"), "ProblemDetails"), "properties", "category"), "enum")
            .Children
            .OfType<YamlScalarNode>()
            .Select(static category => category.Value.ShouldNotBeNull())
            .ToArray();

        extensionCategories.ShouldBe(oracleCategories, ignoreOrder: false);
        problemCategories.ShouldBe(oracleCategories, ignoreOrder: false);
    }

    private static ChatBotCommandSubmission Submission(ClaimsPrincipal principal, string resource)
        => new(
            principal,
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = "RecordGovernedNote",
                Command = new OracleCommand("tenant-alpha", resource),
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX");

    private static void AssertSafeOutcome(JsonElement actual, JsonElement expected, bool includesPriorOutcome = false)
    {
        actual.EnumerateObject().Count().ShouldBe(expected.EnumerateObject().Count() + (includesPriorOutcome ? 1 : 0));
        foreach (JsonProperty property in expected.EnumerateObject())
        {
            JsonElement value = actual.GetProperty(property.Name);
            if (property.Name == "acceptedAt")
            {
                DateTimeOffset timestamp = value.GetDateTimeOffset();
                timestamp.ShouldBe(property.Value.GetDateTimeOffset());
                timestamp.Offset.ShouldBe(TimeSpan.Zero);
            }
            else
            {
                value.GetRawText().ShouldBe(property.Value.GetRawText(), property.Name);
            }
        }
    }

    private static void AssertSafeProblem(JsonElement actual, JsonElement expected)
    {
        actual.EnumerateObject().Count().ShouldBe(expected.EnumerateObject().Count());
        foreach (JsonProperty field in expected.EnumerateObject())
        {
            if (field.Name != "visibility")
            {
                actual.GetProperty(field.Name).GetRawText().ShouldBe(field.Value.GetRawText(), field.Name);
            }
        }

        JsonElement details = actual.GetProperty("details");
        details.EnumerateObject().Count().ShouldBe(1);
        details.GetProperty("visibility").GetString()
            .ShouldBe(expected.GetProperty("visibility").GetString());
    }

    private sealed record OracleCommand(string TenantId, string ResourceName)
    {
        public string NoteId { get; } = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    }

    private sealed class OracleAllowlist : ISpineCommandAllowlist
    {
        public bool IsAllowed(string? commandType) => commandType == "RecordGovernedNote";
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class CountingDispatcher(FixedClock clock) : ICommandDispatcher
    {
        public int Count { get; private set; }

        public ValueTask<ChatBotDispatchResult> DispatchAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
        {
            Count++;
            return ValueTask.FromResult(new ChatBotDispatchResult(clock.UtcNow.ToOffset(TimeSpan.FromHours(2))));
        }
    }

    private static YamlMappingNode LoadContract()
    {
        using StringReader reader = new(File.ReadAllText(ContractPath));
        YamlStream stream = new();
        stream.Load(reader);
        return (YamlMappingNode)stream.Documents[0].RootNode;
    }

    private static YamlMappingNode Mapping(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlMappingNode>();
    }

    private static YamlMappingNode Mapping(YamlMappingNode node, string firstKey, string secondKey)
        => Mapping(Mapping(node, firstKey), secondKey);

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

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.ChatBot.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
