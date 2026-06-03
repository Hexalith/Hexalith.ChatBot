using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>
/// Story 9.12 (Task 2, AC2, NFR2/NFR42 no-leak floor) coverage for <see cref="ProjectionResourceDigest.Create"/>
/// sanitization. The factory reduces both the resource id and the structural state token to <see cref="AuditMetadata"/>-safe
/// bounded tokens (mirroring <c>DerivedStoreEntry.Create</c>), so a malformed or content-bearing token can never smuggle
/// raw content into a snapshot. The coordinator/evaluator tests only ever feed it pre-sanitized tokens; this fixture pins
/// the sanitization boundary directly.
/// </summary>
public sealed class ProjectionResourceDigestTests
{
    private const string SafeFallback = "redacted-ref";

    [Fact]
    public void CreateKeepsAlreadySafeTokensVerbatim()
    {
        ProjectionResourceDigest digest = ProjectionResourceDigest.Create("resource:project-a", "schema.v1|prov.graph|red.metadata");

        digest.ResourceId.ShouldBe("resource:project-a");
        digest.StructuralStateToken.ShouldBe("schema.v1|prov.graph|red.metadata");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has spaces")] // a space is not in the safe charset
    [InlineData("body=Dear customer, your password is hunter2")] // content + a banned marker
    [InlineData("contains-secret-material")] // a banned sensitive marker
    [InlineData("attachment.json")] // a banned file-extension marker
    public void CreateReducesUnsafeOrContentBearingTokensToTheSafeFallback(string? unsafeToken)
    {
        ProjectionResourceDigest digest = ProjectionResourceDigest.Create("resource-a", unsafeToken);

        digest.ResourceId.ShouldBe("resource-a"); // the safe id is untouched
        digest.StructuralStateToken.ShouldBe(SafeFallback); // the unsafe token can never reach the snapshot
    }

    [Fact]
    public void CreateAlsoSanitizesAnUnsafeResourceId()
    {
        ProjectionResourceDigest digest = ProjectionResourceDigest.Create("resource id with spaces", "token-a");

        digest.ResourceId.ShouldBe(SafeFallback);
        digest.StructuralStateToken.ShouldBe("token-a");
    }
}
