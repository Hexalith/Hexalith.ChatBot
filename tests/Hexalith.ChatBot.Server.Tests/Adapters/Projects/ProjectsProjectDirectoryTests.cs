using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Projects;
using Hexalith.ChatBot.Server.Association.Scoring;

using Shouldly;

using Generated = Hexalith.Projects.Client.Generated;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Projects;

public sealed class ProjectsProjectDirectoryTests
{
    private const string Tenant = "tenant-a";

    [Fact]
    public async Task AuthorizesActiveExplicitProjectAsCandidateWithSignals()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => ActiveProject("project-1", "Acme Roadmap")));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-1")]), TestContext.Current.CancellationToken);

        result.IsAvailable.ShouldBeTrue();
        ProjectAssociationCandidateEvidence candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.ProjectId.ShouldBe("project-1");
        candidate.DisplayName.ShouldBe("Acme Roadmap");
        candidate.Signals.ShouldHaveSingleItem().SignalClass.ShouldBe(AssociationSignalClass.ExplicitProjectIdentifier);
        result.Exclusions.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExcludesUnknownExplicitProjectWithoutCandidate()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => throw ApiException(404)));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-gone")]), TestContext.Current.CancellationToken);

        result.IsAvailable.ShouldBeTrue();
        result.Candidates.ShouldBeEmpty();
        AssociationExclusion exclusion = result.Exclusions.ShouldHaveSingleItem();
        exclusion.ProjectId.ShouldBe("project-gone");
        exclusion.State.ShouldBe(AssociationExclusionState.NotFound);
        exclusion.ReasonCode.ShouldBe(AssociationReasonCode.NoAuthorizedCandidate);
    }

    [Fact]
    public async Task ExcludesArchivedProject()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => new Generated.Project
            {
                ProjectId = "project-archived",
                Name = "Old",
                LifecycleState = Generated.ProjectLifecycleState.Archived,
                Freshness = Trusted(),
            }));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.MailboxRoutingRule, "project-archived")]), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
        result.Exclusions.ShouldHaveSingleItem().State.ShouldBe(AssociationExclusionState.Archived);
    }

    [Fact]
    public async Task ExcludesStaleProjection()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => new Generated.Project
            {
                ProjectId = "project-stale",
                Name = "Pending",
                LifecycleState = Generated.ProjectLifecycleState.Active,
                Freshness = new Generated.FreshnessMetadata { Stale = true, TrustState = Generated.ProjectionTrustState.Stale },
            }));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-stale")]), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
        AssociationExclusion exclusion = result.Exclusions.ShouldHaveSingleItem();
        exclusion.State.ShouldBe(AssociationExclusionState.Stale);
        exclusion.ReasonCode.ShouldBe(AssociationReasonCode.AuthorizationEvidenceUnavailable);
    }

    [Fact]
    public async Task SuppressesUnauthorizedProjectWithoutLeakingName()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => throw ApiException(403)));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-secret")]), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
        AssociationExclusion exclusion = result.Exclusions.ShouldHaveSingleItem();
        exclusion.ProjectId.ShouldBe("suppressed");
        exclusion.State.ShouldBe(AssociationExclusionState.Unauthorized);
        exclusion.ReasonCode.ShouldBe(AssociationReasonCode.UnauthorizedCandidateSuppressed);
        exclusion.EvidenceReference.ShouldBe("suppressed");
        exclusion.EvidenceFingerprint.ShouldBe("suppressed");

        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        serialized.ShouldNotContain("project-secret", Case.Sensitive);
    }

    [Fact]
    public async Task ExcludesCrossTenantProjectWithoutQueryingProjects()
    {
        var client = new FakeProjectsClient(getProject: _ => throw new ShouldAssertException("Projects must not be queried for a foreign-tenant id"));
        var directory = new ProjectsProjectDirectory(client);

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "tenant-b:project-9")]), TestContext.Current.CancellationToken);

        result.IsAvailable.ShouldBeTrue();
        result.Candidates.ShouldBeEmpty();
        AssociationExclusion exclusion = result.Exclusions.ShouldHaveSingleItem();
        exclusion.ProjectId.ShouldBe("suppressed");
        exclusion.State.ShouldBe(AssociationExclusionState.TenantMismatch);
        exclusion.EvidenceReference.ShouldBe("suppressed");
    }

    [Fact]
    public async Task ResolvesConversationThreadSignalToAuthorizedCandidate()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            resolve: _ => new Generated.ProjectResolution
            {
                Result = Generated.ResolutionResult.SingleCandidate,
                Candidates = [Candidate("project-thread", "Thread Project")],
                Excluded = [],
            }));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request(
                [Signal(AssociationSignalClass.ConversationThreadIdentifier, "project-thread")],
                conversationId: "conversation-7"), TestContext.Current.CancellationToken);

        ProjectAssociationCandidateEvidence candidate = result.Candidates.ShouldHaveSingleItem();
        candidate.ProjectId.ShouldBe("project-thread");
        candidate.DisplayName.ShouldBe("Thread Project");
        candidate.Signals.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task SurfacesAmbiguousConversationExclusion()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            resolve: _ => new Generated.ProjectResolution
            {
                Result = Generated.ResolutionResult.NoMatch,
                Candidates = [],
                Excluded = [Exclusion("project-thread", Generated.ResolutionExclusionReferenceState.Ambiguous)],
            }));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request(
                [Signal(AssociationSignalClass.ConversationThreadIdentifier, "project-thread")],
                conversationId: "conversation-7"), TestContext.Current.CancellationToken);

        result.Candidates.ShouldBeEmpty();
        AssociationExclusion exclusion = result.Exclusions.ShouldHaveSingleItem();
        exclusion.State.ShouldBe(AssociationExclusionState.Ambiguous);
        exclusion.ReasonCode.ShouldBe(AssociationReasonCode.MultipleAuthorizedCandidates);
    }

    [Fact]
    public async Task FailsClosedWhenProjectsTransportUnavailable()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient(
            getProject: _ => throw ApiException(503)));

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            Request([Signal(AssociationSignalClass.ExplicitProjectIdentifier, "project-1")]), TestContext.Current.CancellationToken);

        result.IsAvailable.ShouldBeFalse();
        result.Candidates.ShouldBeEmpty();
        result.Exclusions.ShouldHaveSingleItem().ReasonCode.ShouldBe(AssociationReasonCode.AuthorizationEvidenceUnavailable);
    }

    [Fact]
    public async Task ReturnsAvailableWithoutCandidatesWhenNoProjectIsClaimed()
    {
        var directory = new ProjectsProjectDirectory(new FakeProjectsClient());

        ProjectDirectoryAssociationResult result = await directory.FindAuthorizedCandidatesAsync(
            new ProjectDirectoryAssociationRequest(Tenant, string.Empty, null, [], "corr-1"), TestContext.Current.CancellationToken);

        result.IsAvailable.ShouldBeTrue();
        result.Candidates.ShouldBeEmpty();
        result.Exclusions.ShouldBeEmpty();
    }

    private static ProjectDirectoryAssociationRequest Request(
        IReadOnlyList<AssociationDeterministicSignal> signals,
        string conversationId = "conversation-default")
        => new(Tenant, conversationId, null, signals, "corr-1");

    private static AssociationDeterministicSignal Signal(AssociationSignalClass signalClass, string projectId)
        => new(signalClass, projectId, $"mailbox:{projectId}", $"fingerprint:{projectId}", 0.9, RequiredForAutoAssociation: true);

    private static Generated.Project ActiveProject(string projectId, string name)
        => new()
        {
            ProjectId = projectId,
            Name = name,
            LifecycleState = Generated.ProjectLifecycleState.Active,
            Freshness = Trusted(),
        };

    private static Generated.FreshnessMetadata Trusted()
        => new() { Stale = false, TrustState = Generated.ProjectionTrustState.Trusted };

    private static Generated.ResolutionCandidate Candidate(string projectId, string displayName)
        => new()
        {
            ProjectId = projectId,
            DisplayName = displayName,
            ReasonCodes = [Generated.ReasonCodes.ConversationLinked],
            Rank = 1,
            Score = 100,
        };

    private static Generated.ResolutionExclusion Exclusion(string projectId, Generated.ResolutionExclusionReferenceState state)
        => new()
        {
            ProjectId = projectId,
            ReferenceState = state,
        };

    private static Generated.HexalithProjectsApiException ApiException(int statusCode)
        => new("projects-error", statusCode, response: string.Empty, headers: null!, innerException: null!);

    private sealed class FakeProjectsClient(
        Func<string, Generated.Project>? getProject = null,
        Func<string, Generated.ProjectResolution>? resolve = null)
        : Generated.Client(new HttpClient { BaseAddress = new Uri("https://projects.invalid") })
    {
        public override Task<Generated.Project> GetProjectAsync(
            string projectId,
            string x_Correlation_Id,
            Generated.ReadConsistencyClass? x_Hexalith_Freshness,
            CancellationToken cancellationToken)
            => getProject is null
                ? throw new InvalidOperationException("GetProjectAsync was not expected.")
                : Task.FromResult(getProject(projectId));

        public override Task<Generated.ProjectResolution> ResolveProjectFromConversationAsync(
            string conversationId,
            bool? includeArchived,
            string x_Correlation_Id,
            Generated.ReadConsistencyClass? x_Hexalith_Freshness,
            CancellationToken cancellationToken)
            => resolve is null
                ? throw new InvalidOperationException("ResolveProjectFromConversationAsync was not expected.")
                : Task.FromResult(resolve(conversationId));
    }
}
