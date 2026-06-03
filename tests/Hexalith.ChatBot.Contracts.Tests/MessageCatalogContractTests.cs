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
        codes.ShouldContain(ChatBotMessageCodes.DuplicateSuppressed);
        codes.ShouldContain(ChatBotMessageCodes.RetryQueued);
        codes.ShouldContain(ChatBotMessageCodes.RetryAccepted);
        codes.ShouldContain(ChatBotMessageCodes.RetryExhausted);
        codes.ShouldContain(ChatBotMessageCodes.TerminalFailure);
        codes.ShouldContain(ChatBotMessageCodes.RecoverableMailboxDegradation);
        codes.ShouldContain(ChatBotMessageCodes.ProjectionRetryable);
        codes.ShouldContain(ChatBotMessageCodes.ReprocessCreated);
        codes.ShouldContain(ChatBotMessageCodes.ProjectAiContextPackageUnavailable);
        codes.ShouldContain(ChatBotMessageCodes.MailboxSourceDisabled);
        codes.ShouldContain(ChatBotMessageCodes.MailboxSourceQuarantined);
        codes.ShouldContain(ChatBotMessageCodes.MailboxSourceRateLimited);
        codes.ShouldContain(ChatBotMessageCodes.ServiceClientDisabled);
        codes.ShouldContain(ChatBotMessageCodes.ServiceClientQuarantined);
        codes.ShouldContain(ChatBotMessageCodes.ServiceClientRateLimited);
        codes.ShouldContain(ChatBotMessageCodes.AiActorDisabled);
        codes.ShouldContain(ChatBotMessageCodes.AiActorQuarantined);
        codes.ShouldContain(ChatBotMessageCodes.AiActorRateLimited);
        codes.ShouldContain(ChatBotMessageCodes.CommandCapabilityDisabled);
        codes.ShouldContain(ChatBotMessageCodes.CommandCapabilityQuarantined);
        codes.ShouldContain(ChatBotMessageCodes.CommandCapabilityRateLimited);
        codes.ShouldContain(ChatBotMessageCodes.OutboundChannelDisabled);

        // Story 7.16: the service-client quarantine entry conveys contained-for-review with the terminal
        // request-access + disabled-action tokens (await-admin), not the transient retry-later set.
        ChatBotMessageCatalogEntry quarantined = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.ServiceClientQuarantined);
        quarantined.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        quarantined.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        quarantined.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.14: the mailbox rate-limit catalog entry uses the transient retry-later + dependency-degraded
        // tokens (not request-access + disabled-action), since intake is deferred and retries automatically.
        ChatBotMessageCatalogEntry rateLimited = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.MailboxSourceRateLimited);
        rateLimited.NextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        rateLimited.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DependencyDegraded);

        // Story 7.17: the service-client rate-limit entry is transient (retry-later + dependency-degraded) —
        // deliberately distinct from the terminal service-client disable/quarantine entries (request-access +
        // disabled-action); the automation's command capacity is temporarily limited and retries shortly.
        ChatBotMessageCatalogEntry serviceClientRateLimited = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.ServiceClientRateLimited);
        serviceClientRateLimited.NextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        serviceClientRateLimited.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DependencyDegraded);
        serviceClientRateLimited.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.18: the AI-actor disable entry conveys terminal/await-admin guidance with the request-access +
        // disabled-action tokens — re-enable is a policy-admin/two-person action — deliberately distinct from the
        // transient retry-later set, and reusing the existing finite disabled-action reason.
        ChatBotMessageCatalogEntry aiActorDisabled = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AiActorDisabled);
        aiActorDisabled.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        aiActorDisabled.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        aiActorDisabled.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.19: the AI-actor quarantine entry conveys contained-for-review/await-admin guidance with the
        // request-access + disabled-action tokens — review/release is a policy-admin/two-person action — distinct
        // from the transient retry-later set, and reusing the existing finite disabled-action reason.
        ChatBotMessageCatalogEntry aiActorQuarantined = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AiActorQuarantined);
        aiActorQuarantined.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        aiActorQuarantined.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        aiActorQuarantined.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.20: the AI-actor rate-limit entry is transient (retry-later + dependency-degraded) — deliberately
        // distinct from the terminal AI-actor disable/quarantine entries (request-access + disabled-action); the AI
        // actor's proposal capacity is temporarily limited to protect reviewers and retries shortly.
        ChatBotMessageCatalogEntry aiActorRateLimited = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.AiActorRateLimited);
        aiActorRateLimited.NextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        aiActorRateLimited.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DependencyDegraded);
        aiActorRateLimited.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.21: the command-capability disable entry conveys terminal/await-admin guidance with the
        // request-access + disabled-action tokens — re-enable is a policy-admin/two-person action — deliberately
        // distinct from the transient retry-later set, and reusing the existing finite disabled-action reason.
        ChatBotMessageCatalogEntry commandCapabilityDisabled = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.CommandCapabilityDisabled);
        commandCapabilityDisabled.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        commandCapabilityDisabled.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        commandCapabilityDisabled.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.24: the outbound-channel disable entry conveys terminal/await-admin guidance with the request-access
        // + disabled-action tokens — re-enable is a policy-admin/two-person action — deliberately distinct from the
        // transient retry-later set, reusing the existing finite disabled-action reason (no new reason constant). This
        // matches the terminal Story 7.21 command-capability disable catalog choice, not a transient rate-limit entry.
        ChatBotMessageCatalogEntry outboundChannelDisabled = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.OutboundChannelDisabled);
        outboundChannelDisabled.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        outboundChannelDisabled.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        outboundChannelDisabled.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.22: the command-capability quarantine entry conveys contained-for-review/await-admin guidance with
        // the request-access + disabled-action tokens — review/release is a policy-admin/two-person action —
        // deliberately distinct from the transient retry-later set, and reusing the existing finite disabled-action
        // reason (no new reason constant).
        ChatBotMessageCatalogEntry commandCapabilityQuarantined = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.CommandCapabilityQuarantined);
        commandCapabilityQuarantined.NextAction.ShouldBe(ChatBotMessageNextActions.RequestAccess);
        commandCapabilityQuarantined.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DisabledAction);
        commandCapabilityQuarantined.Headline.Length.ShouldBeLessThanOrEqualTo(80);

        // Story 7.23: the command-capability rate-limit entry is transient (retry-later + dependency-degraded) —
        // deliberately distinct from the terminal command-capability disable/quarantine entries (request-access +
        // disabled-action); the command's capacity is temporarily limited to protect the tenant workflow and retries
        // shortly. This mirrors the AI-actor/service-client rate-limit entries, not the command-capability control entries.
        ChatBotMessageCatalogEntry commandCapabilityRateLimited = ChatBotMessageCatalog.Resolve(ChatBotMessageCodes.CommandCapabilityRateLimited);
        commandCapabilityRateLimited.NextAction.ShouldBe(ChatBotMessageNextActions.RetryLater);
        commandCapabilityRateLimited.DisabledActionReason.ShouldBe(ChatBotDisabledActionReasons.DependencyDegraded);
        commandCapabilityRateLimited.Headline.Length.ShouldBeLessThanOrEqualTo(80);
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
    public static void RefusalReasonTaxonomyShouldBeFiniteSafeAndCatalogBacked()
    {
        string[] expected =
        [
            "tenant-policy-exceeded",
            "project-authorization-denied",
            "sender-authority-denied",
            "approved-command-scope-exceeded",
            "command-not-allowlisted",
            "unsupported-action",
            "unresolved-association",
            "unresolved-participant",
            "missing-required-context",
            "context-package-unavailable",
            "evidence-expired",
            "policy-snapshot-unavailable",
            "approval-state-invalid",
            "corrected-context-invalidated",
            "dependency-degraded",
        ];

        ChatBotRefusalReasonCodes.All.ShouldBe(expected, ignoreOrder: false);
        foreach (string reasonCode in ChatBotRefusalReasonCodes.All)
        {
            reasonCode.Contains('_', StringComparison.Ordinal).ShouldBeFalse(reasonCode);
            ChatBotMessageCatalogEntry entry = ChatBotRefusalReasonCodes.CatalogEntryFor(reasonCode);
            ChatBotMessageCatalog.Entries.ShouldContain(entry);
            entry.DetailVisibility.ShouldBe(ChatBotDetailVisibility.MetadataOnly);
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
                "disabled-action",
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
            ChatBotDisabledActionReasons.DisabledAction,
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
