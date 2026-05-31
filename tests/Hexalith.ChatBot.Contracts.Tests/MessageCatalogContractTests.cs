using System.Text.RegularExpressions;

using Hexalith.ChatBot.Contracts.Messages;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static partial class MessageCatalogContractTests
{
    private static readonly Regex MessageCodePattern = MessageCodeRegex();

    [Fact]
    public static void CatalogShouldExposeStableVersionAndRequiredEntries()
    {
        ChatBotMessageCatalogVersion.Current.ShouldBe("chatbot.message-catalog.v1");

        string[] codes = ChatBotMessageCatalog.Entries.Select(static entry => entry.Code).ToArray();
        codes.ShouldBe(codes.Distinct(StringComparer.Ordinal).ToArray(), ignoreOrder: false);
        codes.ShouldContain(ChatBotMessageCodes.AuthenticationDenied);
        codes.ShouldContain(ChatBotMessageCodes.AuthorizationDenied);
        codes.ShouldContain(ChatBotMessageCodes.AuditUnavailable);
        codes.ShouldContain(ChatBotMessageCodes.IdempotencyConflictCommandExecution);
        codes.ShouldContain(ChatBotMessageCodes.IdempotencyConflictMessageIntake);
        codes.ShouldContain(ChatBotMessageCodes.InvalidLifecycleTransition);
        codes.ShouldContain(ChatBotMessageCodes.RefusalBlockedAction);
        codes.ShouldContain(ChatBotMessageCodes.DependencyDegraded);
        codes.ShouldContain(ChatBotMessageCodes.FailedAttachment);
        codes.ShouldContain(ChatBotMessageCodes.FailedCommand);
        codes.ShouldContain(ChatBotMessageCodes.DegradedMailbox);
        codes.ShouldContain(ChatBotMessageCodes.UnresolvedParticipant);
        codes.ShouldContain(ChatBotMessageCodes.UnauthorizedParticipant);
        codes.ShouldContain(ChatBotMessageCodes.ParticipantDirectoryDegraded);
        codes.ShouldContain(ChatBotMessageCodes.AssociationAmbiguousRouted);
        codes.ShouldContain(ChatBotMessageCodes.AssociationScorerFailedClosed);
        codes.ShouldContain(ChatBotMessageCodes.AssociationScorerUnavailable);
        codes.ShouldContain(ChatBotMessageCodes.AssociationConflictingDeterministicEvidence);
        codes.ShouldContain(ChatBotMessageCodes.AssociationContextUnavailable);
        codes.ShouldContain(ChatBotMessageCodes.AssociationDecisionAccepted);
        codes.ShouldContain(ChatBotMessageCodes.AssociationAlreadyDecided);
    }

    [Fact]
    public static void CatalogEntriesShouldBeSafeAndSerializationTolerant()
    {
        HashSet<string> nextActions = SafeNextActions();
        HashSet<string> disabledReasons = DisabledReasons();

        foreach (ChatBotMessageCatalogEntry entry in ChatBotMessageCatalog.Entries)
        {
            MessageCodePattern.IsMatch(entry.Code).ShouldBeTrue(entry.Code);
            entry.Headline.Length.ShouldBeLessThanOrEqualTo(80, entry.Code);
            IsOneSentence(entry.Reason).ShouldBeTrue(entry.Code);
            nextActions.ShouldContain(entry.NextAction);
            entry.DetailVisibility.ShouldBe(ChatBotDetailVisibility.MetadataOnly);

            if (entry.DisabledActionReason is not null)
            {
                disabledReasons.ShouldContain(entry.DisabledActionReason);
            }

            AssertNoRestrictedText(entry.Headline, entry.Code);
            AssertNoRestrictedText(entry.Reason, entry.Code);
        }
    }

    [Fact]
    public static void DisabledActionReasonsShouldBeFiniteSet()
    {
        DisabledReasons().ShouldBe(
            [
                "insufficient-authority",
                "state-not-permitted",
                "dependency-degraded",
                "awaiting-other-actor",
                "policy-blocked",
                "unresolved-participant",
                "participant-directory-degraded",
                "candidate-required",
                "evidence-expired",
                "not-authorized",
                "projection-pending",
                "terminal-state",
                "already-decided",
                "already-corrected",
            ],
            ignoreOrder: false);
    }

    private static HashSet<string> SafeNextActions()
        =>
        [
            ChatBotMessageNextActions.Authenticate,
            ChatBotMessageNextActions.RetryLater,
            ChatBotMessageNextActions.RequestAccess,
            ChatBotMessageNextActions.Escalate,
            ChatBotMessageNextActions.Dismiss,
            ChatBotMessageNextActions.CorrectRequest,
            ChatBotMessageNextActions.None,
        ];

    private static HashSet<string> DisabledReasons()
        =>
        [
            ChatBotDisabledActionReasons.InsufficientAuthority,
            ChatBotDisabledActionReasons.StateNotPermitted,
            ChatBotDisabledActionReasons.DependencyDegraded,
            ChatBotDisabledActionReasons.AwaitingOtherActor,
            ChatBotDisabledActionReasons.PolicyBlocked,
            ChatBotDisabledActionReasons.UnresolvedParticipant,
            ChatBotDisabledActionReasons.ParticipantDirectoryDegraded,
            ChatBotDisabledActionReasons.CandidateRequired,
            ChatBotDisabledActionReasons.EvidenceExpired,
            ChatBotDisabledActionReasons.NotAuthorized,
            ChatBotDisabledActionReasons.ProjectionPending,
            ChatBotDisabledActionReasons.TerminalState,
            ChatBotDisabledActionReasons.AlreadyDecided,
            ChatBotDisabledActionReasons.AlreadyCorrected,
        ];

    private static bool IsOneSentence(string text)
        => text.EndsWith(".", StringComparison.Ordinal) &&
            text.Count(static character => character is '.' or '!' or '?') == 1;

    private static void AssertNoRestrictedText(string value, string code)
    {
        string[] restricted =
        [
            "tenant-alpha",
            "project-alpha",
            "file-secret",
            "party-alpha",
            "audit detail",
            "payload",
            "exception",
            "secret",
            "/home/",
            "C:\\",
        ];

        foreach (string marker in restricted)
        {
            value.ShouldNotContain(marker, Case.Insensitive, code);
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex MessageCodeRegex();
}
