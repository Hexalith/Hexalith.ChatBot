using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Parties;
using Hexalith.Parties.Client.Abstractions;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.ValueObjects;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Adapters;

public sealed class ParticipantDirectoryTests
{
    [Fact]
    public static void CanonicalizationShouldTrimNormalizeAndLowercaseEmailEvidence()
    {
        PartiesParticipantDirectory.TryCanonicalizeEmailEvidence("  USER@Example.COM  ", out string? canonical).ShouldBeTrue();
        canonical.ShouldBe("user@example.com");
    }

    [Fact]
    public async Task ResolveShouldReturnTenantScopedPartyIdWithoutRawEvidence()
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchResult = Page([Search("tenant-alpha:parties:party-001")]),
            Detail = Detail("tenant-alpha:parties:party-001"),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("  Sender@Example.TEST "),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldNotBeNull();
        result.Resolved.PartyId.ShouldBe("tenant-alpha:parties:party-001");
        result.Resolved.PartyTenantId.ShouldBe("tenant-alpha");
        result.Unresolved.ShouldBeNull();
        parties.LastQuery.ShouldBe("sender@example.test");
    }

    [Theory]
    [InlineData(0, ParticipantResolutionBlockedReason.NotFound)]
    [InlineData(2, ParticipantResolutionBlockedReason.AmbiguousMatch)]
    public async Task ResolveShouldReturnExplicitUnresolvedOutcomes(int resultCount, ParticipantResolutionBlockedReason expectedReason)
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchResult = Page(Enumerable.Range(0, resultCount).Select(index => Search($"tenant-alpha:parties:party-{index}")).ToArray()),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("sender@example.test"),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldBeNull();
        result.Unresolved.ShouldNotBeNull();
        result.Unresolved.Reason.ShouldBe(expectedReason);
        result.Unresolved.AllowedReviewActions.ShouldBe(RequiredReviewActions());
    }

    [Fact]
    public async Task ResolveShouldTreatTotalCountGreaterThanOneAsAmbiguous()
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchResult = Page([Search("tenant-alpha:parties:party-001")], totalCount: 2),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("sender@example.test"),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldBeNull();
        result.Unresolved.ShouldNotBeNull();
        result.Unresolved.Reason.ShouldBe(ParticipantResolutionBlockedReason.AmbiguousMatch);
        parties.GetCount.ShouldBe(0);
    }

    [Fact]
    public async Task ResolveShouldRejectInvalidEmailEvidenceWithoutQueryingParties()
    {
        RecordingPartiesQueryClient parties = new();
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("not-an-email"),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldBeNull();
        result.Unresolved.ShouldNotBeNull();
        result.Unresolved.Reason.ShouldBe(ParticipantResolutionBlockedReason.InvalidEvidence);
        result.Unresolved.AllowedReviewActions.ShouldBe(RequiredReviewActions());
        parties.SearchCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(ProjectionFreshnessStatus.Stale)]
    [InlineData(ProjectionFreshnessStatus.Rebuilding)]
    [InlineData(ProjectionFreshnessStatus.Degraded)]
    [InlineData(ProjectionFreshnessStatus.Unavailable)]
    public async Task ResolveShouldFailClosedWhenPartiesProjectionIsNotCurrent(ProjectionFreshnessStatus status)
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchResult = Page([Search("tenant-alpha:parties:party-001")], status),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("sender@example.test"),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldBeNull();
        result.Unresolved.ShouldNotBeNull();
        result.Unresolved.Reason.ShouldBe(ParticipantResolutionBlockedReason.DirectoryDegraded);
        parties.GetCount.ShouldBe(0);
    }

    [Fact]
    public async Task ResolveShouldReturnDirectoryUnavailableWithoutRawExceptionDetail()
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchException = new HttpRequestException("raw endpoint tenant-alpha sender@example.test"),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution result = await directory.ResolveEmailEvidenceAsync(
            Lookup("sender@example.test"),
            TestContext.Current.CancellationToken);

        result.Resolved.ShouldBeNull();
        result.Unresolved.ShouldNotBeNull();
        result.Unresolved.Reason.ShouldBe(ParticipantResolutionBlockedReason.DirectoryUnavailable);
        result.Unresolved.EvidenceFingerprint.ShouldBe("evidence-sha256");
        result.Unresolved.ToString().ShouldNotContain("raw endpoint", Case.Insensitive);
        result.Unresolved.ToString().ShouldNotContain("sender@example.test", Case.Insensitive);
    }

    [Fact]
    public async Task ResolveShouldFailClosedForRestrictedErasedAndCrossTenantParties()
    {
        (await ReasonFor(Search("tenant-beta:parties:party-001"))).ShouldBe(ParticipantResolutionBlockedReason.TenantMismatch);
        (await ReasonFor(Search("tenant-alpha:parties:party-001") with { Party = Search("tenant-alpha:parties:party-001").Party with { IsErased = true } }))
            .ShouldBe(ParticipantResolutionBlockedReason.ErasedParty);
        (await ReasonFor(Search("tenant-alpha:parties:party-001"), Detail("tenant-alpha:parties:party-001") with { IsRestricted = true }))
            .ShouldBe(ParticipantResolutionBlockedReason.RestrictedParty);
    }

    private static async Task<ParticipantResolutionBlockedReason> ReasonFor(PartySearchResult result, PartyDetail? detail = null)
    {
        RecordingPartiesQueryClient parties = new()
        {
            SearchResult = Page([result]),
            Detail = detail ?? Detail(result.Party.Id),
        };
        PartiesParticipantDirectory directory = new(parties);

        ParticipantDirectoryResolution resolution = await directory.ResolveEmailEvidenceAsync(
            Lookup("sender@example.test"),
            TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        return resolution.Unresolved.ShouldNotBeNull().Reason;
    }

    private static ParticipantDirectoryLookup Lookup(string address)
        => new("tenant-alpha", "01ARZ3NDEKTSV4RRFFQ69G5FAZ", address, "mailbox:intake:sender", "evidence-sha256");

    private static IReadOnlyList<ParticipantReviewAction> RequiredReviewActions()
        =>
        [
            ParticipantReviewAction.Link,
            ParticipantReviewAction.CreatePending,
            ParticipantReviewAction.Reject,
            ParticipantReviewAction.Quarantine,
        ];

    private static PagedResult<PartySearchResult> Page(
        IReadOnlyList<PartySearchResult> items,
        ProjectionFreshnessStatus status = ProjectionFreshnessStatus.Current,
        int? totalCount = null)
        => new()
        {
            Items = items,
            Page = 1,
            PageSize = 2,
            TotalCount = totalCount ?? items.Count,
            TotalPages = 1,
            Freshness = ProjectionFreshnessMetadata.Create(status),
        };

    private static PartySearchResult Search(string partyId)
        => new()
        {
            Party = new PartyIndexEntry
            {
                Id = partyId,
                Type = PartyType.Person,
                DisplayName = "Redacted",
                IsActive = true,
            },
            Matches = [],
        };

    private static PartyDetail Detail(string partyId)
        => new()
        {
            Id = partyId,
            Type = PartyType.Person,
            DisplayName = "Redacted",
            SortName = "Redacted",
            IsActive = true,
        };

    private sealed class RecordingPartiesQueryClient : IPartiesQueryClient
    {
        public string? LastQuery { get; private set; }

        public int SearchCount { get; private set; }

        public int GetCount { get; private set; }

        public PagedResult<PartySearchResult> SearchResult { get; init; } = Page([]);

        public PartyDetail Detail { get; init; } = Detail("tenant-alpha:parties:party-001");

        public Exception? SearchException { get; init; }

        public Task<PartyDetail> GetPartyAsync(
            string partyId,
            CancellationToken ct,
            Func<HttpRequestMessage, CancellationToken, ValueTask>? requestCustomizer = null)
        {
            GetCount++;
            return Task.FromResult(Detail);
        }

        public Task<PagedResult<PartyIndexEntry>> ListPartiesAsync(
            int page,
            int pageSize,
            PartyType? type,
            bool? active,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            DateTimeOffset? modifiedAfter,
            DateTimeOffset? modifiedBefore,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<PagedResult<PartySearchResult>> SearchPartiesAsync(
            string query,
            int page,
            int pageSize,
            CancellationToken ct,
            string? mode = null,
            string? caseId = null,
            Func<HttpRequestMessage, CancellationToken, ValueTask>? requestCustomizer = null,
            PartyType? type = null,
            bool? active = null)
        {
            SearchCount++;
            LastQuery = query;
            if (SearchException is not null)
            {
                return Task.FromException<PagedResult<PartySearchResult>>(SearchException);
            }

            return Task.FromResult(SearchResult);
        }
    }
}
