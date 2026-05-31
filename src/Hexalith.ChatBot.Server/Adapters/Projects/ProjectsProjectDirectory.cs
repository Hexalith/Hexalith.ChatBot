using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Association.Scoring;

using Generated = Hexalith.Projects.Client.Generated;

namespace Hexalith.ChatBot.Server.Adapters.Projects;

/// <summary>
/// Live <see cref="IProjectDirectory"/> backed by the Hexalith.Projects typed server query API.
/// <para>
/// It never trusts mailbox-supplied project ids: every claimed project id is verified against the
/// tenant-scoped Projects service (tenant authority is server-derived from the authenticated principal),
/// and unknown, archived, stale, ambiguous, cross-tenant, or unauthorized projects are surfaced as
/// metadata-only exclusions rather than candidates. Authorized candidate display names are only carried
/// after live authorization succeeds. Transport or projection unavailability fails closed so the gateway
/// writes no association state. Generated Projects DTOs never escape this adapter boundary.
/// </para>
/// </summary>
internal sealed class ProjectsProjectDirectory(Generated.IClient projects) : IProjectDirectory
{
    public async ValueTask<ProjectDirectoryAssociationResult> FindAuthorizedCandidatesAsync(
        ProjectDirectoryAssociationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        IGrouping<string, AssociationDeterministicSignal>[] claimed = [.. request.Signals
            .Where(static signal => !string.IsNullOrWhiteSpace(signal.ProjectId))
            .GroupBy(static signal => signal.ProjectId, StringComparer.Ordinal)];

        if (claimed.Length == 0)
        {
            // No deterministic project claim to authorize: the directory is available, but with zero
            // candidates so the scorer fails closed to a NeedsReview outcome instead of filing the mail.
            return new ProjectDirectoryAssociationResult(true, [], []);
        }

        // Conversation/thread signals are authorized through the conversation-resolution query, which returns
        // server-authorized candidates and per-signal exclusion evidence scoped to the bound tenant.
        ConversationResolution? conversation = null;
        if (request.Signals.Any(static signal => signal.SignalClass == AssociationSignalClass.ConversationThreadIdentifier)
            && !string.IsNullOrWhiteSpace(request.SourceConversationId))
        {
            try
            {
                Generated.ProjectResolution resolution = await projects
                    .ResolveProjectFromConversationAsync(
                        request.SourceConversationId,
                        includeArchived: false,
                        request.CorrelationId,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
                conversation = ConversationResolution.From(resolution);
            }
            catch (Exception ex) when (IsProjectsFailure(ex))
            {
                return ProjectDirectoryAssociationResult.Unavailable(BuildUnavailableExclusions(claimed));
            }
        }

        var candidates = new List<ProjectAssociationCandidateEvidence>();
        var exclusions = new List<AssociationExclusion>();

        foreach (IGrouping<string, AssociationDeterministicSignal> group in claimed)
        {
            string projectId = group.Key;
            AssociationDeterministicSignal[] signals = [.. group];
            AssociationDeterministicSignal evidenceSignal = signals[0];

            // Defence-in-depth: never surface a project whose opaque id encodes a tenant other than the
            // gateway-bound tenant, even when an upstream deterministic signal claimed it.
            if (EncodesForeignTenant(projectId, request.TenantId))
            {
                exclusions.Add(Exclusion(projectId, AssociationExclusionState.TenantMismatch, AssociationReasonCode.UnauthorizedCandidateSuppressed, evidenceSignal));
                continue;
            }

            bool hasDirectClaim = signals.Any(static signal =>
                signal.SignalClass is AssociationSignalClass.ExplicitProjectIdentifier or AssociationSignalClass.MailboxRoutingRule);

            if (hasDirectClaim)
            {
                ProjectAuthorization authorization;
                try
                {
                    authorization = await AuthorizeDirectClaimAsync(projectId, request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsProjectsFailure(ex))
                {
                    return ProjectDirectoryAssociationResult.Unavailable(BuildUnavailableExclusions(claimed));
                }

                if (authorization.IsAuthorized)
                {
                    candidates.Add(new ProjectAssociationCandidateEvidence(projectId, authorization.DisplayName, signals));
                }
                else
                {
                    exclusions.Add(Exclusion(projectId, authorization.State, authorization.ReasonCode, evidenceSignal));
                }

                continue;
            }

            // Conversation/thread-only claim: authorized only if the conversation resolution returned it.
            if (conversation is null)
            {
                exclusions.Add(Exclusion(projectId, AssociationExclusionState.Unavailable, AssociationReasonCode.AuthorizationEvidenceUnavailable, evidenceSignal));
            }
            else if (conversation.TryGetCandidate(projectId, out string? displayName))
            {
                candidates.Add(new ProjectAssociationCandidateEvidence(projectId, displayName, signals));
            }
            else if (conversation.TryGetExclusion(projectId, out AssociationExclusionState state, out AssociationReasonCode reason))
            {
                exclusions.Add(Exclusion(projectId, state, reason, evidenceSignal));
            }
            else
            {
                exclusions.Add(Exclusion(projectId, AssociationExclusionState.NotFound, AssociationReasonCode.NoAuthorizedCandidate, evidenceSignal));
            }
        }

        return new ProjectDirectoryAssociationResult(true, candidates, exclusions);
    }

    private async ValueTask<ProjectAuthorization> AuthorizeDirectClaimAsync(
        string projectId,
        ProjectDirectoryAssociationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Generated.Project project = await projects
                .GetProjectAsync(projectId, request.CorrelationId, null, cancellationToken)
                .ConfigureAwait(false);

            if (project.LifecycleState == Generated.ProjectLifecycleState.Archived)
            {
                return ProjectAuthorization.Excluded(AssociationExclusionState.Archived, AssociationReasonCode.NoAuthorizedCandidate);
            }

            if (project.Freshness is { Stale: true } || project.Freshness?.TrustState == Generated.ProjectionTrustState.Stale)
            {
                return ProjectAuthorization.Excluded(AssociationExclusionState.Stale, AssociationReasonCode.AuthorizationEvidenceUnavailable);
            }

            if (project.Freshness?.TrustState == Generated.ProjectionTrustState.Unavailable)
            {
                return ProjectAuthorization.Excluded(AssociationExclusionState.Unavailable, AssociationReasonCode.AuthorizationEvidenceUnavailable);
            }

            return ProjectAuthorization.Authorized(NormalizeDisplayName(project.Name));
        }
        catch (Generated.HexalithProjectsApiException api) when (api.StatusCode is 404 or 410)
        {
            return ProjectAuthorization.Excluded(AssociationExclusionState.NotFound, AssociationReasonCode.NoAuthorizedCandidate);
        }
        catch (Generated.HexalithProjectsApiException api) when (api.StatusCode is 401 or 403)
        {
            return ProjectAuthorization.Excluded(AssociationExclusionState.Unauthorized, AssociationReasonCode.UnauthorizedCandidateSuppressed);
        }
    }

    private static AssociationExclusion[] BuildUnavailableExclusions(
        IEnumerable<IGrouping<string, AssociationDeterministicSignal>> claimed)
        => [.. claimed.Select(static group => Exclusion(
            group.Key,
            AssociationExclusionState.Unavailable,
            AssociationReasonCode.AuthorizationEvidenceUnavailable,
            group.First()))];

    private static AssociationExclusion Exclusion(
        string projectId,
        AssociationExclusionState state,
        AssociationReasonCode reasonCode,
        AssociationDeterministicSignal evidence)
    {
        if (state is AssociationExclusionState.Unauthorized or AssociationExclusionState.TenantMismatch ||
            reasonCode == AssociationReasonCode.UnauthorizedCandidateSuppressed)
        {
            return new("suppressed", state, reasonCode, "suppressed", "suppressed");
        }

        return new(projectId, state, reasonCode, evidence.EvidenceReference, evidence.EvidenceFingerprint);
    }

    private static bool EncodesForeignTenant(string projectId, string tenantId)
    {
        int separator = projectId.IndexOf(':', StringComparison.Ordinal);
        return separator > 0 && !string.Equals(projectId[..separator], tenantId, StringComparison.Ordinal);
    }

    private static bool IsProjectsFailure(Exception ex)
        => ex switch
        {
            Generated.HexalithProjectsApiException => true,
            HttpRequestException => true,
            TaskCanceledException => true,
            InvalidOperationException => true,
            _ => false,
        };

    private static string? NormalizeDisplayName(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static (AssociationExclusionState State, AssociationReasonCode ReasonCode) MapExclusion(
        Generated.ResolutionExclusionReferenceState state)
        => state switch
        {
            Generated.ResolutionExclusionReferenceState.Unauthorized => (AssociationExclusionState.Unauthorized, AssociationReasonCode.UnauthorizedCandidateSuppressed),
            Generated.ResolutionExclusionReferenceState.TenantMismatch => (AssociationExclusionState.TenantMismatch, AssociationReasonCode.UnauthorizedCandidateSuppressed),
            Generated.ResolutionExclusionReferenceState.Archived => (AssociationExclusionState.Archived, AssociationReasonCode.NoAuthorizedCandidate),
            Generated.ResolutionExclusionReferenceState.Ambiguous => (AssociationExclusionState.Ambiguous, AssociationReasonCode.MultipleAuthorizedCandidates),
            Generated.ResolutionExclusionReferenceState.Conflict => (AssociationExclusionState.Conflict, AssociationReasonCode.ConflictingDeterministicEvidence),
            Generated.ResolutionExclusionReferenceState.Stale => (AssociationExclusionState.Stale, AssociationReasonCode.AuthorizationEvidenceUnavailable),
            Generated.ResolutionExclusionReferenceState.Unavailable => (AssociationExclusionState.Unavailable, AssociationReasonCode.AuthorizationEvidenceUnavailable),
            Generated.ResolutionExclusionReferenceState.Pending => (AssociationExclusionState.Unavailable, AssociationReasonCode.AuthorizationEvidenceUnavailable),
            Generated.ResolutionExclusionReferenceState.InvalidReference => (AssociationExclusionState.InvalidReference, AssociationReasonCode.NoAuthorizedCandidate),
            _ => (AssociationExclusionState.Unavailable, AssociationReasonCode.NoAuthorizedCandidate),
        };

    private readonly record struct ProjectAuthorization(
        bool IsAuthorized,
        string? DisplayName,
        AssociationExclusionState State,
        AssociationReasonCode ReasonCode)
    {
        public static ProjectAuthorization Authorized(string? displayName)
            => new(true, displayName, default, default);

        public static ProjectAuthorization Excluded(AssociationExclusionState state, AssociationReasonCode reasonCode)
            => new(false, null, state, reasonCode);
    }

    private sealed class ConversationResolution
    {
        private readonly Dictionary<string, string?> _candidates;
        private readonly Dictionary<string, (AssociationExclusionState State, AssociationReasonCode ReasonCode)> _exclusions;

        private ConversationResolution(
            Dictionary<string, string?> candidates,
            Dictionary<string, (AssociationExclusionState, AssociationReasonCode)> exclusions)
        {
            _candidates = candidates;
            _exclusions = exclusions;
        }

        public static ConversationResolution From(Generated.ProjectResolution resolution)
        {
            var candidates = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (Generated.ResolutionCandidate candidate in resolution.Candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate.ProjectId))
                {
                    candidates[candidate.ProjectId] = NormalizeDisplayName(candidate.DisplayName);
                }
            }

            var exclusions = new Dictionary<string, (AssociationExclusionState, AssociationReasonCode)>(StringComparer.Ordinal);
            foreach (Generated.ResolutionExclusion exclusion in resolution.Excluded)
            {
                if (string.IsNullOrWhiteSpace(exclusion.ProjectId)
                    || exclusion.ReferenceState is Generated.ResolutionExclusionReferenceState.Included
                    || candidates.ContainsKey(exclusion.ProjectId))
                {
                    continue;
                }

                exclusions[exclusion.ProjectId] = MapExclusion(exclusion.ReferenceState);
            }

            return new ConversationResolution(candidates, exclusions);
        }

        public bool TryGetCandidate(string projectId, out string? displayName)
            => _candidates.TryGetValue(projectId, out displayName);

        public bool TryGetExclusion(string projectId, out AssociationExclusionState state, out AssociationReasonCode reasonCode)
        {
            if (_exclusions.TryGetValue(projectId, out (AssociationExclusionState State, AssociationReasonCode ReasonCode) mapped))
            {
                state = mapped.State;
                reasonCode = mapped.ReasonCode;
                return true;
            }

            state = default;
            reasonCode = default;
            return false;
        }
    }
}
