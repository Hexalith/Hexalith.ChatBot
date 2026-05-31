using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class AssociationContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void AssociationWorkflowIdShouldBeUlidOnly()
    {
        AssociationWorkflowId.TryParse("01ARZ3NDEKTSV4RRFFQ69G5FAV", out AssociationWorkflowId id).ShouldBeTrue();
        id.Value.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        AssociationWorkflowId.TryParse(Guid.NewGuid().ToString(), out _).ShouldBeFalse();
    }

    [Fact]
    public static void AssociationResultShouldSerializeCamelCaseWithoutRawPii()
    {
        AssociationScoringResult result = new(
            0.9,
            AssociationThresholdBand.Auto,
            AssociationScoringOutcome.AutoAssociated,
            [AssociationReasonCode.ExplicitProjectIdentifierMatched],
            "association-deterministic.kernel.m0.v1",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "controlled-mailbox-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "conversation-001",
            "thread-001",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "metadata_only",
            "collaboration_input",
            "chatbot.association-scoring-result.v1");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"confidenceScore\"");
        json.ShouldContain("\"kernelVersion\"");
        json.ShouldNotContain("ConfidenceScore", Case.Sensitive);
        json.ShouldNotContain("sender@example.test", Case.Insensitive);
        json.ShouldNotContain("Project Alpha", Case.Sensitive);
    }

    [Fact]
    public static void AssociationRoutingStatusShouldExposeNeedsReviewContractSafely()
    {
        AssociationRoutingStatus status = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            LifecycleState.NeedsReview,
            AssociationScoringOutcome.FailedClosed,
            AssociationThresholdBand.FailClosed,
            0.0,
            [AssociationReasonCode.ScorerError],
            [],
            [],
            "association-thresholds.m0.default.v1",
            [],
            "association-deterministic.kernel.m0.v1",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.association-routing-status.v1",
            1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            [ChatBotDisabledActionReasons.AwaitingOtherActor],
            [ChatBotMessageCodes.AssociationScorerFailedClosed]);

        string json = JsonSerializer.Serialize(status, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"lifecycleState\"");
        json.ShouldContain("\"NeedsReview\"");
        json.ShouldContain("\"outcome\":\"failed-closed\"");
        json.ShouldContain("\"thresholdBand\":\"fail-closed\"");
        json.ShouldContain("\"reasonCodes\":[\"scorer-error\"]");
        json.ShouldContain("\"candidates\":[]");
        json.ShouldContain("\"reasonCodes\"");
        json.ShouldNotContain("FailedClosed", Case.Sensitive);
        json.ShouldNotContain("FailClosed", Case.Sensitive);
        json.ShouldNotContain("ScorerError", Case.Sensitive);
        json.ShouldNotContain("sender@example.test", Case.Insensitive);
        json.ShouldNotContain("raw-body", Case.Insensitive);
        json.ShouldNotContain("Project Alpha", Case.Sensitive);
    }

    [Fact]
    public static void AssociationRoutingStatusShouldExposeCorrectionPropagationContractSafely()
    {
        AssociationRoutingStatus status = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            "thread-001",
            LifecycleState.Correcting,
            AssociationScoringOutcome.CandidatesGenerated,
            AssociationThresholdBand.Ambiguous,
            0.72,
            [AssociationReasonCode.MultipleAuthorizedCandidates],
            [],
            [],
            "association-thresholds.m0.default.v1",
            [],
            "association-deterministic.kernel.m0.v1",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.association-routing-status.v1",
            3,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            ["corrected-context-stale"],
            [ChatBotMessageCodes.AssociationCorrectionPropagationPending, ChatBotMessageCodes.AssociationAiContextBlocked],
            CorrectedProjectId: "project-002",
            PriorProjectId: "project-001",
            PredecessorAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            CorrectionKind: AssociationCorrectionKind.ProjectReassignment,
            DownstreamImpactStatus: "correcting",
            PropagationStatus: "correcting",
            PropagationProgressNumerator: 2,
            PropagationProgressDenominator: 4,
            PropagationEstimatedCompletionAtUtc: new DateTimeOffset(2026, 5, 31, 9, 40, 0, TimeSpan.Zero),
            IsCorrectedContextStale: true,
            ResponsibleOwnerRole: "project-owner",
            SafeNextAction: "wait-for-propagation",
            WorkflowInstanceId: "workflow-correction-001",
            RequiredStoreKeys: ["association-routing", "evidence-snapshot", "operational-status", "ai-context-readiness"],
            CompletedStoreKeys: ["association-routing", "evidence-snapshot"],
            FailedStoreKeys: []);

        string json = JsonSerializer.Serialize(status, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"lifecycleState\":\"Correcting\"");
        json.ShouldContain("\"correctionKind\":\"project-reassignment\"");
        json.ShouldContain("\"downstreamImpactStatus\":\"correcting\"");
        json.ShouldContain("\"propagationStatus\":\"correcting\"");
        json.ShouldContain("\"propagationProgressNumerator\":2");
        json.ShouldContain("\"propagationProgressDenominator\":4");
        json.ShouldContain("\"isCorrectedContextStale\":true");
        json.ShouldContain("\"responsibleOwnerRole\":\"project-owner\"");
        json.ShouldContain("\"safeNextAction\":\"wait-for-propagation\"");
        json.ShouldContain("\"workflowInstanceId\":\"workflow-correction-001\"");
        json.ShouldContain("\"requiredStoreKeys\":[\"association-routing\",\"evidence-snapshot\",\"operational-status\",\"ai-context-readiness\"]");
        json.ShouldContain("\"completedStoreKeys\":[\"association-routing\",\"evidence-snapshot\"]");
        json.ShouldContain("association_correction_propagation_pending");
        json.ShouldContain("association_ai_context_blocked");
        json.ShouldNotContain("restricted@example.com", Case.Insensitive);
        json.ShouldNotContain("raw provider payload", Case.Insensitive);
        json.ShouldNotContain("Secret Project", Case.Sensitive);
        json.ShouldNotContain("raw exception", Case.Insensitive);
    }

    [Fact]
    public static void AssociationRoutingStatusShouldUseEnumMemberWireTokensInNestedCollections()
    {
        AssociationRoutingStatus status = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "controlled-mailbox-001",
            "conversation-001",
            null,
            LifecycleState.NeedsReview,
            AssociationScoringOutcome.CandidatesGenerated,
            AssociationThresholdBand.Ambiguous,
            0.75,
            [AssociationReasonCode.MultipleAuthorizedCandidates],
            [
                new AssociationCandidate(
                    "project-001",
                    null,
                    0.75,
                    1,
                    [AssociationReasonCode.ConversationThreadMatched],
                    [new AssociationEvidenceReference("mailbox:thread", "hash-thread", "conversation-thread-identifier")],
                    [
                        new AssociationConfidenceInput(
                            AssociationSignalClass.ConversationThreadIdentifier,
                            AssociationReasonCode.ConversationThreadMatched,
                            0.75,
                            "mailbox:thread",
                            "hash-thread"),
                    ],
                    false),
            ],
            [
                new AssociationExclusion(
                    "suppressed",
                    AssociationExclusionState.TenantMismatch,
                    AssociationReasonCode.UnauthorizedCandidateSuppressed,
                    "mailbox:hidden",
                    "hash-hidden"),
            ],
            "association-thresholds.m0.default.v1",
            [],
            "association-deterministic.kernel.m0.v1",
            new DateTimeOffset(2026, 5, 31, 9, 0, 0, TimeSpan.Zero),
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.association-routing-status.v1",
            1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            [ChatBotDisabledActionReasons.AwaitingOtherActor],
            [ChatBotMessageCodes.AssociationAmbiguousRouted]);

        string json = JsonSerializer.Serialize(status, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"outcome\":\"candidates-generated\"");
        json.ShouldContain("\"thresholdBand\":\"ambiguous\"");
        json.ShouldContain("\"multiple-authorized-candidates\"");
        json.ShouldContain("\"conversation-thread-identifier\"");
        json.ShouldContain("\"conversation-thread-matched\"");
        json.ShouldContain("\"tenant-mismatch\"");
        json.ShouldContain("\"unauthorized-candidate-suppressed\"");
        json.ShouldNotContain("CandidatesGenerated", Case.Sensitive);
        json.ShouldNotContain("ConversationThreadIdentifier", Case.Sensitive);
        json.ShouldNotContain("TenantMismatch", Case.Sensitive);
    }

    [Fact]
    public static void AssociationReasonCodesShouldHaveStableWireTokens()
    {
        WireValue(AssociationReasonCode.ExplicitProjectIdentifierMatched).ShouldBe("explicit-project-identifier-matched");
        WireValue(AssociationReasonCode.UnauthorizedCandidateSuppressed).ShouldBe("unauthorized-candidate-suppressed");
        WireValue(AssociationThresholdBand.FailClosed).ShouldBe("fail-closed");
        WireValue(AssociationDecisionKind.NeedsReview).ShouldBe("needs-review");
    }

    [Fact]
    public static void AssociationDecisionCommandsShouldSerializeCamelCaseWithoutEnvelopeAuthority()
    {
        AssociateEmailToProject command = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "project-001",
            AssociationDecisionKind.Associate,
            "Reviewed against safe evidence.",
            "hash-project",
            3,
            "chatbot.association-decision-command.v1");

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"associationId\"");
        json.ShouldContain("\"decisionKind\":\"associate\"");
        json.ShouldContain("\"candidateEvidenceFingerprint\"");
        json.ShouldNotContain("\"AssociationId\"", Case.Sensitive);
        json.ShouldNotContain("tenant", Case.Insensitive);
        json.ShouldNotContain("actor", Case.Insensitive);
        json.ShouldNotContain("surfaceOrigin", Case.Sensitive);
        json.ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public static void AssociationCorrectionCommandShouldSerializeMetadataOnlyWithoutEnvelopeAuthority()
    {
        CorrectEmailProjectAssociation command = new(
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "01ARZ3NDEKTSV4RRFFQ69G5FAY",
            "project-001",
            "project-002",
            AssociationCorrectionKind.ProjectReassignment,
            "Wrong project selected from safe metadata.",
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            "hash-project-002",
            2,
            "chatbot.association-correction-command.v1");

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"associationId\"");
        json.ShouldContain("\"priorProjectId\":\"project-001\"");
        json.ShouldContain("\"targetProjectId\":\"project-002\"");
        json.ShouldContain("\"correctionKind\":\"project-reassignment\"");
        json.ShouldContain("\"predecessorAssociationId\"");
        json.ShouldContain("\"candidateEvidenceFingerprint\"");
        json.ShouldNotContain("\"AssociationId\"", Case.Sensitive);
        json.ShouldNotContain("tenant", Case.Insensitive);
        json.ShouldNotContain("actor", Case.Insensitive);
        json.ShouldNotContain("surfaceOrigin", Case.Sensitive);
        json.ShouldNotContain("sender@example.test", Case.Insensitive);
        json.ShouldNotContain("raw-body", Case.Insensitive);
    }

    [Fact]
    public static void OpenApiShouldDeclareAssociationRequiredFieldsAndEnums()
    {
        YamlMappingNode schemas = Mapping(Mapping(LoadContract(), "components"), "schemas");
        YamlMappingNode command = Mapping(schemas, nameof(ScoreMailboxMessageAssociation));
        YamlMappingNode decisionCommand = Mapping(schemas, nameof(AssociateEmailToProject));
        YamlMappingNode correctionCommand = Mapping(schemas, nameof(CorrectEmailProjectAssociation));

        Sequence(command, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(
                [
                    "associationId",
                    "intakeId",
                    "sourceMailboxId",
                    "sourceConversationId",
                    "deterministicSignals",
                    "thresholdPolicy",
                    "candidates",
                    "exclusions",
                    "result",
                    "scoringKernelVersion",
                ],
                ignoreOrder: false);

        Sequence(Mapping(schemas, nameof(AssociationThresholdBand)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["auto", "ambiguous", "fail-closed"], ignoreOrder: false);
        Sequence(Mapping(schemas, nameof(AssociationReasonCode)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("authorization-evidence-unavailable");

        YamlMappingNode routingStatus = Mapping(schemas, nameof(AssociationRoutingStatus));
        Sequence(routingStatus, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("lifecycleState");
        Mapping(routingStatus, "properties").Children.Keys.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("evidenceRefs");
        Sequence(decisionCommand, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("candidateEvidenceFingerprint");
        Sequence(correctionCommand, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("priorProjectId");
        Mapping(decisionCommand, "properties").Children.Keys.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldNotContain("tenantId");
        Sequence(Mapping(schemas, nameof(AssociationDecisionKind)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["associate", "reject", "defer", "needs-review"], ignoreOrder: false);
        Sequence(correctionCommand, "required").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(
                [
                    "associationId",
                    "intakeId",
                    "priorProjectId",
                    "targetProjectId",
                    "correctionKind",
                    "predecessorAssociationId",
                    "candidateEvidenceFingerprint",
                    "sourceVersion",
                    "schemaVersion",
                ],
                ignoreOrder: false);
        Mapping(correctionCommand, "properties").Children.Keys.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldNotContain("tenantId");
        Mapping(correctionCommand, "properties").Children.Keys.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldNotContain("actorId");
        Sequence(Mapping(schemas, nameof(AssociationCorrectionKind)), "enum").Children.OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["project-reassignment"], ignoreOrder: false);
    }

    private static string WireValue<T>(T enumValue)
        where T : struct, Enum
    {
        MemberInfo member = typeof(T).GetMember(enumValue.ToString()).Single();
        string? wireValue = member.GetCustomAttribute<EnumMemberAttribute>()?.Value;
        wireValue.ShouldNotBeNull();
        return wireValue;
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

    private static YamlSequenceNode Sequence(YamlMappingNode node, string key)
    {
        node.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? value).ShouldBeTrue(key);
        return value.ShouldBeOfType<YamlSequenceNode>();
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
