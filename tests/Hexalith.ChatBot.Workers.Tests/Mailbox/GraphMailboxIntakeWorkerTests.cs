using Hexalith.ChatBot.Client;
using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Workers.Mailbox;

using Shouldly;

using ContractCaptureMailboxMessageIntake = Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake;
using ContractMailboxAuthenticationVerdictKind = Hexalith.ChatBot.Contracts.Enums.MailboxAuthenticationVerdictKind;
using ContractMailboxAuthenticityMetadata = Hexalith.ChatBot.Contracts.Commands.MailboxAuthenticityMetadata;
using ContractMailboxAuthenticityStrictness = Hexalith.ChatBot.Contracts.Enums.MailboxAuthenticityStrictness;
using ContractMailboxDelegatedSenderState = Hexalith.ChatBot.Contracts.Enums.MailboxDelegatedSenderState;
using ContractMailboxHeaderDiscrepancyKind = Hexalith.ChatBot.Contracts.Enums.MailboxHeaderDiscrepancyKind;
using ContractMailboxHeaderValueState = Hexalith.ChatBot.Contracts.Enums.MailboxHeaderValueState;
using ContractMailboxPartyResolutionState = Hexalith.ChatBot.Contracts.Enums.MailboxPartyResolutionState;
using MailboxSourceControlState = Hexalith.ChatBot.Contracts.Enums.MailboxSourceControlState;
using MailboxRateLimitWindow = Hexalith.ChatBot.Contracts.Enums.MailboxRateLimitWindow;

namespace Hexalith.ChatBot.Workers.Tests.Mailbox;

public sealed class GraphMailboxIntakeWorkerTests
{
    [Fact]
    public async Task CreatedNotificationShouldSubmitTypedMailboxCommandWithMailboxOrigin()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        client.Submissions.Count.ShouldBe(1);
        client.Submissions[0].Origin.ShouldBe(ChatBotSurfaceOrigin.Mailbox);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake command =
            client.Submissions[0].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        command.Source.ProviderMessageId.ShouldBe("graph-message-001");
        command.Source.InternetMessageId.ShouldBe("<message-001@example.test>");
        command.Source.ConversationId.ShouldBe("graph-conversation-001");
        command.Source.MailboxId.ShouldBe("controlled-mailbox-001");
        command.Source.ReceivedAt.Offset.ShouldBe(TimeSpan.Zero);
        command.Source.SourceContext.ShouldBe("graph-message-v1");
        command.Recipients.Single().Kind.ShouldBe("to");
        command.Attachments.Single().ProviderAttachmentId.ShouldBe("attachment-001");
    }

    [Fact]
    public async Task DuplicateNotificationShouldSubmitSameProviderIdentityForGatewayIdempotency()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);
        GraphMailboxNotification notification = new("controlled-mailbox-001", "graph-message-001", null);

        _ = await worker.ProcessAsync(notification, cancellationToken: TestContext.Current.CancellationToken);
        _ = await worker.ProcessAsync(notification, cancellationToken: TestContext.Current.CancellationToken);

        client.Submissions.Count.ShouldBe(2);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake first =
            client.Submissions[0].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake second =
            client.Submissions[1].Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        first.IntakeId.ShouldNotBe(second.IntakeId);
        first.Source.MailboxId.ShouldBe(second.Source.MailboxId);
        first.Source.ProviderMessageId.ShouldBe(second.Source.ProviderMessageId);
    }

    [Fact]
    public async Task ProviderLocalTimestampsShouldSubmitUtcWhilePreservingSourceTimezoneContext()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            receivedAt: new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.FromHours(2)),
            sentAt: new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.FromHours(2)),
            createdAt: new DateTimeOffset(2026, 5, 30, 10, 5, 0, TimeSpan.FromHours(2)),
            sourceTimezone: "W. Europe Standard Time");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake command =
            client.Submissions.Single().Command.ShouldBeOfType<Hexalith.ChatBot.Contracts.Commands.CaptureMailboxMessageIntake>();
        command.Source.ReceivedAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 15, 0, TimeSpan.Zero));
        command.Source.SentAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 10, 0, TimeSpan.Zero));
        command.Source.CreatedAt.ShouldBe(new DateTimeOffset(2026, 5, 30, 8, 5, 0, TimeSpan.Zero));
        command.Source.SourceTimezone.ShouldBe("W. Europe Standard Time");
        command.Source.SourceContext.ShouldBe("graph-message-v1");
    }

    [Fact]
    public async Task OpaqueProviderStateShouldNotBeForwardedIntoCommandOrResult()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-secret-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        string commandText = client.Submissions.Single().Command.ToString() ?? string.Empty;
        commandText.ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
        result.ToString().ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
    }

    [Fact]
    public async Task AuthenticationResultsHeadersShouldMapToMetadataOnlyVerdictsAndDiscrepancies()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            replyTo: [new GraphMailboxParticipant("reply@example.test", "Reply")],
            headers:
            [
                new GraphMailboxInternetMessageHeader("Received", "from mx1.example.test by mx2.example.test"),
                new GraphMailboxInternetMessageHeader("authentication-results", "spf=pass smtp.mailfrom=sender.example; dkim=fail header.d=example.test; dmarc=bestguesspass action=none header.from=example.test; compauth=pass reason=109"),
                new GraphMailboxInternetMessageHeader("Authentication-Results", "spf=softfail smtp.mailfrom=other.example"),
                new GraphMailboxInternetMessageHeader("From", "Sender <sender@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Delegate <delegate@example.test>"),
                new GraphMailboxInternetMessageHeader("Reply-To", "Reply <reply@example.test>"),
                new GraphMailboxInternetMessageHeader("X-Original-Sender", "sender@example.test"),
                new GraphMailboxInternetMessageHeader("Subject", "must not be forwarded"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        ContractMailboxAuthenticityMetadata authenticity = command.Authenticity.ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.Ambiguous);
        authenticity.AuthenticationResults.Dkim.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.Dmarc.ShouldBe(ContractMailboxAuthenticationVerdictKind.BestGuessPass);
        authenticity.AuthenticationResults.CompositeAuthentication.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.CompositeAuthenticationReason.ShouldBe("109");
        authenticity.HeaderInspection.AuthenticationResultsHeaders.Select(static header => header.Ordinal).ShouldBe([0, 1], ignoreOrder: false);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MultipleAuthenticationResults);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromSenderMismatch);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromReplyToMismatch);

        string commandText = command.ToString() ?? string.Empty;
        commandText.ShouldNotContain("Subject", Case.Insensitive);
        commandText.ShouldNotContain("must not be forwarded", Case.Insensitive);
        commandText.ShouldNotContain("smtp.mailfrom", Case.Insensitive);
    }

    [Fact]
    public async Task RepeatedReceivedHeadersShouldPreserveProviderOrderAndInspectOriginalSenderDisagreement()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            headers:
            [
                new GraphMailboxInternetMessageHeader("received", "from mx1.example.test by mx2.example.test"),
                new GraphMailboxInternetMessageHeader("Received", "from mx2.example.test by mx3.example.test"),
                new GraphMailboxInternetMessageHeader("RECEIVED", " "),
                new GraphMailboxInternetMessageHeader("From", "Sender <sender@example.test>"),
                new GraphMailboxInternetMessageHeader("X-Original-Sender", "original@example.test"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractMailboxAuthenticityMetadata authenticity = client.Submissions.Single()
            .Command
            .ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.HeaderInspection.ReceivedHeaders.Select(static header => header.Name).ShouldBe(
            ["Received", "Received", "Received"],
            ignoreOrder: false);
        authenticity.HeaderInspection.ReceivedHeaders.Select(static header => header.Ordinal).ShouldBe([0, 1, 2], ignoreOrder: false);
        authenticity.HeaderInspection.ReceivedHeaders.Select(static header => header.ValueState).ShouldBe(
            [
                ContractMailboxHeaderValueState.Supplied,
                ContractMailboxHeaderValueState.Supplied,
                ContractMailboxHeaderValueState.Malformed,
            ],
            ignoreOrder: false);
        authenticity.HeaderInspection.XOriginalSender.ShouldBe(ContractMailboxHeaderValueState.Supplied);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromXOriginalSenderMismatch);
    }

    [Fact]
    public async Task ProviderSenderDifferentFromFromShouldRecordDelegateAuthorityAndPrincipalFor()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "Sender <sender@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Header Delegate <delegate@example.test>"),
                new GraphMailboxInternetMessageHeader("Reply-To", "Sender <sender@example.test>"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        command.Source.Sender.Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.ShouldNotBeNull().State.ShouldBe(ContractMailboxDelegatedSenderState.Delegated);
        command.Source.DelegatedSender.Delegate.ShouldNotBeNull().Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.PrincipalFor.ShouldNotBeNull().Address.ShouldBe("sender@example.test");
        command.Source.DelegatedSender.EvidenceRefs.ShouldContain("provider:sender");
        command.Source.DelegatedSender.EvidenceRefs.ShouldContain("provider:from");
        command.Source.ExternalSender.ShouldNotBeNull().ExternalSender.ShouldBeTrue();
        command.Source.ExternalSender.PartyResolutionState.ShouldBe(ContractMailboxPartyResolutionState.Unavailable);
        command.Authenticity.ShouldNotBeNull().StrictnessPolicy.ShouldNotBeNull().Strictness.ShouldBe(ContractMailboxAuthenticityStrictness.Strict);
    }

    [Fact]
    public async Task HeaderProviderConflictShouldKeepProviderAuthorityAndRecordAmbiguousDelegation()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            sender: new GraphMailboxParticipant("delegate@example.test", "Delegate"),
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "Forged <forged@example.test>"),
                new GraphMailboxInternetMessageHeader("Sender", "Other <other@example.test>"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        _ = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        command.Source.Sender.Address.ShouldBe("delegate@example.test");
        command.Source.DelegatedSender.ShouldNotBeNull().State.ShouldBe(ContractMailboxDelegatedSenderState.Ambiguous);
        command.Source.DelegatedSender.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.FromSenderMismatch);
    }

    [Fact]
    public async Task RepeatedAuthenticationResultsShouldFillMissingVerdictsFromLaterHeaders()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            headers:
            [
                new GraphMailboxInternetMessageHeader("Authentication-Results", "spf=pass smtp.mailfrom=sender.example"),
                new GraphMailboxInternetMessageHeader("Authentication-Results", "dkim=pass header.d=example.test; dmarc=fail action=oreject header.from=example.test; compauth=fail reason=001"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractMailboxAuthenticityMetadata authenticity = client.Submissions.Single()
            .Command
            .ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.Dkim.ShouldBe(ContractMailboxAuthenticationVerdictKind.Pass);
        authenticity.AuthenticationResults.Dmarc.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.CompositeAuthentication.ShouldBe(ContractMailboxAuthenticationVerdictKind.Fail);
        authenticity.AuthenticationResults.CompositeAuthenticationReason.ShouldBe("001");
        authenticity.HeaderInspection.AuthenticationResultsHeaders.Select(static header => header.Ordinal).ShouldBe([0, 1], ignoreOrder: false);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MultipleAuthenticationResults);
    }

    [Fact]
    public async Task MissingAndMalformedHeadersShouldSubmitRecoverableMetadata()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage message = Message(
            headers:
            [
                new GraphMailboxInternetMessageHeader("From", "not an address"),
            ]);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        ContractMailboxAuthenticityMetadata authenticity = client.Submissions.Single()
            .Command
            .ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Authenticity
            .ShouldNotBeNull();
        authenticity.AuthenticationResults.Spf.ShouldBe(ContractMailboxAuthenticationVerdictKind.NotSupplied);
        authenticity.HeaderInspection.From.ShouldBe(ContractMailboxHeaderValueState.Malformed);
        authenticity.HeaderInspection.Sender.ShouldBe(ContractMailboxHeaderValueState.NotSupplied);
        authenticity.HeaderInspection.Discrepancies.ShouldContain(ContractMailboxHeaderDiscrepancyKind.MalformedFrom);
    }

    [Theory]
    [InlineData("graph_throttled")]
    [InlineData("graph_subscription_expired")]
    [InlineData("graph_token_expired")]
    [InlineData("graph_partial_access")]
    public async Task RetryableGraphFailuresShouldRemainScopedRecoverableMailboxDegradation(string reasonCode)
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.RetryableFailure(reasonCode)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe(reasonCode);
        result.OperationClass.ShouldBe("message-intake");
        result.RetryCount.ShouldBe(1);
        result.MaxAttempts.ShouldBeGreaterThan(1);
        result.NextRetryAt.ShouldNotBeNull();
        result.SafeNextAction.ShouldBe("retry-later");
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task RevokedCredentialShouldRecoverWithoutSubmittingCommand()
    {
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.PermissionRevoked()), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("graph_permission_revoked");
        result.NextRetryAt.ShouldBeNull();
        result.OwnerRole.ShouldBe("tenant-admin");
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchedMessageWithDifferentProviderIdentityShouldFailClosedWithoutSubmit()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage mismatched = Message(providerMessageId: "foreign-message-002");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(mismatched)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_message_scope_mismatch");
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("foreign-message-002", Case.Sensitive);
    }

    [Fact]
    public async Task FetchedMessageFromDifferentMailboxShouldFailClosedWithoutSubmit()
    {
        RecordingChatBotClient client = new();
        GraphMailboxMessage mismatched = Message(mailboxId: "foreign-mailbox");
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(mismatched)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_message_scope_mismatch");
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("foreign-mailbox", Case.Sensitive);
    }

    [Theory]
    [InlineData(403, "authorization_denied")]
    [InlineData(503, "audit_unavailable")]
    public async Task RecoverableGatewaySubmissionFailuresShouldReturnSafeWorkerResult(int statusCode, string code)
    {
        RejectingChatBotClient client = new(statusCode, code);
        GraphMailboxIntakeWorker worker = new(Pattern(), new FakeGraphSource(GraphMailboxFetchResult.Found(Message())), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque-secret-delta-token"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe(code);
        result.IntakeId.ShouldBeNull();
        result.ToString().ShouldNotContain("opaque-secret-delta-token", Case.Sensitive);
    }

    [Fact]
    public async Task ForeignMailboxShouldFailClosedBeforeGraphFetch()
    {
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        GraphMailboxIntakeWorker worker = new(Pattern(), source, client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("foreign-mailbox", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_scope_mismatch");
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task DisabledMailboxSourceShouldBlockIntakeBeforeFetchWhileSiblingActiveSourceIsUnaffected()
    {
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message(mailboxId: "controlled-mailbox-002")));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [
                new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", MailboxSourceControlState.Disabled),
                new ControlledMailboxPattern("controlled-mailbox-002", "graph-message-v2", MailboxSourceControlState.Active),
            ]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client);

        MailboxIntakeWorkerResult disabled = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        disabled.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        disabled.ReasonCode.ShouldBe("mailbox_source_disabled");
        disabled.OwnerRole.ShouldBe("mailbox-admin");
        // Recoverable await-admin outcome (mailbox-admin re-enablement), not a poison drop and not a blind retry loop:
        // no auto-retry is scheduled and the safe next action escalates to the owning admin.
        disabled.OperationClass.ShouldBe("message-intake");
        disabled.NextRetryAt.ShouldBeNull();
        disabled.SafeNextAction.ShouldBe("escalate");
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
        disabled.ToString().ShouldNotContain("@", Case.Sensitive);

        // Isolation: a still-Active sibling source for the same tenant continues to process normally.
        MailboxIntakeWorkerResult active = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-002", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        active.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        source.FetchCount.ShouldBe(1);
        client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Source.MailboxId.ShouldBe("controlled-mailbox-002");
    }

    [Fact]
    public async Task QuarantinedMailboxSourceShouldRouteIntakeBeforeFetchWhileSiblingActiveSourceIsUnaffected()
    {
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message(mailboxId: "controlled-mailbox-002")));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [
                new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", MailboxSourceControlState.Quarantined),
                new ControlledMailboxPattern("controlled-mailbox-002", "graph-message-v2", MailboxSourceControlState.Active),
            ]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client);

        MailboxIntakeWorkerResult quarantined = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        quarantined.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        quarantined.ReasonCode.ShouldBe("mailbox_source_quarantined");
        quarantined.OwnerRole.ShouldBe("mailbox-admin");
        // Recoverable await-admin outcome (mailbox-admin review/release), not a poison drop and not a blind retry loop:
        // no auto-retry is scheduled and the safe next action escalates to the owning admin.
        quarantined.OperationClass.ShouldBe("message-intake");
        quarantined.NextRetryAt.ShouldBeNull();
        quarantined.SafeNextAction.ShouldBe("escalate");
        // No restricted content is fetched or read for the quarantined source, and no normal-pipeline intake is created.
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
        quarantined.ToString().ShouldNotContain("@", Case.Sensitive);

        // Isolation: a still-Active sibling source for the same tenant continues to process normally.
        MailboxIntakeWorkerResult active = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-002", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        active.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        source.FetchCount.ShouldBe(1);
        client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Source.MailboxId.ShouldBe("controlled-mailbox-002");
    }

    [Fact]
    public async Task RateLimitedSourceAtBudgetShouldDeferBeforeFetchWhileSiblingAndUnderBudgetAreUnaffected()
    {
        FixedIntakeClock clock = new(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        FakeIntakeHistory history = new();
        // Source-001 has already captured exactly its budget (3) within the trailing hour → the next message defers.
        history.Seed("tenant-alpha", "controlled-mailbox-001", clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddMinutes(-30), clock.UtcNow.AddMinutes(-55));
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message(mailboxId: "controlled-mailbox-002")));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [
                new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", RateLimit: new MailboxRateLimitState(3, MailboxRateLimitWindow.RollingHour)),
                new ControlledMailboxPattern("controlled-mailbox-002", "graph-message-v2", RateLimit: new MailboxRateLimitState(3, MailboxRateLimitWindow.RollingHour)),
            ]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client, clock, history);

        MailboxIntakeWorkerResult deferred = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        // Defer (never drop) on the retryable path BEFORE any fetch: no restricted content is read for the source.
        deferred.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        deferred.ReasonCode.ShouldBe("mailbox_source_rate_limited");
        deferred.NextRetryAt.ShouldNotBeNull();
        deferred.SafeNextAction.ShouldBe("retry-later");
        deferred.OwnerRole.ShouldBe("mailbox-operator");
        deferred.RateLimit.ShouldNotBeNull();
        deferred.RateLimit.Deferred.ShouldBeTrue();
        deferred.RateLimit.Budget.ShouldBe(3);
        deferred.RateLimit.ObservedWindowCount.ShouldBe(3);
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();

        // Isolation (NFR30): a sibling source with its own independent (empty) counter is unaffected and submits.
        MailboxIntakeWorkerResult sibling = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-002", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        sibling.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        sibling.RateLimit.ShouldNotBeNull();
        sibling.RateLimit.Deferred.ShouldBeFalse();
        sibling.RateLimit.ObservedWindowCount.ShouldBe(0);
        source.FetchCount.ShouldBe(1);
        client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>()
            .Source.MailboxId.ShouldBe("controlled-mailbox-002");
        // The successful sibling capture advanced only its own counter.
        history.GetIntakeTimestamps("tenant-alpha", "controlled-mailbox-002").Count.ShouldBe(1);
        history.GetIntakeTimestamps("tenant-alpha", "controlled-mailbox-001").Count.ShouldBe(3);
    }

    [Fact]
    public async Task RateLimitedSourceUnderBudgetShouldProcessNormallyAndAdvanceCounterOnSuccess()
    {
        FixedIntakeClock clock = new(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        FakeIntakeHistory history = new();
        // One prior capture within the window; budget is 3 → under budget, processes normally.
        history.Seed("tenant-alpha", "controlled-mailbox-001", clock.UtcNow.AddMinutes(-10));
        // An aged-out capture (older than the rolling hour) must not count toward the window.
        history.Seed("tenant-alpha", "controlled-mailbox-001", clock.UtcNow.AddMinutes(-75));
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", RateLimit: new MailboxRateLimitState(3, MailboxRateLimitWindow.RollingHour))]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client, clock, history);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        result.RateLimit.ShouldNotBeNull();
        result.RateLimit.Deferred.ShouldBeFalse();
        result.RateLimit.ObservedWindowCount.ShouldBe(1);
        source.FetchCount.ShouldBe(1);
        client.Submissions.ShouldHaveSingleItem();
        // Counter advanced only on the successful capture: now two in-window timestamps.
        MailboxRateLimitState.CountInTrailingWindow(
            history.GetIntakeTimestamps("tenant-alpha", "controlled-mailbox-001"),
            clock.UtcNow,
            TimeSpan.FromHours(1)).ShouldBe(2);
    }

    [Fact]
    public async Task RateLimitedSourceWithOutOfBoundsBudgetShouldFallBackToSafeDefaultNeverRaisingTheCap()
    {
        FixedIntakeClock clock = new(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        FakeIntakeHistory history = new();
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", RateLimit: new MailboxRateLimitState(MailboxRateLimitBounds.Maximum + 5000, MailboxRateLimitWindow.RollingHour))]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client, clock, history);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        // The out-of-bounds budget falls back to the safe default (the declared maximum) — never the raised value.
        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        result.RateLimit.ShouldNotBeNull();
        result.RateLimit.Budget.ShouldBe(MailboxRateLimitBounds.Maximum);
    }

    [Theory]
    [InlineData(MailboxSourceControlState.Disabled, "mailbox_source_disabled")]
    [InlineData(MailboxSourceControlState.Quarantined, "mailbox_source_quarantined")]
    public async Task ControlStateBlockShouldTakePrecedenceOverRateLimitForNonActiveSource(
        MailboxSourceControlState controlState,
        string expectedReason)
    {
        FixedIntakeClock clock = new(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        FakeIntakeHistory history = new();
        // Seed the trailing window AT the budget: if the rate-limit branch were (wrongly) evaluated for a non-Active
        // source it would defer with "mailbox_source_rate_limited". The control-state block must win instead.
        history.Seed("tenant-alpha", "controlled-mailbox-001", clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddMinutes(-15), clock.UtcNow.AddMinutes(-25));
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", controlState, new MailboxRateLimitState(3, MailboxRateLimitWindow.RollingHour))]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client, clock, history);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        // AC5: the rate-limit check sits AFTER the disable/quarantine control-state blocks and applies only to an
        // Active source. A blocked source returns its control-state await-admin reason — never the rate-limit defer,
        // never fetches restricted content, and carries no rate-limit observation.
        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe(expectedReason);
        result.RateLimit.ShouldBeNull();
        result.NextRetryAt.ShouldBeNull();
        result.SafeNextAction.ShouldBe("escalate");
        result.OwnerRole.ShouldBe("mailbox-admin");
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("@", Case.Sensitive);
    }

    [Fact]
    public async Task RateLimitCounterShouldBeIndependentPerTenantForTheSameMailboxSource()
    {
        FixedIntakeClock clock = new(new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero));
        // One shared history store keyed per (tenant × mailbox source) — the per-source isolation seam.
        FakeIntakeHistory history = new();
        // tenant-alpha's counter for the shared mailbox source id is already AT budget; tenant-beta's is untouched.
        history.Seed("tenant-alpha", "controlled-mailbox-001", clock.UtcNow.AddMinutes(-5), clock.UtcNow.AddMinutes(-15), clock.UtcNow.AddMinutes(-25));
        MailboxRateLimitState budget = new(3, MailboxRateLimitWindow.RollingHour);

        FakeGraphSource alphaSource = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient alphaClient = new();
        GraphMailboxIntakeWorker alphaWorker = new(
            "tenant-alpha",
            new RecordingMailboxConfigurationProvider("tenant-alpha", [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", RateLimit: budget)]),
            alphaSource,
            alphaClient,
            clock,
            history);

        FakeGraphSource betaSource = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient betaClient = new();
        GraphMailboxIntakeWorker betaWorker = new(
            "tenant-beta",
            new RecordingMailboxConfigurationProvider("tenant-beta", [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1", RateLimit: budget)]),
            betaSource,
            betaClient,
            clock,
            history);

        MailboxIntakeWorkerResult alpha = await alphaWorker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);
        MailboxIntakeWorkerResult beta = await betaWorker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-001", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        // Isolation (NFR30/NFR18): the trailing-window counter is keyed per (tenant × mailbox source). tenant-alpha is
        // at budget and defers; tenant-beta shares the same mailbox source id but has its own empty counter, so it is
        // unaffected and processes normally — deferring a noisy tenant never throttles or starves another tenant.
        alpha.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        alpha.ReasonCode.ShouldBe("mailbox_source_rate_limited");
        alphaSource.FetchCount.ShouldBe(0);
        alphaClient.Submissions.ShouldBeEmpty();

        beta.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        beta.RateLimit.ShouldNotBeNull();
        beta.RateLimit.Deferred.ShouldBeFalse();
        beta.RateLimit.ObservedWindowCount.ShouldBe(0);
        betaSource.FetchCount.ShouldBe(1);
        betaClient.Submissions.ShouldHaveSingleItem();

        // Each tenant's counter advanced (or not) independently: the successful tenant-beta capture touched only its
        // own key; tenant-alpha's deferred message never advanced its counter.
        history.GetIntakeTimestamps("tenant-beta", "controlled-mailbox-001").Count.ShouldBe(1);
        history.GetIntakeTimestamps("tenant-alpha", "controlled-mailbox-001").Count.ShouldBe(3);
    }

    [Fact]
    public async Task TenantScopedConfigurationProviderShouldSelectMatchingMailboxPattern()
    {
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new(
            "tenant-alpha",
            [
                new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1"),
                new ControlledMailboxPattern("controlled-mailbox-002", "graph-message-v2"),
            ]);
        GraphMailboxMessage message = Message(mailboxId: "controlled-mailbox-002");
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, new FakeGraphSource(GraphMailboxFetchResult.Found(message)), client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("controlled-mailbox-002", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Submitted);
        provider.Requests.ShouldBe([("tenant-alpha", "controlled-mailbox-002")], ignoreOrder: false);
        ContractCaptureMailboxMessageIntake command = client.Submissions.Single().Command.ShouldBeOfType<ContractCaptureMailboxMessageIntake>();
        command.Source.MailboxId.ShouldBe("controlled-mailbox-002");
        command.Source.SourceContext.ShouldBe("graph-message-v2");
    }

    [Fact]
    public async Task UnknownConfiguredMailboxShouldReturnScopedRecoverableDegradationBeforeGraphFetch()
    {
        FakeGraphSource source = new(GraphMailboxFetchResult.Found(Message()));
        RecordingChatBotClient client = new();
        RecordingMailboxConfigurationProvider provider = new("tenant-alpha", [new ControlledMailboxPattern("controlled-mailbox-001", "graph-message-v1")]);
        GraphMailboxIntakeWorker worker = new("tenant-alpha", provider, source, client);

        MailboxIntakeWorkerResult result = await worker.ProcessAsync(
            new GraphMailboxNotification("foreign-mailbox", "graph-message-001", "opaque"),
            cancellationToken: TestContext.Current.CancellationToken);

        result.Kind.ShouldBe(MailboxIntakeWorkerResultKind.Recoverable);
        result.ReasonCode.ShouldBe("mailbox_scope_mismatch");
        result.OwnerRole.ShouldBe("mailbox-admin");
        source.FetchCount.ShouldBe(0);
        client.Submissions.ShouldBeEmpty();
        result.ToString().ShouldNotContain("controlled-mailbox-001", Case.Sensitive);
    }

    [Fact]
    public static void WorkerShouldDocumentLeastPrivilegeGraphPermission()
    {
        GraphMailboxIntakeWorker.LeastPrivilegeGraphPermission.ShouldBe("Mail.Read");
    }

    private static ControlledMailboxPattern Pattern()
        => new("controlled-mailbox-001", "graph-message-v1");

    private static GraphMailboxMessage Message(
        string mailboxId = "controlled-mailbox-001",
        string providerMessageId = "graph-message-001",
        DateTimeOffset? receivedAt = null,
        DateTimeOffset? sentAt = null,
        DateTimeOffset? createdAt = null,
        string sourceTimezone = "UTC",
        GraphMailboxParticipant? sender = null,
        IReadOnlyList<GraphMailboxParticipant>? replyTo = null,
        IReadOnlyList<GraphMailboxInternetMessageHeader>? headers = null)
        => new(
            mailboxId,
            providerMessageId,
            "<message-001@example.test>",
            "graph-conversation-001",
            "graph-thread-001",
            new GraphMailboxParticipant("sender@example.test", "Sender"),
            sender,
            replyTo ?? [],
            [new GraphMailboxRecipient("project@example.test", "Project", "to")],
            receivedAt ?? new DateTimeOffset(2026, 5, 30, 10, 15, 0, TimeSpan.Zero),
            sentAt ?? new DateTimeOffset(2026, 5, 30, 10, 10, 0, TimeSpan.Zero),
            createdAt ?? new DateTimeOffset(2026, 5, 30, 10, 5, 0, TimeSpan.Zero),
            sourceTimezone,
            [new GraphMailboxAttachment("attachment-001", "evidence.pdf", "application/pdf", 1024)],
            headers ?? []);

    private sealed class FixedIntakeClock(DateTimeOffset utcNow) : IMailboxIntakeClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }

    private sealed class FakeIntakeHistory : IMailboxSourceIntakeHistory
    {
        private readonly Dictionary<string, List<DateTimeOffset>> _timestamps = new(StringComparer.Ordinal);

        public void Seed(string tenantRef, string mailboxSourceRef, params DateTimeOffset[] timestamps)
        {
            foreach (DateTimeOffset timestamp in timestamps)
            {
                RecordIntake(tenantRef, mailboxSourceRef, timestamp);
            }
        }

        public IReadOnlyList<DateTimeOffset> GetIntakeTimestamps(string tenantRef, string mailboxSourceRef)
            => _timestamps.TryGetValue(Key(tenantRef, mailboxSourceRef), out List<DateTimeOffset>? list) ? list : [];

        public void RecordIntake(string tenantRef, string mailboxSourceRef, DateTimeOffset capturedAtUtc)
        {
            string key = Key(tenantRef, mailboxSourceRef);
            if (!_timestamps.TryGetValue(key, out List<DateTimeOffset>? list))
            {
                list = [];
                _timestamps[key] = list;
            }

            list.Add(capturedAtUtc.ToUniversalTime());
        }

        private static string Key(string tenantRef, string mailboxSourceRef)
            => $"{tenantRef} {mailboxSourceRef}";
    }

    private sealed class FakeGraphSource(GraphMailboxFetchResult result) : IGraphMailboxMessageSource
    {
        public int FetchCount { get; private set; }

        public ValueTask<GraphMailboxFetchResult> FetchMessageAsync(GraphMailboxNotification notification, CancellationToken cancellationToken)
        {
            FetchCount++;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RecordingMailboxConfigurationProvider(string tenantId, IReadOnlyList<ControlledMailboxPattern> patterns) : IMailboxConfigurationProvider
    {
        public List<(string TenantId, string MailboxId)> Requests { get; } = [];

        public ValueTask<ControlledMailboxPattern?> ResolvePatternAsync(
            string requestedTenantId,
            string notificationMailboxId,
            CancellationToken cancellationToken)
        {
            Requests.Add((requestedTenantId, notificationMailboxId));
            return ValueTask.FromResult(
                string.Equals(requestedTenantId, tenantId, StringComparison.Ordinal)
                    ? patterns.SingleOrDefault(pattern => string.Equals(pattern.MailboxId, notificationMailboxId, StringComparison.Ordinal))
                    : null);
        }
    }

    private sealed class RecordingChatBotClient : IChatBotClient
    {
        public List<Submission> Submissions { get; } = [];

        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
        {
            Submissions.Add(new Submission(command, correlationId, taskId, origin));
            return Task.FromResult(new CommandSubmissionResponse
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                TaskId = taskId,
                LifecycleState = Hexalith.ChatBot.Client.Generated.LifecycleState.Proposed,
                AcceptedAt = DateTimeOffset.UtcNow,
            });
        }

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RejectingChatBotClient(int statusCode, string code) : IChatBotClient
    {
        public Task<CommandSubmissionResponse> SubmitAsync(
            IChatBotCommand command,
            string? correlationId = null,
            string? taskId = null,
            ChatBotSurfaceOrigin origin = ChatBotSurfaceOrigin.Api,
            CancellationToken cancellationToken = default)
            => throw new HexalithChatBotApiException<ProblemDetails>(
                "Metadata-only failure.",
                statusCode,
                null,
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal),
                new ProblemDetails
                {
                    Code = code,
                    Status = statusCode,
                    CorrelationId = correlationId ?? "01ARZ3NDEKTSV4RRFFQ69G5FAW",
                    Details = new ProblemDetailsDetails { Visibility = ProblemDetailsDetailsVisibility.Metadata_only },
                },
                innerException: null);

        public Task<OperationStatus> GetOperationStatusAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<OperationAuditHistory> GetOperationAuditHistoryAsync(
            string operationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssociationRoutingStatus> GetAssociationRoutingStatusAsync(
            string associationId,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<ProjectConversationResponse> GetProjectConversationAsync(
            string projectId,
            string? cursor = null,
            int pageSize = 25,
            string? correlationId = null,
            string? taskId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed record Submission(
        IChatBotCommand Command,
        string? CorrelationId,
        string? TaskId,
        ChatBotSurfaceOrigin Origin);
}
