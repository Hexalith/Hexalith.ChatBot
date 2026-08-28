using Hexalith.ChatBot.Server.Audit;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Audit;

/// <summary>Known-answer and canonical-order coverage for retained projection snapshot fingerprints.</summary>
public sealed class ProjectionSnapshotFingerprintTests
{
    [Fact]
    public void LengthFramedOrdinalFingerprintMatchesThePinnedKnownAnswer()
    {
        IReadOnlyList<ProjectionResourceDigest> snapshot =
        [
            new("resource-b", new string('b', 64)),
            new("resource-a", new string('a', 64)),
        ];

        ProjectionSnapshotFingerprint.Compute(snapshot)
            .ShouldBe("ed3186f427c2f0b992edb14993e4c44ab74ad9f5e1846c1179f51cba18baddd6");
        ProjectionSnapshotFingerprint.Compute([.. snapshot.Reverse()])
            .ShouldBe("ed3186f427c2f0b992edb14993e4c44ab74ad9f5e1846c1179f51cba18baddd6");
    }
}
