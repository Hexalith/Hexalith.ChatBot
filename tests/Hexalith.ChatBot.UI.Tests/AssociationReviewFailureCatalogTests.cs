using Hexalith.ChatBot.Contracts.Messages;
using Hexalith.ChatBot.UI.State.AssociationReview;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Verifies legacy failure aliases without relaxing the canonical catalog boundary.</summary>
public sealed class AssociationReviewFailureCatalogTests
{
    /// <summary>Legacy aliases retain their established case-insensitive recognition.</summary>
    /// <param name="alias">The mixed-case legacy token.</param>
    /// <param name="code">Its canonical catalog code.</param>
    [Theory]
    [InlineData("TIMEOUT", ChatBotMessageCodes.DependencyDegraded)]
    [InlineData("Not-Authorized", ChatBotMessageCodes.AuthorizationDenied)]
    [InlineData("Projection-Pending", ChatBotMessageCodes.AssociationCorrectionProjectionUnavailable)]
    [InlineData("AUTHORIZATION_DENIED", ChatBotMessageCodes.AuthorizationDenied)]
    public void LegacyAliasShouldPreserveCaseInsensitiveMapping(string alias, string code)
    {
        AssociationReviewFailureCatalog.IsKnown(alias).ShouldBeTrue();
        AssociationReviewFailureCatalog.CatalogCode(alias).ShouldBe(code);
        AssociationReviewFailureCatalog.IsRetryable(alias).ShouldBe(ChatBotMessageCatalog.Resolve(code).NextAction == ChatBotMessageNextActions.RetryLater);
    }

    /// <summary>Canonical codes outside the alias inventory still use exact catalog validation.</summary>
    [Fact]
    public void CanonicalCodeCaseShouldRemainValidated()
    {
        AssociationReviewFailureCatalog.CatalogCode("AUDIT_UNAVAILABLE").ShouldBe(ChatBotMessageCodes.GenericDenial);
    }
}
