using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Adapters.Mailbox;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class AttachmentSafetyPolicyTests
{
    [Theory]
    [InlineData(null, AttachmentUnsafeHandling.Quarantine)]
    [InlineData("", AttachmentUnsafeHandling.Quarantine)]
    [InlineData("invalid", AttachmentUnsafeHandling.Quarantine)]
    [InlineData(AttachmentUnsafeHandling.Quarantine, AttachmentUnsafeHandling.Quarantine)]
    [InlineData(AttachmentUnsafeHandling.Block, AttachmentUnsafeHandling.Block)]
    [InlineData(AttachmentUnsafeHandling.RejectMessage, AttachmentUnsafeHandling.RejectMessage)]
    public async Task SafetyPolicyShouldNormalizeUnsafeHandlingMode(string? configured, string expected)
    {
        DefaultAttachmentSafetyPolicy policy = new(new FixedAttachmentScanner(AttachmentScanResult.Clean()));

        ProjectConversationAttachmentSafetyOutcomeView outcome = await policy
            .EvaluateAsync(Request(configured), TestContext.Current.CancellationToken);

        outcome.UnsafeHandling.ShouldBe(expected);
    }

    [Theory]
    [InlineData(AttachmentUnsafeHandling.Quarantine, ProjectConversationAttachmentStatus.Unsafe, "quarantine-review")]
    [InlineData(AttachmentUnsafeHandling.Block, ProjectConversationAttachmentStatus.Unsafe, "blocked-by-policy")]
    [InlineData(AttachmentUnsafeHandling.RejectMessage, ProjectConversationAttachmentStatus.Rejected, "review-source-evidence")]
    public async Task UnsafeScannerResultShouldFailClosedByUnsafeHandlingMode(
        string unsafeHandling,
        ProjectConversationAttachmentStatus expectedStatus,
        string expectedNextAction)
    {
        DefaultAttachmentSafetyPolicy policy = new(new FixedAttachmentScanner(AttachmentScanResult.Unsafe("ignored raw malware family")));

        ProjectConversationAttachmentSafetyOutcomeView outcome = await policy
            .EvaluateAsync(Request(unsafeHandling), TestContext.Current.CancellationToken);

        outcome.ScanStatus.ShouldBe(expectedStatus);
        outcome.AiContextEligibility.ShouldBe("not-eligible");
        outcome.AllowedActions.ShouldBeEmpty();
        outcome.SafeNextAction.ShouldBe(expectedNextAction);
        outcome.ReasonCode.ShouldNotContain("malware", Case.Insensitive);
    }

    [Theory]
    [InlineData((int)AttachmentScanResultKind.Unavailable, ProjectConversationAttachmentStatus.Unavailable, "inspect-later")]
    [InlineData((int)AttachmentScanResultKind.Retryable, ProjectConversationAttachmentStatus.Retryable, "retry-scan")]
    [InlineData((int)AttachmentScanResultKind.Failed, ProjectConversationAttachmentStatus.Failed, "inspect-later")]
    [InlineData((int)AttachmentScanResultKind.Indeterminate, ProjectConversationAttachmentStatus.Retryable, "retry-scan")]
    public async Task ScannerDegradationShouldNeverGrantFileOrAiActions(
        int resultKind,
        ProjectConversationAttachmentStatus expectedStatus,
        string expectedNextAction)
    {
        DefaultAttachmentSafetyPolicy policy = new(new FixedAttachmentScanner(new AttachmentScanResult((AttachmentScanResultKind)resultKind, "raw scanner path C:\\secret\\sample.bin")));

        ProjectConversationAttachmentSafetyOutcomeView outcome = await policy
            .EvaluateAsync(Request(AttachmentUnsafeHandling.Quarantine), TestContext.Current.CancellationToken);

        outcome.ScanStatus.ShouldBe(expectedStatus);
        outcome.AiContextEligibility.ShouldBe("not-eligible");
        outcome.AllowedActions.ShouldBeEmpty();
        outcome.SafeNextAction.ShouldBe(expectedNextAction);
        outcome.ReasonCode.ShouldNotContain("C:\\", Case.Insensitive);
    }

    [Fact]
    public async Task SizeAndContentTypeRestrictionsShouldRejectBeforeScanner()
    {
        CountingAttachmentScanner scanner = new();
        DefaultAttachmentSafetyPolicy policy = new(scanner);

        ProjectConversationAttachmentSafetyOutcomeView sizeOutcome = await policy
            .EvaluateAsync(Request(AttachmentUnsafeHandling.Quarantine) with { SizeInBytes = DefaultAttachmentSafetyPolicy.DefaultMaxSizeInBytes + 1 }, TestContext.Current.CancellationToken);
        ProjectConversationAttachmentSafetyOutcomeView typeOutcome = await policy
            .EvaluateAsync(Request(AttachmentUnsafeHandling.Quarantine) with { ContentType = "application/x-msdownload" }, TestContext.Current.CancellationToken);

        scanner.Calls.ShouldBe(0);
        sizeOutcome.ScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Rejected);
        typeOutcome.ScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Rejected);
        sizeOutcome.AiContextEligibility.ShouldBe("not-eligible");
        typeOutcome.AllowedActions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData((int)MailboxAttachmentContentResultKind.Unavailable, ProjectConversationAttachmentStatus.Unavailable, "inspect-later")]
    [InlineData((int)MailboxAttachmentContentResultKind.Retryable, ProjectConversationAttachmentStatus.Retryable, "retry-scan")]
    public async Task UnavailableContentShouldFailClosedBeforeScanner(
        int contentKind,
        ProjectConversationAttachmentStatus expectedStatus,
        string expectedNextAction)
    {
        CountingAttachmentScanner scanner = new();
        DefaultAttachmentSafetyPolicy policy = new(scanner);
        MailboxAttachmentContentResult content = (MailboxAttachmentContentResultKind)contentKind is MailboxAttachmentContentResultKind.Retryable
            ? MailboxAttachmentContentResult.Retryable("raw provider payload /tmp/secret")
            : MailboxAttachmentContentResult.Unavailable("raw provider payload /tmp/secret");

        ProjectConversationAttachmentSafetyOutcomeView outcome = await policy
            .EvaluateAsync(Request(AttachmentUnsafeHandling.Quarantine) with { Content = content }, TestContext.Current.CancellationToken);

        scanner.Calls.ShouldBe(0);
        outcome.ScanStatus.ShouldBe(expectedStatus);
        outcome.AiContextEligibility.ShouldBe("not-eligible");
        outcome.AllowedActions.ShouldBeEmpty();
        outcome.SafeNextAction.ShouldBe(expectedNextAction);
        outcome.ReasonCode.ShouldNotContain("provider", Case.Insensitive);
        outcome.ReasonCode.ShouldNotContain("/tmp", Case.Insensitive);
    }

    [Fact]
    public async Task CleanScanShouldMakeCapturedStorageEligibleForGovernedFileAndAiActions()
    {
        DefaultAttachmentSafetyPolicy policy = new(new FixedAttachmentScanner(AttachmentScanResult.Clean()));

        ProjectConversationAttachmentSafetyOutcomeView outcome = await policy
            .EvaluateAsync(Request(AttachmentUnsafeHandling.Quarantine), TestContext.Current.CancellationToken);

        outcome.ScanStatus.ShouldBe(ProjectConversationAttachmentStatus.Captured);
        outcome.AiContextEligibility.ShouldBe("eligible");
        outcome.AllowedActions.ShouldBe(["add-to-ai-context", "open-governed-file"], ignoreOrder: true);
        outcome.SafeNextAction.ShouldBe("none");
    }

    private static AttachmentSafetyPolicyRequest Request(string? unsafeHandling)
        => new(
            "tenant-alpha",
            "project-001",
            "association-001",
            "intake-001",
            "mailbox-001",
            "message-001",
            "attachment-001",
            0,
            "invoice.pdf",
            "application/pdf",
            1024,
            ProjectConversationAttachmentStatus.Captured,
            "folder-001",
            "file-001",
            MailboxAttachmentContentResult.Available("hello"u8.ToArray(), "application/pdf", "hashref_safe"),
            10,
            "correlation-001",
            unsafeHandling);

    private sealed class FixedAttachmentScanner(AttachmentScanResult result) : IAttachmentScanner
    {
        public ValueTask<AttachmentScanResult> ScanAsync(AttachmentScanRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CountingAttachmentScanner : IAttachmentScanner
    {
        public int Calls { get; private set; }

        public ValueTask<AttachmentScanResult> ScanAsync(AttachmentScanRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(AttachmentScanResult.Clean());
        }
    }
}
