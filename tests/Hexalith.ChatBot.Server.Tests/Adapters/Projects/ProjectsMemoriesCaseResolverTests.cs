using Hexalith.ChatBot.Server.Adapters.Projects;

using Shouldly;

using Generated = Hexalith.Projects.Client.Generated;

namespace Hexalith.ChatBot.Server.Tests.Adapters.Projects;

public sealed class ProjectsMemoriesCaseResolverTests
{
    [Fact]
    public async Task ResolveCaseIdAsync_UsesTheSoleIncludedMemoryReference()
    {
        ProjectsMemoriesCaseResolver resolver = new(new FakeProjectsClient(Context("project-1", "case-1")));

        string caseId = await resolver.ResolveCaseIdAsync(
            "tenant-a",
            "project-1",
            "correlation-1",
            TestContext.Current.CancellationToken);

        caseId.ShouldBe("case-1");
        caseId.ShouldNotBe("project-1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task ResolveCaseIdAsync_RejectsZeroOrMultipleMemoryReferences(int count)
    {
        Generated.ProjectContext context = Context("project-1", "case-1");
        context.MemoryReferences = [.. Enumerable.Range(1, count).Select(index => IncludedMemory($"case-{index}"))];
        ProjectsMemoriesCaseResolver resolver = new(new FakeProjectsClient(context));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => resolver
            .ResolveCaseIdAsync("tenant-a", "project-1", "correlation-1", TestContext.Current.CancellationToken)
            .AsTask());

        exception.Message.ShouldBe(ProjectsMemoriesCaseResolver.MemoryReferenceAmbiguousReasonCode);
    }

    [Fact]
    public async Task ResolveCaseIdAsync_RejectsStaleContext()
    {
        Generated.ProjectContext context = Context("project-1", "case-1");
        context.Freshness = Generated.ProjectContextFreshness.Stale;
        ProjectsMemoriesCaseResolver resolver = new(new FakeProjectsClient(context));

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => resolver
            .ResolveCaseIdAsync("tenant-a", "project-1", "correlation-1", TestContext.Current.CancellationToken)
            .AsTask());

        exception.Message.ShouldBe(ProjectsMemoriesCaseResolver.ContextStaleReasonCode);
    }

    [Fact]
    public async Task ResolveCaseIdAsync_IgnoresNonIncludedMemoryReferences()
    {
        Generated.ProjectContext context = Context("project-1", "case-1");
        context.MemoryReferences.Add(new Generated.ProjectContextReference
        {
            ReferenceKind = Generated.ProjectContextReferenceReferenceKind.Memory,
            ReferenceId = "case-excluded",
            ReferenceState = Generated.ProjectContextReferenceReferenceState.Excluded,
        });
        ProjectsMemoriesCaseResolver resolver = new(new FakeProjectsClient(context));

        string caseId = await resolver.ResolveCaseIdAsync(
            "tenant-a",
            "project-1",
            "correlation-1",
            TestContext.Current.CancellationToken);

        caseId.ShouldBe("case-1");
    }

    private static Generated.ProjectContext Context(string projectId, string caseId)
        => new()
        {
            ProjectId = projectId,
            Lifecycle = Generated.ProjectContextLifecycle.Active,
            AssemblyOutcome = Generated.ProjectContextAssemblyOutcome.Assembled,
            Freshness = Generated.ProjectContextFreshness.Fresh,
            MemoryReferences = [IncludedMemory(caseId)],
        };

    private static Generated.ProjectContextReference IncludedMemory(string caseId)
        => new()
        {
            ReferenceKind = Generated.ProjectContextReferenceReferenceKind.Memory,
            ReferenceId = caseId,
            ReferenceState = Generated.ProjectContextReferenceReferenceState.Included,
        };

    private sealed class FakeProjectsClient(Generated.ProjectContext context)
        : Generated.Client(new HttpClient { BaseAddress = new Uri("https://projects.invalid") })
    {
        public override Task<Generated.ProjectContext> GetProjectContextAsync(
            string projectId,
            string x_Correlation_Id,
            Generated.ReadConsistencyClass? x_Hexalith_Freshness,
            CancellationToken cancellationToken)
            => Task.FromResult(context);
    }
}
