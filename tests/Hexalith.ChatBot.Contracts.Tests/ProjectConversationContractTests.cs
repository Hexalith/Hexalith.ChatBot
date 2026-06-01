using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

using YamlDotNet.RepresentationModel;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ProjectConversationContractTests
{
    private static readonly string RepositoryRoot = LocateRepositoryRoot();
    private static readonly string ContractPath = Path.Combine(RepositoryRoot, "src", "Hexalith.ChatBot.Contracts", "openapi", "hexalith.chatbot.v1.yaml");

    [Fact]
    public static void ProjectConversationDtoShouldSerializeMetadataOnlyWireTokens()
    {
        ProjectConversationResponse response = new(
            "project-001",
            "Authorized Project",
            null,
            ProjectConversationReadStatus.Current,
            LifecycleState.Associated,
            [
                new ProjectConversationItem(
                    "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                    ProjectConversationItemKind.EmailDerived,
            ProjectConversationActorKind.Mailbox,
                    "Mailbox event",
                    new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                    LifecycleState.Associated,
                    AssociationThresholdBand.Auto,
                    0.91,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    "controlled-mailbox-001",
                    "graph-message-001",
                    "<internet-message-001@example.test>",
                    "conversation-001",
                    "thread-001",
                    new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
                    "UTC",
                    "Microsoft 365 mailbox",
                    "m365-mailbox-intake",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    4,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project"),
                new ProjectConversationItem(
                    "decision:01ARZ3NDEKTSV4RRFFQ69G5FAW:7",
                    ProjectConversationItemKind.SystemDecision,
                    ProjectConversationActorKind.SystemDecision,
                    "System decision",
                    new DateTimeOffset(2026, 6, 1, 0, 3, 0, TimeSpan.Zero),
                    LifecycleState.CorrectionDelayed,
                    AssociationThresholdBand.Auto,
                    0.91,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    "controlled-mailbox-001",
                    "graph-message-001",
                    "<internet-message-001@example.test>",
                    "conversation-001",
                    "thread-001",
                    new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 31, 23, 58, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 31, 23, 57, 0, TimeSpan.Zero),
                    "UTC",
                    "Microsoft 365 mailbox",
                    "m365-mailbox-intake",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    7,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project",
                    DecisionLabel: "ProjectReassignment",
                    SafeNextAction: "wait-for-propagation",
                    DecisionKind: AssociationDecisionKind.Associate,
                    DecisionActorId: "user-001",
                    DecisionActorType: "human",
                    DecidedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 2, 0, TimeSpan.Zero),
                    DecisionNoteRedactionState: "redacted",
                    SurfaceOrigin: "ui",
                    PolicySnapshotVersion: "association-thresholds.m0.default.v1",
                    EvidenceReferenceSummary: ["mailbox:intake:subject"],
                    CorrectionKind: AssociationCorrectionKind.ProjectReassignment,
                    PriorProjectId: "project-000",
                    CorrectedProjectId: "project-001",
                    PredecessorAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FB1",
                    SupersedesAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FB2",
                    SupersededByAssociationId: "01ARZ3NDEKTSV4RRFFQ69G5FB3",
                    CorrectionRationaleRedactionState: "redacted",
                    CorrectionActorId: "user-001",
                    CorrectionActorType: "human",
                    CorrectedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 3, 0, TimeSpan.Zero),
                    DownstreamImpactStatus: "delayed",
                    CorrectionId: "correction-001",
                    WorkflowInstanceId: "workflow-001",
                    RequiredStoreKeys: ["project-conversation", "participants"],
                    CompletedStoreKeys: ["project-conversation"],
                    FailedStoreKeys: ["participants"],
                    PropagationProgressNumerator: 1,
                    PropagationProgressDenominator: 2,
                    PropagationStartedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 3, 10, TimeSpan.Zero),
                    PropagationEstimatedCompletionAtUtc: new DateTimeOffset(2026, 6, 1, 0, 5, 0, TimeSpan.Zero),
                    PropagationStatus: "delayed",
                    IsCorrectedContextStale: true,
                    ResponsibleOwnerRole: "operations"),
                new ProjectConversationItem(
                    "participant:01ARZ3NDEKTSV4RRFFQ69G5FAY:01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    ProjectConversationItemKind.Participant,
                    ProjectConversationActorKind.UnresolvedParticipant,
                    "Unresolved participant",
                    new DateTimeOffset(2026, 6, 1, 0, 1, 0, TimeSpan.Zero),
                    LifecycleState.Associated,
                    AssociationThresholdBand.Auto,
                    0.91,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    "controlled-mailbox-001",
                    null,
                    null,
                    "conversation-001",
                    "thread-001",
                    null,
                    null,
                    null,
                    null,
                    null,
                    "m365-mailbox-intake",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    5,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project",
                    ParticipantResolutionId: "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                    SourceParticipantId: "01ARZ3NDEKTSV4RRFFQ69G5FAZ",
                    ParticipantStatus: ParticipantResolutionStatus.Unresolved,
                    ParticipantBlockedReason: ParticipantResolutionBlockedReason.NotFound,
                    ParticipantDisplayKind: ProjectConversationParticipantDisplayKind.UnresolvedParticipant,
                    ParticipantEvidenceReference: "mailbox:intake:sender",
                    ParticipantEvidenceFingerprint: "evidence-sha256",
                    ParticipantAllowedReviewActions: [ParticipantReviewAction.Link, ParticipantReviewAction.CreatePending],
                    ParticipantRedactionState: "metadata_only"),
                new ProjectConversationItem(
                    "attachment:01ARZ3NDEKTSV4RRFFQ69G5FAW:0:826F",
                    ProjectConversationItemKind.Attachment,
                    ProjectConversationActorKind.MailboxAttachment,
                    "Mailbox attachment",
                    new DateTimeOffset(2026, 6, 1, 0, 2, 0, TimeSpan.Zero),
                    LifecycleState.Associated,
                    AssociationThresholdBand.Auto,
                    0.91,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    "controlled-mailbox-001",
                    null,
                    null,
                    "conversation-001",
                    "thread-001",
                    null,
                    null,
                    null,
                    null,
                    null,
                    "m365-mailbox-intake",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    6,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project",
                    SourceProviderAttachmentId: "graph-attachment-001",
                    AttachmentDisplayName: "invoice.pdf",
                    AttachmentContentType: "application/pdf",
                    AttachmentSizeInBytes: 4096,
                    AttachmentCaptureStatus: ProjectConversationAttachmentStatus.Captured,
                    AttachmentStorageStatus: ProjectConversationAttachmentStatus.Pending,
                    AttachmentScanStatus: ProjectConversationAttachmentStatus.Pending,
                    AttachmentDuplicateState: "not-evaluated",
                    AttachmentRetryState: "not-retryable",
                    AttachmentAiContextEligibility: "pending",
                    AttachmentAllowedActions: [],
                    AttachmentRedactionState: "metadata_only"),
                new ProjectConversationItem(
                    "approval:approval-001:request:8",
                    ProjectConversationItemKind.ApprovalEvent,
                    ProjectConversationActorKind.ApprovalSystem,
                    "Approval event",
                    new DateTimeOffset(2026, 6, 1, 0, 4, 0, TimeSpan.Zero),
                    LifecycleState.NeedsReview,
                    AssociationThresholdBand.Auto,
                    0,
                    "proposal-001",
                    "approval-event",
                    null,
                    null,
                    "decision:01ARZ3NDEKTSV4RRFFQ69G5FAW:7",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    "approval-event",
                    "metadata_only",
                    "collaboration_input",
                    "chatbot.project-conversation-item.v1",
                    8,
                    "01ARZ3NDEKTSV4RRFFQ69G5FAX",
                    ProjectId: "project-001",
                    ProjectDisplayName: "Authorized Project",
                    SafeNextAction: "await-approval",
                    ApprovalId: "approval-001",
                    ApprovalEventKind: ApprovalEventKind.Request,
                    ApprovalStatus: ApprovalStatus.Pending,
                    ApprovalRequesterId: "user-001",
                    ApprovalRequesterActorType: "human",
                    ApprovalRequestedAtUtc: new DateTimeOffset(2026, 6, 1, 0, 4, 0, TimeSpan.Zero),
                    ApprovalProposalId: "proposal-001",
                    ApprovalSourceMessageId: "graph-message-001",
                    ApprovalSourceConversationItemId: "decision:01ARZ3NDEKTSV4RRFFQ69G5FAW:7",
                    ApprovalCommandName: "SendExternalReply",
                    ApprovalCommandAllowlistVersion: "allowlist.v1",
                    ApprovalRiskClass: RiskClass.High,
                    ApprovalRiskActionClasses: ["externally-visible"],
                    ApprovalPolicySnapshotId: "policy-snapshot-001",
                    ApprovalPolicySnapshotVisibility: "authorized",
                    ApprovalEvidenceReferences: ["evidence:summary:001"],
                    ApprovalEvidenceFreshnessStates: [ApprovalEvidenceFreshness.Expired],
                    ApprovalAffectedResourceReferences: ["project:project-001"],
                    ApprovalRecipientReferences: ["recipient:external:001"],
                    ApprovalSenderAuthorityClass: "on-behalf-of",
                    ApprovalExpectedPostStateRedactionState: "metadata_only",
                    ApprovalActionSummaryRedactionState: "redacted"),
            ],
            new ProjectConversationCursorPage("opaque-cursor", true, 25),
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            "chatbot.project-conversation-response.v1",
            "01ARZ3NDEKTSV4RRFFQ69G5FAX",
            "none");

        string json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("\"status\":\"current\"");
        json.ShouldContain("\"kind\":\"email-derived\"");
        json.ShouldContain("\"kind\":\"system-decision\"");
        json.ShouldContain("\"kind\":\"participant\"");
        json.ShouldContain("\"kind\":\"attachment\"");
        json.ShouldContain("\"kind\":\"approval-event\"");
        json.ShouldContain("\"actorKind\":\"mailbox\"");
        json.ShouldContain("\"actorKind\":\"unresolved-participant\"");
        json.ShouldContain("\"actorKind\":\"mailbox-attachment\"");
        json.ShouldContain("\"actorKind\":\"approval-system\"");
        json.ShouldContain("\"participantDisplayKind\":\"unresolved-participant\"");
        json.ShouldContain("\"participantStatus\":\"unresolved\"");
        json.ShouldContain("\"participantEvidenceReference\":\"mailbox:intake:sender\"");
        json.ShouldContain("\"sourceProviderMessageId\":\"graph-message-001\"");
        json.ShouldContain("\"internetMessageId\":");
        json.ShouldContain("internet-message-001@example.test");
        json.ShouldContain("\"sourceReceivedAtUtc\":\"2026-06-01T00:00:00+00:00\"");
        json.ShouldContain("\"sourceProvenanceDisplayToken\":\"Microsoft 365 mailbox\"");
        json.ShouldContain("\"sourceProviderAttachmentId\":\"graph-attachment-001\"");
        json.ShouldContain("\"attachmentDisplayName\":\"invoice.pdf\"");
        json.ShouldContain("\"attachmentCaptureStatus\":\"captured\"");
        json.ShouldContain("\"attachmentStorageStatus\":\"pending\"");
        json.ShouldContain("\"attachmentScanStatus\":\"pending\"");
        json.ShouldContain("\"decisionKind\":\"associate\"");
        json.ShouldContain("\"correctionKind\":\"project-reassignment\"");
        json.ShouldContain("\"decisionActorType\":\"human\"");
        json.ShouldContain("\"decisionNoteRedactionState\":\"redacted\"");
        json.ShouldContain("\"correctionRationaleRedactionState\":\"redacted\"");
        json.ShouldContain("\"evidenceReferenceSummary\":[\"mailbox:intake:subject\"]");
        json.ShouldContain("\"requiredStoreKeys\":[\"project-conversation\",\"participants\"]");
        json.ShouldContain("\"isCorrectedContextStale\":true");
        json.ShouldContain("\"approvalEventKind\":\"request\"");
        json.ShouldContain("\"approvalStatus\":\"pending\"");
        json.ShouldContain("\"approvalRiskClass\":\"high\"");
        json.ShouldContain("\"approvalEvidenceFreshnessStates\":[\"expired\"]");
        json.ShouldContain("\"approvalPolicySnapshotId\":\"policy-snapshot-001\"");
        json.ShouldContain("\"approvalActionSummaryRedactionState\":\"redacted\"");
        json.ShouldContain("\"thresholdBand\":\"auto\"");
        json.ShouldNotContain("EmailDerived", Case.Sensitive);
        json.ShouldNotContain("MailboxMessageBody", Case.Sensitive);
        json.ShouldNotContain("raw-body", Case.Insensitive);
        json.ShouldNotContain("sourceContext", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("rawAttachmentContent", Case.Insensitive);
        json.ShouldNotContain("\"decisionNote\":", Case.Insensitive);
        json.ShouldNotContain("\"correctionRationale\":", Case.Insensitive);
        json.ShouldNotContain("malwareScanDetail", Case.Insensitive);
        json.ShouldNotContain("providerDisplayName", Case.Insensitive);
        json.ShouldNotContain("addressEvidence", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
        json.ShouldNotContain("modelOutput", Case.Insensitive);
        json.ShouldNotContain("commandPayload", Case.Insensitive);
        json.ShouldNotContain("policyBody", Case.Insensitive);
        json.ShouldNotContain("auditEnvelope", Case.Insensitive);
        json.ShouldNotContain("decisionRationale\":", Case.Insensitive);
    }

    [Fact]
    public static void ProjectConversationOpenApiShouldDeclareCursorPaginationAndMetadataOnlyFields()
    {
        YamlMappingNode root = LoadContract();
        YamlMappingNode operation = Mapping(Mapping(Mapping(root, "paths"), "/api/v1/projects/{projectId}/conversation"), "get");
        Scalar(operation, "operationId").ShouldBe("GetProjectConversation");
        Mapping(operation, "responses").Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("200");

        YamlMappingNode schemas = Mapping(Mapping(root, "components"), "schemas");
        Sequence(Mapping(schemas, "ProjectConversationReadStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["current", "empty", "stale", "degraded", "blocked"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationItemKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("failure-state");
        string[] actorKindTokens = Sequence(Mapping(schemas, "ProjectConversationActorKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ToArray();
        actorKindTokens.ShouldContain("approval-system");
        actorKindTokens.ShouldContain("system-status");
        Sequence(Mapping(schemas, "FailureStateKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["failure", "retry-queued", "retry-accepted", "retry-exhausted", "blocked", "duplicate-suppressed", "dependency-degraded", "projection-retryable", "terminal-failure", "reprocess-created"], ignoreOrder: false);
        Sequence(Mapping(schemas, "FailureStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["retryable", "terminal", "blocked", "degraded", "resolved", "unknown"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ApprovalEventKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["request", "decision", "outcome"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ApprovalStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["pending", "approved", "rejected", "revision-requested", "cancelled", "executed", "failed"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ApprovalDecisionKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["approve", "reject", "request-revision", "cancel"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ApprovalEvidenceFreshness"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["fresh", "stale", "expired"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationAttachmentStatus"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["captured", "pending", "unavailable", "rejected", "unsafe", "failed", "retryable"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationParticipantDisplayKind"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldBe(["internal-participant", "external-participant", "unresolved-participant", "restricted-participant"], ignoreOrder: false);
        Sequence(Mapping(schemas, "ProjectConversationResponse"), "required").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("page");

        YamlMappingNode itemProperties = Mapping(Mapping(schemas, "ProjectConversationItem"), "properties");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceProviderMessageId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("internetMessageId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceReceivedAtUtc");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceSentAtUtc");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceCreatedAtUtc");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceTimezone");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceProvenanceDisplayToken");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("participantResolutionId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("participantAllowedReviewActions");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("participantRedactionState");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("sourceProviderAttachmentId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("attachmentDisplayName");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("attachmentScanStatus");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("attachmentAllowedActions");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("attachmentRedactionState");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("decisionKind");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("decisionActorId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("decidedAtUtc");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("decisionNoteRedactionState");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("surfaceOrigin");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("policySnapshotVersion");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("evidenceReferenceSummary");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("correctionKind");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("priorProjectId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("correctedProjectId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("supersedesAssociationId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("correctionRationaleRedactionState");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("downstreamImpactStatus");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("requiredStoreKeys");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("propagationProgressNumerator");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("isCorrectedContextStale");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("responsibleOwnerRole");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalEventKind");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalStatus");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalDecisionKind");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalPolicySnapshotVisibility");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalEvidenceFreshnessStates");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalAuditOperationId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("approvalCommandOutcomeStatus");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("supersedesApprovalId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("failureStateKind");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("failureStatus");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("messageCatalogCode");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("messageCatalogVersion");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("messageDetailVisibility");
        Sequence(Mapping(itemProperties, "blockedReason"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("retry-exhausted");
        Sequence(Mapping(itemProperties, "blockedReason"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("already-decided");
        Sequence(Mapping(itemProperties, "blockedReason"), "enum").Children
            .OfType<YamlScalarNode>()
            .Select(static node => node.Value.ShouldNotBeNull())
            .ShouldContain("audit-unavailable");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("retryOperationId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("workflowInstanceId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("operationId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("auditOperationId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("duplicateSafetyState");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldContain("reprocessCreatedWorkflowInstanceId");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("sourceContext");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("providerPayload");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("attachmentContent");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("malwareScanDetail");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("addressEvidence");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("decisionNote");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("correctionRationale");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("commandPayload");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("policyBody");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("auditEnvelope");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("decisionRationale");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("exception");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("stackTrace");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("providerDiagnostic");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("payload");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("prompt");
        itemProperties.Children.Keys.Select(static key => ((YamlScalarNode)key).Value).ShouldNotContain("output");
    }

    [Fact]
    public static void ProjectConversationEnumsShouldHaveStableWireTokens()
    {
        WireValue(ProjectConversationReadStatus.Blocked).ShouldBe("blocked");
        WireValue(ProjectConversationItemKind.SystemDecision).ShouldBe("system-decision");
        WireValue(ProjectConversationItemKind.Participant).ShouldBe("participant");
        WireValue(ProjectConversationItemKind.Attachment).ShouldBe("attachment");
        WireValue(ProjectConversationItemKind.ApprovalEvent).ShouldBe("approval-event");
        WireValue(ProjectConversationItemKind.FailureState).ShouldBe("failure-state");
        WireValue(ProjectConversationActorKind.Mailbox).ShouldBe("mailbox");
        WireValue(ProjectConversationActorKind.MailboxAttachment).ShouldBe("mailbox-attachment");
        WireValue(ProjectConversationActorKind.ApprovalSystem).ShouldBe("approval-system");
        WireValue(ProjectConversationActorKind.SystemStatus).ShouldBe("system-status");
        WireValue(FailureStateKind.RetryQueued).ShouldBe("retry-queued");
        WireValue(FailureStateKind.TerminalFailure).ShouldBe("terminal-failure");
        WireValue(FailureStatus.Retryable).ShouldBe("retryable");
        WireValue(ApprovalEventKind.Outcome).ShouldBe("outcome");
        WireValue(ApprovalStatus.RevisionRequested).ShouldBe("revision-requested");
        WireValue(ApprovalDecisionKind.RequestRevision).ShouldBe("request-revision");
        WireValue(ApprovalEvidenceFreshness.Expired).ShouldBe("expired");
        WireValue(ProjectConversationAttachmentStatus.Retryable).ShouldBe("retryable");
        WireValue(ProjectConversationActorKind.InternalParticipant).ShouldBe("internal-participant");
        WireValue(ProjectConversationParticipantDisplayKind.RestrictedParticipant).ShouldBe("restricted-participant");
    }

    private static string WireValue<T>(T value)
        where T : struct, Enum
        => typeof(T)
            .GetField(value.ToString())
            ?.GetCustomAttribute<EnumMemberAttribute>()
            ?.Value
            ?? value.ToString();

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
