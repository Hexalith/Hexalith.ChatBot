using System.Text.Json;
using Hexalith.ChatBot.Server.Audit;
using Hexalith.ChatBot.Server.Operations;
using Hexalith.ChatBot.Server.Authorization;
using Hexalith.ChatBot.Server.Gateway.Stages;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Governance.AiMediation;

namespace Hexalith.ChatBot.Server.Gateway.Admission;

/// <summary>
/// Fails closed while the exact-artifact A9a registry, current owner profile proofs and A13 atomic hook
/// are unavailable. Local policy, a classifier version string, a signed marker, SDK acceptance and
/// preparation records are deliberately insufficient evidence for any of those boundaries.
/// </summary>
internal sealed class ChatBotAdmissionEvidenceValidator(
    IChatBotAdmissionEvidenceSource? evidenceSource = null,
    ISystemClock? clock = null) : IChatBotAdmissionEvidenceValidator
{
    private readonly IChatBotAdmissionEvidenceSource _evidenceSource = evidenceSource ?? new UnavailableChatBotAdmissionEvidenceSource();
    private readonly ISystemClock _clock = clock ?? new SystemClock();
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async ValueTask<string?> ValidateOperationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!AuditMetadata.IsSafeStableIdentifier(context.Submission.Request.CommandId))
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }

            JsonElement payload = context.Submission.Request.Command is JsonElement element
                ? element : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);
            if (payload.ValueKind != JsonValueKind.Object)
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }

            if (payload.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != payload.EnumerateObject().Count())
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            foreach (JsonProperty property in payload.EnumerateObject())
            {
                if (property.Name.Equals("admissionProfile", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("profile", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("successorCommand", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Equals("nextCommand", StringComparison.OrdinalIgnoreCase) ||
                    ((property.Name.Equals("riskClassification", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("executionRecord", StringComparison.OrdinalIgnoreCase)) && property.Value.ValueKind != JsonValueKind.Null))
                {
                    return ChatBotMessageCodes.CommandContractInvalid;
                }
            }

            if (context.Submission.Request.CommandType == nameof(ProposeAIAction))
            {
                ProposeAIAction proposal = payload.Deserialize<ProposeAIAction>(ReadOptions)!;
                AiActionCommandMetadata? metadata = AiActionCommandMetadataProvider.TryGet(proposal.IntendedCommandName);
                if (metadata is null) { return ChatBotAuthorizationReasonCodes.CommandNotAllowlisted; }
                if (proposal.CommandDefaultRisk is { } claimedRisk && claimedRisk != metadata.CommandDefaultRisk ||
                    proposal.EffectSurface is { } claimedEffect && claimedEffect != metadata.EffectSurface ||
                    proposal.TenantPolicyClassification is { } claimedPolicy && claimedPolicy != metadata.TenantPolicyClassification ||
                    proposal.CommandMetadataSupported is false)
                {
                    return ChatBotMessageCodes.CommandContractInvalid;
                }
            }
            if (context.Submission.Request.CommandType == nameof(ExecuteApprovedAIAction) &&
                ReadString(payload, "commandName") != AiActionCommandMetadataProvider.AppendConversationMessageCommandName)
            {
                return ChatBotAuthorizationReasonCodes.CommandNotAllowlisted;
            }

            if (context.Submission.Request.Concurrency is { ExpectedRevision: < 0 })
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            if (!TryReadDeclaredRevisions(context, payload, out long[] expected))
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            bool hasRevision = context.Submission.Request.Concurrency is not null || expected.Length > 0;
            ChatBotProfileAdmissionEvidence? proof;
            try
            {
                proof = await _evidenceSource.ReadProfileEvidenceAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return ChatBotMessageCodes.CanonicalExecutionUnavailable;
            }
            if (!MatchesCurrentOperation(context, proof) || !hasRevision && string.IsNullOrWhiteSpace(proof!.OwnerConcurrencyGuardReference)) { return ChatBotMessageCodes.CanonicalExecutionUnavailable; }
            if (context.Submission.Request.Concurrency is { } concurrency && concurrency.ExpectedRevision != proof!.CurrentRevision)
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            if (expected.Length == 1 && expected[0] != proof!.CurrentRevision) { return ChatBotMessageCodes.CommandContractInvalid; }
            context.SetAdmissionEvidence(proof!);
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return exception is JsonException or InvalidOperationException or ArgumentException
                ? ChatBotMessageCodes.CommandContractInvalid : ChatBotMessageCodes.CanonicalExecutionUnavailable;
        }
    }

    private bool MatchesCurrentOperation(ChatBotGatewayContext context, ChatBotProfileAdmissionEvidence? proof)
        => proof is not null && context.AdmissionProfile is { } profile && proof.Profile == profile &&
            proof.TenantId == context.TenantBinding.TenantId && proof.ActorId == context.Actor.ActorId &&
            proof.CommandType == context.Submission.Request.CommandType && proof.OperationId == context.Submission.Request.CommandId &&
            proof.ObservedAt <= _clock.UtcNow && proof.ExpiresAt > _clock.UtcNow && proof.ObservedAt < proof.ExpiresAt && proof.CurrentRevision >= 0;

    /// <inheritdoc />
    public async ValueTask<string?> ValidateClassificationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ChatBotClassifierRuntimeIdentity? runtime = _evidenceSource.RuntimeIdentity;
            ChatBotClassifierQualification? qualification = await _evidenceSource.ReadQualificationAsync(cancellationToken).ConfigureAwait(false);
            if (runtime is null || qualification is null || context.RiskClassification?.Record is not { } record ||
                !GovernedOperationAggregate.IsSafeClassificationMetadata(record) ||
                record.SchemaVersion != "chatbot.ai-action-risk-classification.v1" || record.RedactionState != "metadata_only" ||
                record.ProducedAtUtc > _clock.UtcNow || record.IndeterminateReason is not null || !MatchesClassificationRequest(context, record) ||
                record.ClassifierVersion != AiActionRiskClassifier.CurrentVersion || record.ClassifierVersion != runtime.ArtifactVersion ||
                qualification.ComputedStatus != "approved-current" || !SameIdentity(runtime, qualification.Identity) ||
                string.IsNullOrWhiteSpace(qualification.OwnerId) || string.IsNullOrWhiteSpace(qualification.IndependentApproverId) ||
                qualification.OwnerId == qualification.IndependentApproverId ||
                qualification.ApprovedAt > _clock.UtcNow || qualification.ExpiresAt <= _clock.UtcNow ||
                qualification.ApprovedAt >= qualification.ExpiresAt || qualification.EvaluatedCount <= 0 ||
                qualification.MisclassificationCount < 0 || qualification.MisclassificationCount > qualification.EvaluatedCount ||
                (double)qualification.MisclassificationCount / qualification.EvaluatedCount > 0.01 ||
                qualification.BoundaryDowngradeCount != 0 || qualification.InvalidOutputCount != 0 ||
                !qualification.CoveredEffectClasses.SetEquals(Enum.GetValues<AiActionRiskActionClass>()) ||
                qualification.EvidenceReferences.Count == 0 || qualification.EvidenceReferences.Any(string.IsNullOrWhiteSpace))
            {
                return ChatBotMessageCodes.ClassifierIndeterminate;
            }
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ChatBotMessageCodes.ClassifierIndeterminate;
        }
    }

    private static bool MatchesClassificationRequest(ChatBotGatewayContext context, AiActionRiskClassificationRecord record)
    {
        JsonElement payload = Payload(context);
        string? commandName = context.Submission.Request.CommandType switch
        {
            nameof(ExecuteLowRiskAIAssistance) => AiActionCommandMetadataProvider.ExecuteLowRiskAssistanceCommandName,
            nameof(ProposeAIAction) => ReadString(payload, "intendedCommandName"),
            nameof(ExecuteApprovedAIAction) => ReadString(payload, "commandName"),
            _ => null,
        };
        AiActionCommandMetadata? metadata = commandName is null ? null : AiActionCommandMetadataProvider.TryGet(commandName);
        string? policy = ReadString(payload, "policySnapshotId");
        string? claimedAllowlist = ReadString(payload, "commandAllowlistVersion");
        if (metadata is null || record.InputTuple is not { } input || record.RiskActionClasses is null || input.ActionClasses is null ||
            record.CorrelationId != context.Submission.CorrelationId || input.CorrelationId != context.Submission.CorrelationId ||
            input.IntendedCommandName != metadata.CommandName || string.IsNullOrWhiteSpace(policy) ||
            record.PolicySnapshotId != policy || input.PolicySnapshotId != policy ||
            record.CommandAllowlistVersion != metadata.CommandAllowlistVersion || input.CommandAllowlistVersion != metadata.CommandAllowlistVersion ||
            claimedAllowlist is not null && claimedAllowlist != metadata.CommandAllowlistVersion ||
            record.CommandDefaultRisk != metadata.CommandDefaultRisk || input.CommandDefaultRisk != metadata.CommandDefaultRisk ||
            input.EffectSurface != metadata.EffectSurface || input.TenantPolicyClassification != metadata.TenantPolicyClassification ||
            input.AllowlistMetadataState != "declared" || input.ProjectAuthorizationState != "authorized" ||
            record.RequesterAuthorityClass != input.RequesterAuthorityClass ||
            !AiActionRiskClassifier.IsDeclaredRequesterAuthority(input.RequesterAuthorityClass) ||
            record.RiskActionClasses.Any(effect => !Enum.IsDefined(effect)) || input.ActionClasses.Any(effect => !Enum.IsDefined(effect)) ||
            !record.RiskActionClasses.ToHashSet().SetEquals(input.ActionClasses) ||
            metadata.ActionClasses.Any(effect => !record.RiskActionClasses.Contains(effect)) ||
            (metadata.CommandDefaultRisk == AiActionRiskClass.ApprovalRequired || record.RiskActionClasses.Count > 0) && record.RiskClass != AiActionRiskClass.ApprovalRequired)
        {
            return false;
        }
        return context.Submission.Request.CommandType != nameof(ProposeAIAction) ||
            (payload.Deserialize<ProposeAIAction>(ReadOptions)!.ProposedActionClasses ?? []).All(record.RiskActionClasses.Contains);
    }

    private static bool SameIdentity(ChatBotClassifierRuntimeIdentity current, ChatBotClassifierRuntimeIdentity qualified)
        => !string.IsNullOrWhiteSpace(current.ArtifactHash) && !string.IsNullOrWhiteSpace(current.CandidateRevision) &&
            !string.IsNullOrWhiteSpace(current.EnvironmentProfile) && !string.IsNullOrWhiteSpace(current.DatasetVersion) &&
            !string.IsNullOrWhiteSpace(current.DatasetHash) && current.DependencyRevisions.Count > 0 &&
            current.DependencyRevisions.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)) &&
            current.ArtifactVersion == qualified.ArtifactVersion && current.ArtifactHash == qualified.ArtifactHash &&
            current.CandidateRevision == qualified.CandidateRevision && current.EnvironmentProfile == qualified.EnvironmentProfile &&
            current.DatasetVersion == qualified.DatasetVersion && current.DatasetHash == qualified.DatasetHash &&
            current.DependencyRevisions.Count == qualified.DependencyRevisions.Count &&
            current.DependencyRevisions.All(pair => qualified.DependencyRevisions.TryGetValue(pair.Key, out string? revision) && revision == pair.Value);

    /// <inheritdoc />
    public async ValueTask<string?> ValidateProfileAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            ChatBotProfileAdmissionEvidence? proof = context.AdmissionEvidence ?? await _evidenceSource.ReadProfileEvidenceAsync(context, cancellationToken).ConfigureAwait(false);
            if (!MatchesCurrentOperation(context, proof) || proof is null)
            {
                return ChatBotMessageCodes.CanonicalExecutionUnavailable;
            }
            ChatBotAdmissionProfile profile = context.AdmissionProfile!.Value;
            if (context.Submission.Request.Concurrency is { } concurrency && concurrency.ExpectedRevision != proof.CurrentRevision)
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            JsonElement payload = Payload(context);
            if (!TryReadDeclaredRevisions(context, payload, out long[] expectedRevisions))
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            if (expectedRevisions.Length == 1 && expectedRevisions[0] != proof.CurrentRevision)
            {
                return ChatBotMessageCodes.CommandContractInvalid;
            }
            if (context.Submission.Request.Concurrency is null && expectedRevisions.Length == 0 &&
                string.IsNullOrWhiteSpace(proof.OwnerConcurrencyGuardReference))
            {
                return ChatBotMessageCodes.CanonicalExecutionUnavailable;
            }
            IReadOnlyList<string> required = ChatBotCommandAdmissionCatalog.RequiredStagesForCommand(context.Submission.Request.CommandType);
            if (required.Any(stage => !proof.StageReferences.TryGetValue(stage, out string? reference) || string.IsNullOrWhiteSpace(reference)))
            {
                return ChatBotMessageCodes.CanonicalExecutionUnavailable;
            }
            if (context.Submission.Request.CommandType == nameof(ExecuteApprovedOutboundDraft))
            {
                // No owner source supplies structured outbound approval yet. A nonblank reference cannot prove
                // independent human presence, expiry, exact scope or the two expected revisions.
                return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
            }
            if (profile is ChatBotAdmissionProfile.AiRead or ChatBotAdmissionProfile.AiEffect)
            {
                if (proof.Policy is not { IsValid: true, IsFresh: true, PolicyVersion: > 0 } policy ||
                    policy.TenantId != context.TenantBinding.TenantId || string.IsNullOrWhiteSpace(policy.PolicySnapshotId) ||
                    ChatBotCommandAdmissionCatalog.RequiresClassification(context.Submission.Request.CommandType) &&
                        policy.PolicySnapshotId != ReadString(payload, "policySnapshotId"))
                {
                    return ChatBotRefusalReasonCodes.PolicySnapshotUnavailable;
                }
                if (profile == ChatBotAdmissionProfile.AiRead)
                {
                    ExecuteLowRiskAIAssistance command = payload.Deserialize<ExecuteLowRiskAIAssistance>(ReadOptions)!;
                    if (!Enum.IsDefined(command.AssistanceKind) || context.RiskClassification?.Record is not { RiskClass: AiActionRiskClass.LowRisk, RiskActionClasses.Count: 0 })
                    {
                        return ChatBotMessageCodes.ClassifierIndeterminate;
                    }
                    if (!policy.LowRiskAllowed || policy.EffectSurface != "read-only" ||
                        !policy.AssistanceKinds.Contains(AiActionApprovalGate.AssistanceKindToken(command.AssistanceKind), StringComparer.Ordinal))
                    {
                        return ChatBotMessageCodes.ApprovalRequired;
                    }
                    if (proof.ContextPackage is not { } package || package.TenantId != proof.TenantId || package.ProjectId != command.ProjectId ||
                        package.PackageId != command.ContextPackageId || package.PackageVersion != command.ContextPackageVersion ||
                        package.PolicySnapshotId != policy.PolicySnapshotId || package.SourceVersion < 0 ||
                        package.RedactionDecision != command.ContextPackageRedactionState || package.RetentionClass != command.RetentionClass ||
                        package.ProviderReuseSetting != command.ProviderReuseSetting ||
                        !SameReferences(command.SourceEvidenceReferences, package.SourceEvidenceReferences) ||
                        !SameReferences(command.AuthorizedContextReferences, package.IncludedFiles?.Select(file => file.ReferenceToken)) ||
                        !SameReferences(command.ExcludedContextReasons, package.ExcludedFiles?.Select(file => file.ReasonCode)))
                    {
                        return ChatBotMessageCodes.ProjectAiContextPackageUnavailable;
                    }
                }
                if (context.Submission.Request.CommandType == nameof(ExecuteApprovedAIAction))
                {
                    ExecuteApprovedAIAction command = payload.Deserialize<ExecuteApprovedAIAction>(ReadOptions)!;
                    if (ValidateApproval(context, command, proof.Approval) is { } failure) { return failure; }
                }
            }
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ChatBotMessageCodes.CanonicalExecutionUnavailable;
        }
    }

    private string? ValidateApproval(ChatBotGatewayContext context, ExecuteApprovedAIAction command, ChatBotHumanApprovalEvidence? approval)
    {
        if (approval is null || command.ApprovalId != approval.Request.ApprovalId || command.ApprovalId != approval.Decision.ApprovalId ||
            approval.Decision.AuthorityResult != "authorized" || approval.Decision.DisabledReason is not null || approval.Decision.AuditStatus != "available" ||
            approval.Decision.DecisionActorType != "human" || approval.Approver.Context.IsMachine ||
            approval.Approver.Context.ActorClass != "human" || approval.Approver.Context.TenantId != context.TenantBinding.TenantId ||
            approval.Approver.Context.SubjectId != approval.Decision.DecisionActorId ||
            approval.Decision.DecisionActorId == approval.Request.RequesterId ||
            !approval.Approver.IsCurrent(_clock.UtcNow) || !approval.Approver.HasProject(command.ProjectId) ||
            string.IsNullOrWhiteSpace(approval.PresenceReference) || approval.Request.RequestedAtUtc == default ||
            approval.Decision.DecidedAtUtc == default || approval.Request.RequestedAtUtc > _clock.UtcNow ||
            approval.Request.RequestedAtUtc > approval.Decision.DecidedAtUtc || approval.Decision.DecidedAtUtc > _clock.UtcNow ||
            approval.ExpiresAt <= _clock.UtcNow || approval.ExpiresAt <= approval.Decision.DecidedAtUtc ||
            string.IsNullOrWhiteSpace(approval.ScopeDigest) || approval.ScopeDigest != approval.CurrentScopeDigest ||
            context.RiskClassification?.Record is not { RiskActionClasses: not null } classification ||
            approval.ClassifierVersion != classification.ClassifierVersion || approval.Request.AiRiskClass != classification.RiskClass ||
            approval.Request.RiskInputTuple != GovernedOperationAggregate.RiskInputTupleRef(classification.InputTuple) ||
            !SameReferences(approval.Request.AiRiskActionClasses, classification.RiskActionClasses.Select(GovernedOperationAggregate.AiRiskActionClassToken)) ||
            command.PolicySnapshotId != approval.Request.PolicySnapshotId || command.PolicySnapshotId != approval.Decision.PolicySnapshotId)
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }
        AiActionCommandMetadata? metadata = AiActionCommandMetadataProvider.TryGet(command.CommandName);
        if (metadata is null || command.CommandAllowlistVersion != metadata.CommandAllowlistVersion ||
            approval.Request.CommandAllowlistVersion != metadata.CommandAllowlistVersion)
        {
            return ChatBotRefusalReasonCodes.CommandNotAllowlisted;
        }
        bool sensitive = classification.RiskActionClasses.Any(effect => effect != AiActionRiskActionClass.ModifiesState);
        if (approval.ExpiresAt - approval.Decision.DecidedAtUtc > TimeSpan.FromMinutes(sensitive ? 10 : 30))
        {
            return ChatBotRefusalReasonCodes.ApprovalStateInvalid;
        }
        return GovernedOperationAggregate.ValidateApprovedExecutionApproval(command, approval.Request, approval.Decision);
    }

    private static bool SameReferences(IEnumerable<string>? supplied, IEnumerable<string>? trusted)
        => supplied is not null && trusted is not null &&
            supplied.Order(StringComparer.Ordinal).SequenceEqual(trusted.Order(StringComparer.Ordinal), StringComparer.Ordinal);

    private static bool TryReadDeclaredRevisions(ChatBotGatewayContext context, JsonElement payload, out long[] revisions)
    {
        revisions = [];
        Type? contract = typeof(IChatBotCommand).Assembly.GetType($"Hexalith.ChatBot.Contracts.Commands.{context.Submission.Request.CommandType}");
        if (contract is null || !typeof(IChatBotCommand).IsAssignableFrom(contract)) { return false; }
        HashSet<string> declared = contract.GetProperties().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> supplied = payload.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (contract.GetProperties().Any(property => property.PropertyType == typeof(long) &&
            property.Name.StartsWith("Expected", StringComparison.OrdinalIgnoreCase) &&
            (property.Name.EndsWith("Version", StringComparison.OrdinalIgnoreCase) || property.Name.EndsWith("Revision", StringComparison.OrdinalIgnoreCase)) &&
            !supplied.Contains(property.Name)))
        {
            return false;
        }
        List<long> values = [];
        foreach (JsonProperty property in payload.EnumerateObject().Where(property => property.Name.StartsWith("expected", StringComparison.OrdinalIgnoreCase)))
        {
            if (!declared.Contains(property.Name)) { return false; }
            if (property.Name.EndsWith("Version", StringComparison.OrdinalIgnoreCase) || property.Name.EndsWith("Revision", StringComparison.OrdinalIgnoreCase))
            {
                if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt64(out long revision) || revision < 0) { return false; }
                values.Add(revision);
            }
        }
        revisions = values.ToArray();
        return true;
    }

    private static JsonElement Payload(ChatBotGatewayContext context)
        => context.Submission.Request.Command is JsonElement element ? element : JsonSerializer.SerializeToElement(context.Submission.Request.Command, ReadOptions);

    private static string? ReadString(JsonElement payload, string name)
        => payload.EnumerateObject().FirstOrDefault(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    /// <inheritdoc />
    public ValueTask<string?> ValidateAtomicParticipationAsync(ChatBotGatewayContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // The current SDK admission hook evaluates commands but cannot commit the domain event, lifetime
        // identity, policy/approval references and canonical envelope in one accepted owner transaction.
        // This restriction applies to every profile, including read-only request/result bookkeeping.
        return ValueTask.FromResult<string?>(ChatBotMessageCodes.CanonicalExecutionUnavailable);
    }
}
