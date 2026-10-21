using System.Security.Claims;
using CommandSubmissionRequest = Hexalith.ChatBot.Client.Generated.CommandSubmissionRequest;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Admission;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Server.Governance.AiMediation;
using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Gateway;

/// <summary>Verifies qualification, profile evidence and the mandatory approval floor.</summary>
public sealed class ChatBotAdmissionEvidenceValidatorTests
{
    /// <summary>Rejects classifier output that is qualified as an artifact but does not describe the current request or mandatory effects.</summary>
    [Theory]
    [InlineData("missing-input")]
    [InlineData("output-correlation")]
    [InlineData("input-correlation")]
    [InlineData("command")]
    [InlineData("output-policy")]
    [InlineData("input-policy")]
    [InlineData("joint-policy")]
    [InlineData("output-allowlist")]
    [InlineData("input-allowlist")]
    [InlineData("joint-allowlist")]
    [InlineData("output-default-risk")]
    [InlineData("input-default-risk")]
    [InlineData("effect-surface")]
    [InlineData("policy-classification")]
    [InlineData("output-effects")]
    [InlineData("input-effects")]
    [InlineData("joint-missing-effects")]
    [InlineData("low-risk")]
    [InlineData("empty-low-risk")]
    [InlineData("unknown-effect")]
    [InlineData("blank-authority")]
    [InlineData("undeclared-authority")]
    [InlineData("null-reason")]
    [InlineData("unsafe-reason")]
    [InlineData("unknown-schema")]
    [InlineData("visible-result")]
    [InlineData("missing-retention")]
    [InlineData("missing-produced-time")]
    [InlineData("future-produced-time")]
    public async Task QualifiedClassificationMustBindTheCurrentRequestAndTrustedEffectRiskFloor(string mismatch)
    {
        SyntheticAdmissionEvidenceSource source = new();
        ChatBotGatewayContext context = Context();
        AiActionRiskClassificationRecord record = context.RiskClassification!.Record!;
        context.SetRiskClassification(ChatBotRiskClassification.Classified(mismatch switch
        {
            "missing-input" => record with { InputTuple = null! },
            "null-reason" => record with { ReasonCode = null! },
            "unsafe-reason" => record with { ReasonCode = "raw result reason" },
            "unknown-schema" => record with { SchemaVersion = "unknown.v9" },
            "visible-result" => record with { RedactionState = "visible" },
            "missing-retention" => record with { RetentionClass = null! },
            "missing-produced-time" => record with { ProducedAtUtc = default },
            "future-produced-time" => record with { ProducedAtUtc = source.UtcNow.AddMinutes(1) },
            "output-correlation" => record with { CorrelationId = "another-correlation" },
            "input-correlation" => record with { InputTuple = record.InputTuple with { CorrelationId = "another-correlation" } },
            "command" => record with { InputTuple = record.InputTuple with { IntendedCommandName = AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName } },
            "output-policy" => record with { PolicySnapshotId = "another-policy" },
            "input-policy" => record with { InputTuple = record.InputTuple with { PolicySnapshotId = "another-policy" } },
            "joint-policy" => record with { PolicySnapshotId = "another-policy", InputTuple = record.InputTuple with { PolicySnapshotId = "another-policy" } },
            "output-allowlist" => record with { CommandAllowlistVersion = "stale" },
            "input-allowlist" => record with { InputTuple = record.InputTuple with { CommandAllowlistVersion = "stale" } },
            "joint-allowlist" => record with { CommandAllowlistVersion = "stale", InputTuple = record.InputTuple with { CommandAllowlistVersion = "stale" } },
            "output-default-risk" => record with { CommandDefaultRisk = AiActionRiskClass.LowRisk },
            "input-default-risk" => record with { InputTuple = record.InputTuple with { CommandDefaultRisk = AiActionRiskClass.LowRisk } },
            "effect-surface" => record with { InputTuple = record.InputTuple with { EffectSurface = "read-only" } },
            "policy-classification" => record with { InputTuple = record.InputTuple with { TenantPolicyClassification = "low-risk" } },
            "output-effects" => record with { RiskActionClasses = [] },
            "input-effects" => record with { InputTuple = record.InputTuple with { ActionClasses = [] } },
            "joint-missing-effects" => record with { RiskActionClasses = [], InputTuple = record.InputTuple with { ActionClasses = [] } },
            "low-risk" => record with { RiskClass = AiActionRiskClass.LowRisk },
            "empty-low-risk" => record with { RiskClass = AiActionRiskClass.LowRisk, RiskActionClasses = [], InputTuple = record.InputTuple with { ActionClasses = [] } },
            "blank-authority" => record with { RequesterAuthorityClass = "", InputTuple = record.InputTuple with { RequesterAuthorityClass = "" } },
            "undeclared-authority" => record with { RequesterAuthorityClass = "undeclared", InputTuple = record.InputTuple with { RequesterAuthorityClass = "undeclared" } },
            _ => record with { RiskActionClasses = [(AiActionRiskActionClass)999], InputTuple = record.InputTuple with { ActionClasses = [(AiActionRiskActionClass)999] } },
        }));

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateClassificationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);
        context.Idempotency.ShouldBeNull();
        context.AdmissionEnvelope.ShouldBeNull();
    }

    /// <summary>Verifies qualification Must Match Every Current Binding And Threshold.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("artifact")]
    [InlineData("candidate")]
    [InlineData("dependencies")]
    [InlineData("dataset")]
    [InlineData("environment")]
    [InlineData("expired")]
    [InlineData("revoked")]
    [InlineData("independence")]
    [InlineData("threshold")]
    [InlineData("boundary-downgrade")]
    [InlineData("invalid-output")]
    [InlineData("missing-effect-partition")]
    [InlineData("missing-runtime")]
    [InlineData("artifact-version")]
    [InlineData("zero-evaluated")]
    [InlineData("negative-misclassified")]
    [InlineData("too-many-misclassified")]
    [InlineData("future-approved")]
    [InlineData("approved-at-expiry")]
    [InlineData("approved-after-expiry")]
    [InlineData("blank-owner")]
    [InlineData("empty-evidence")]
    [InlineData("blank-evidence")]
    [InlineData("added-dependency")]
    [InlineData("removed-dependency")]
    public async Task QualificationMustMatchEveryCurrentBindingAndThreshold(string failure)
    {
        SyntheticAdmissionEvidenceSource source = new();
        ChatBotClassifierQualification qualified = source.Qualification!;
        if (failure == "missing-runtime") { source.RuntimeIdentity = null; }
        source.Qualification = failure switch
        {
            "missing" => null,
            "missing-runtime" => qualified,
            "artifact" => qualified with { Identity = qualified.Identity with { ArtifactHash = "drift" } },
            "artifact-version" => qualified with { Identity = qualified.Identity with { ArtifactVersion = "drift" } },
            "zero-evaluated" => qualified with { EvaluatedCount = 0 },
            "negative-misclassified" => qualified with { MisclassificationCount = -1 },
            "too-many-misclassified" => qualified with { MisclassificationCount = qualified.EvaluatedCount + 1 },
            "future-approved" => qualified with { ApprovedAt = source.UtcNow.AddMinutes(1) },
            "approved-at-expiry" => qualified with { ApprovedAt = source.UtcNow.AddMinutes(-1), ExpiresAt = source.UtcNow.AddMinutes(-1) },
            "approved-after-expiry" => qualified with { ApprovedAt = source.UtcNow.AddMinutes(-1), ExpiresAt = source.UtcNow.AddMinutes(-2) },
            "blank-owner" => qualified with { OwnerId = "" },
            "empty-evidence" => qualified with { EvidenceReferences = [] },
            "blank-evidence" => qualified with { EvidenceReferences = [""] },
            "added-dependency" => qualified with { Identity = qualified.Identity with { DependencyRevisions = new Dictionary<string, string> { ["owner"] = "synthetic-dependency", ["other"] = "extra" } } },
            "removed-dependency" => qualified with { Identity = qualified.Identity with { DependencyRevisions = new Dictionary<string, string>() } },
            "candidate" => qualified with { Identity = qualified.Identity with { CandidateRevision = "drift" } },
            "dependencies" => qualified with { Identity = qualified.Identity with { DependencyRevisions = new Dictionary<string, string> { ["owner"] = "drift" } } },
            "dataset" => qualified with { Identity = qualified.Identity with { DatasetHash = "drift" } },
            "environment" => qualified with { Identity = qualified.Identity with { EnvironmentProfile = "drift" } },
            "expired" => qualified with { ExpiresAt = source.UtcNow },
            "revoked" => qualified with { ComputedStatus = "invalidated" },
            "independence" => qualified with { IndependentApproverId = qualified.OwnerId },
            "threshold" => qualified with { MisclassificationCount = 11 },
            "boundary-downgrade" => qualified with { BoundaryDowngradeCount = 1 },
            "invalid-output" => qualified with { InvalidOutputCount = 1 },
            _ => qualified with { CoveredEffectClasses = new HashSet<AiActionRiskActionClass> { AiActionRiskActionClass.ModifiesState } },
        };
        ChatBotAdmissionEvidenceValidator validator = new(source, source);
        (await validator.ValidateClassificationAsync(Context(), TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);
    }

    /// <summary>Verifies synthetic Qualified Record Only Validates Classification And Never Enables Atomic Execution.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task SyntheticQualifiedRecordOnlyValidatesClassificationAndNeverEnablesAtomicExecution(int misclassified)
    {
        SyntheticAdmissionEvidenceSource source = new();
        source.Qualification = source.Qualification! with { MisclassificationCount = misclassified };
        ChatBotAdmissionEvidenceValidator validator = new(source, source);
        ChatBotGatewayContext context = Context();
        (await validator.ValidateClassificationAsync(context, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
        (await validator.ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>Verifies missing Current Owner Profile Evidence Cannot Be Supplied By Caller Payload.</summary>
    [Fact]
    public async Task MissingCurrentOwnerProfileEvidenceCannotBeSuppliedByCallerPayload()
    {
        ChatBotAdmissionEvidenceValidator validator = new();
        ChatBotGatewayContext context = Context();
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>Verifies synthetic Qualified Read Proofs Require Current Policy Context And Project Authority Without Proposal Approval.</summary>
    [Theory]
    [InlineData("current", null)]
    [InlineData("missing-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData("policy-low-risk-disallowed", ChatBotMessageCodes.ApprovalRequired)]
    [InlineData("policy-effect-surface", ChatBotMessageCodes.ApprovalRequired)]
    [InlineData("policy-kind-missing", ChatBotMessageCodes.ApprovalRequired)]
    [InlineData("context-drift", ChatBotMessageCodes.ProjectAiContextPackageUnavailable)]
    [InlineData("revision-drift", ChatBotMessageCodes.CommandContractInvalid)]
    [InlineData("expired", ChatBotMessageCodes.CanonicalExecutionUnavailable)]
    [InlineData("project-proof-missing", ChatBotMessageCodes.CanonicalExecutionUnavailable)]
    public async Task SyntheticQualifiedReadProofsRequireCurrentPolicyContextAndProjectAuthorityWithoutProposalApproval(string state, string? expected)
    {
        SyntheticAdmissionEvidenceSource source = new();
        ChatBotGatewayContext context = ReadContext();
        TenantAiPolicySnapshot policy = new("synthetic-tenant", 1, "synthetic-policy", true, "read-only", ["summarize-visible-context"], true, true);
        ProjectAiContextPackage package = new("synthetic-tenant", "synthetic-project", "synthetic-policy", "metadata_only", "collaboration_input", "disabled",
            "synthetic-package", "v1", ProjectAiContextPackage.SchemaVersionValue, 1, context.Submission.CorrelationId, [], [], ["synthetic:source"], "synthetic", ProjectAiContextPackage.DerivationKernelVersionValue);
        source.ProfileEvidence = new("synthetic-tenant", "synthetic-requester", context.Submission.Request.CommandType, context.Submission.Request.CommandId,
            ChatBotAdmissionProfile.AiRead, 1, null, ChatBotCommandAdmissionCatalog.RequiredStages(ChatBotAdmissionProfile.AiRead).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1), policy, package);
        source.ProfileEvidence = state switch
        {
            "missing-policy" => source.ProfileEvidence with { Policy = null },
            "policy-low-risk-disallowed" => source.ProfileEvidence with { Policy = policy with { LowRiskAllowed = false } },
            "policy-effect-surface" => source.ProfileEvidence with { Policy = policy with { EffectSurface = "project-conversation" } },
            "policy-kind-missing" => source.ProfileEvidence with { Policy = policy with { AssistanceKinds = [] } },
            "context-drift" => source.ProfileEvidence with { ContextPackage = package with { PackageVersion = "v2" } },
            "revision-drift" => source.ProfileEvidence with { CurrentRevision = 2 },
            "expired" => source.ProfileEvidence with { ExpiresAt = source.UtcNow },
            "project-proof-missing" => source.ProfileEvidence with { StageReferences = new Dictionary<string, string>() },
            _ => source.ProfileEvidence,
        };
        ChatBotAdmissionEvidenceValidator validator = new(source, source);
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBe(expected);
        source.ProfileEvidence.Approval.ShouldBeNull();
        (await validator.ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>All collections supplied to the provider must equal the operation-bound package's references and exclusion reasons.</summary>
    [Theory]
    [InlineData("current")]
    [InlineData("source-substitution")]
    [InlineData("context-substitution")]
    [InlineData("exclusion-substitution")]
    [InlineData("missing-source")]
    [InlineData("missing-context")]
    [InlineData("missing-exclusions")]
    [InlineData("extra-context")]
    [InlineData("extra-exclusion")]
    public async Task ReadProfileMustBindEveryProviderCollectionToTheTrustedPackage(string mutation)
    {
        ChatBotGatewayContext context = ReadContext();
        ExecuteLowRiskAIAssistance command = (ExecuteLowRiskAIAssistance)context.Submission.Request.Command;
        command = command with
        {
            SourceEvidenceReferences = ["source-two", "source-one"],
            AuthorizedContextReferences = ["file-two", "file-one"],
            ExcludedContextReasons = ["restricted", "unsafe"],
        };
        context.Submission.Request.Command = mutation switch
        {
            "source-substitution" => command with { SourceEvidenceReferences = ["other-source", "source-one"] },
            "context-substitution" => command with { AuthorizedContextReferences = ["other-file", "file-one"] },
            "exclusion-substitution" => command with { ExcludedContextReasons = ["other-reason", "unsafe"] },
            "missing-source" => command with { SourceEvidenceReferences = null! },
            "missing-context" => command with { AuthorizedContextReferences = null! },
            "missing-exclusions" => command with { ExcludedContextReasons = null! },
            "extra-context" => command with { AuthorizedContextReferences = ["file-one", "file-two", "other-file"] },
            "extra-exclusion" => command with { ExcludedContextReasons = ["restricted", "unsafe", "other-reason"] },
            _ => command,
        };
        SyntheticAdmissionEvidenceSource source = new();
        ProjectAiContextPackage package = new("synthetic-tenant", command.ProjectId, "synthetic-policy", "metadata_only", "collaboration_input", "disabled",
            command.ContextPackageId, command.ContextPackageVersion, ProjectAiContextPackage.SchemaVersionValue, 1, context.Submission.CorrelationId,
            [new("file-one", "folder-one", "one", "attachment-one", "metadata_only", "collaboration_input", "source-one"),
                new("file-two", "folder-two", "two", "attachment-two", "metadata_only", "collaboration_input", "source-two")],
            [new("excluded-one", "unsafe"), new("excluded-two", "restricted")], ["source-one", "source-two"], "synthetic", ProjectAiContextPackage.DerivationKernelVersionValue);
        source.ProfileEvidence = new("synthetic-tenant", "synthetic-requester", context.Submission.Request.CommandType, context.Submission.Request.CommandId,
            ChatBotAdmissionProfile.AiRead, 1, null,
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(context.Submission.Request.CommandType).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1),
            new("synthetic-tenant", 1, "synthetic-policy", true, "read-only", ["summarize-visible-context"], true, true), package);
        ChatBotAdmissionEvidenceValidator validator = new(source, source);
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(mutation == "current" ? null : ChatBotMessageCodes.ProjectAiContextPackageUnavailable);
        context.AdmissionEnvelope.ShouldBeNull();
        context.Idempotency.ShouldBeNull();
        (await validator.ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>Commands without a classification input retain current operation-bound owner policy instead of requiring an absent wire field.</summary>
    [Theory]
    [InlineData(nameof(DecideAiActionApproval), "current", null)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "current", null)]
    [InlineData(nameof(DecideAiActionApproval), "stale-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "stale-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(DecideAiActionApproval), "invalid-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "invalid-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(DecideAiActionApproval), "foreign-tenant", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "foreign-tenant", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(DecideAiActionApproval), "missing-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "missing-policy", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(DecideAiActionApproval), "other-operation", ChatBotMessageCodes.CanonicalExecutionUnavailable)]
    [InlineData(nameof(MarkAiActionProposalInvalidatedByCorrection), "other-operation", ChatBotMessageCodes.CanonicalExecutionUnavailable)]
    [InlineData(nameof(ProposeAIAction), "current", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(ExecuteApprovedAIAction), "current", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    [InlineData(nameof(ExecuteLowRiskAIAssistance), "current", ChatBotRefusalReasonCodes.PolicySnapshotUnavailable)]
    public async Task OwnerPolicyWithoutAWireSnapshotOnlyAppliesToClassificationExemptCommands(string commandType, string state, string? expected)
    {
        ChatBotGatewayContext original = ReadContext();
        original.Submission.Request.CommandType = commandType;
        original.Submission.Request.Command = new { projectId = "synthetic-project" };
        ChatBotGatewayContext context = new(original.Submission, original.Actor, original.TenantBinding);
        ChatBotAdmissionProfile profile = commandType == nameof(ExecuteLowRiskAIAssistance) ? ChatBotAdmissionProfile.AiRead : ChatBotAdmissionProfile.AiEffect;
        context.SelectAdmissionProfile(profile);
        SyntheticAdmissionEvidenceSource source = new();
        TenantAiPolicySnapshot policy = new("synthetic-tenant", 1, "synthetic-policy", false, "project-conversation", [], true, true);
        source.ProfileEvidence = new("synthetic-tenant", "synthetic-requester", commandType, context.Submission.Request.CommandId, profile, 1, null,
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(commandType).ToDictionary(stage => stage, stage => "synthetic:" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1), state switch
            {
                "stale-policy" => policy with { IsFresh = false },
                "invalid-policy" => policy with { IsValid = false },
                "foreign-tenant" => policy with { TenantId = "other-tenant" },
                "missing-policy" => null,
                _ => policy,
            });
        if (state == "other-operation") { source.ProfileEvidence = source.ProfileEvidence with { OperationId = "other-operation" }; }
        ChatBotAdmissionEvidenceValidator validator = new(source, source);
        (await validator.ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBe(expected);
        context.RiskClassification.ShouldBeNull();
        context.AdmissionEnvelope.ShouldBeNull();
        context.Idempotency.ShouldBeNull();
        (await validator.ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken)).ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>Pre-send commands need their applicable sender/content proofs, while opaque approval references never authorize a send.</summary>
    [Theory]
    [InlineData(nameof(CreateOutboundDraft), null)]
    [InlineData(nameof(RequestOutboundSendApproval), null)]
    [InlineData(nameof(DecideOutboundApproval), null)]
    [InlineData(nameof(ExecuteApprovedOutboundDraft), ChatBotRefusalReasonCodes.ApprovalStateInvalid)]
    public async Task OutboundStagesMustMatchTheCommandLifecycleAndRequireStructuredSendApproval(string commandType, string? expected)
    {
        ChatBotGatewayContext context = ReadContext();
        context.Submission.Request.CommandType = commandType;
        context.Submission.Request.Command = new { draftId = "synthetic-draft" };
        context.Submission.Request.Concurrency = null;
        context = new(context.Submission, context.Actor, context.TenantBinding);
        context.SelectAdmissionProfile(ChatBotAdmissionProfile.HumanEffect);
        SyntheticAdmissionEvidenceSource source = new();
        source.ProfileEvidence = new(context.TenantBinding.TenantId, context.Actor.ActorId, commandType, context.Submission.Request.CommandId,
            ChatBotAdmissionProfile.HumanEffect, 1, "synthetic:owner-concurrency",
            ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(commandType).ToDictionary(stage => stage, stage => "synthetic:opaque-" + stage),
            source.UtcNow.AddSeconds(-1), source.UtcNow.AddMinutes(1));

        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateProfileAsync(context, TestContext.Current.CancellationToken)).ShouldBe(expected);
        context.Idempotency.ShouldBeNull();
        context.AdmissionEnvelope.ShouldBeNull();
        (await new ChatBotAdmissionEvidenceValidator(source, source).ValidateAtomicParticipationAsync(context, TestContext.Current.CancellationToken))
            .ShouldBe(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }

    /// <summary>Verifies mandatory Effect Must Stop Before Even APermissive Policy Can Downgrade It.</summary>
    [Theory]
    [InlineData(AiActionRiskActionClass.ModifiesState)]
    [InlineData(AiActionRiskActionClass.ExposesFiles)]
    [InlineData(AiActionRiskActionClass.SendsExternal)]
    [InlineData(AiActionRiskActionClass.CreatesTasks)]
    [InlineData(AiActionRiskActionClass.InvokesTools)]
    [InlineData(AiActionRiskActionClass.ActsOnBehalf)]
    public async Task MandatoryEffectMustStopBeforeEvenAPermissivePolicyCanDowngradeIt(AiActionRiskActionClass effect)
    {
        ChatBotGatewayContext context = ReadContext();
        AiActionRiskClassificationRecord forged = context.RiskClassification!.Record! with
        {
            RiskClass = AiActionRiskClass.LowRisk,
            RiskActionClasses = [effect],
        };
        context.SetRiskClassification(ChatBotRiskClassification.Classified(forged));
        SyntheticPermissiveAiActionPolicyEvaluator policy = new();
        ChatBotApprovalResult result = await new AiActionApprovalGate(policy).EvaluateAsync(context, TestContext.Current.CancellationToken);
        result.Kind.ShouldBe(ChatBotApprovalResultKind.RoutedToApproval);
        policy.Calls.ShouldBe(0);
    }

    /// <summary>Rejects undeclared assistance subtypes before policy evaluation or effect bookkeeping.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task UndeclaredAssistanceSubtypeMustStopBeforePolicyEvaluation(int subtype)
    {
        ChatBotGatewayContext original = ReadContext();
        ExecuteLowRiskAIAssistance command = (ExecuteLowRiskAIAssistance)original.Submission.Request.Command;
        ChatBotGatewayContext context = original with
        {
            Submission = original.Submission with
            {
                Request = new CommandSubmissionRequest
                {
                    CommandId = original.Submission.Request.CommandId,
                    CommandType = original.Submission.Request.CommandType,
                    Command = command with { AssistanceKind = (LowRiskAiAssistanceKind)subtype },
                    Concurrency = original.Submission.Request.Concurrency,
                },
            },
        };

        ChatBotRiskClassification classification = await new DeterministicAiActionRiskClassifier()
            .ClassifyAsync(context, TestContext.Current.CancellationToken);
        classification.Rejected.ShouldBeTrue();
        classification.RejectionReasonCode.ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);

        SyntheticPermissiveAiActionPolicyEvaluator policy = new();
        ChatBotApprovalResult approval = await new AiActionApprovalGate(policy)
            .EvaluateAsync(context, TestContext.Current.CancellationToken);
        approval.Kind.ShouldBe(ChatBotApprovalResultKind.Blocked);
        approval.ReasonCode.ShouldBe(ChatBotMessageCodes.ClassifierIndeterminate);
        policy.Calls.ShouldBe(0);
        context.AdmissionEnvelope.ShouldBeNull();
        context.Idempotency.ShouldBeNull();
        context.ExternalEffectAttempted.ShouldBeFalse();
    }

    /// <summary>Creates a synthetic authorized read-only operation context.</summary>
    internal static ChatBotGatewayContext ReadContext()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "synthetic-requester")], "synthetic"));
        ExecuteLowRiskAIAssistance command = new("synthetic-project", "synthetic-proposal", "synthetic-intent", "synthetic-source", "synthetic-requester",
            LowRiskAiAssistanceKind.SummarizeVisibleContext, "synthetic-package", "v1", "metadata_only", "collaboration_input", "disabled",
            ["synthetic:source"], [], [], 1, "synthetic-policy", "01ARZ3NDEKTSV4RRFFQ69G5FAW", "synthetic-execution", "synthetic-transition");
        ChatBotCommandSubmission submission = new(principal, new CommandSubmissionRequest
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", CommandType = nameof(ExecuteLowRiskAIAssistance), Command = command,
            Concurrency = new() { ExpectedRevision = 1 },
        }, command.CorrelationId, null);
        ChatBotGatewayContext context = new(submission, new("synthetic-requester", principal), new("synthetic-tenant"));
        context.SelectAdmissionProfile(ChatBotAdmissionProfile.AiRead);
        context.SetRiskClassification(ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName, [], null, null, "project-contributor", "synthetic-policy",
            null, null, null, "authorized", submission.CorrelationId))));
        return context;
    }

    private static ChatBotGatewayContext Context()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity([new Claim("sub", "synthetic-requester")], "synthetic"));
        ChatBotCommandSubmission submission = new(principal, new CommandSubmissionRequest
        {
            CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY", CommandType = nameof(Contracts.Commands.ProposeAIAction),
            Command = new { intendedCommandName = AiActionCommandMetadataProvider.AppendConversationMessageCommandName, expectedSourceVersion = 1, policySnapshotId = "synthetic-policy" },
        }, "01ARZ3NDEKTSV4RRFFQ69G5FAW", null);
        ChatBotGatewayContext context = new(submission, new("synthetic-requester", principal), new("synthetic-tenant"));
        context.SelectAdmissionProfile(ChatBotAdmissionProfile.AiEffect);
        context.SetRiskClassification(ChatBotRiskClassification.Classified(AiActionRiskClassifier.Classify(new AiActionRiskInputTuple(
            AiActionCommandMetadataProvider.AppendConversationMessageCommandName, [], null, null, "project-contributor", "synthetic-policy",
            null, null, null, "authorized", submission.CorrelationId))));
        return context;
    }
}
