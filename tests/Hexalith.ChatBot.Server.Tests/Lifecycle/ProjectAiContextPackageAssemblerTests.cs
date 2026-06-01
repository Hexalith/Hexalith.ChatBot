using System.Reflection;

using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;
using Hexalith.ChatBot.Server.Lifecycle.Attachments;
using Hexalith.ChatBot.Server.Projections;

using Shouldly;

namespace Hexalith.ChatBot.Server.Tests.Lifecycle;

public sealed class ProjectAiContextPackageAssemblerTests
{
    private const string Tenant = "tenant-alpha";
    private const string OtherTenant = "tenant-beta";
    private const string Project = "project-001";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
    private static readonly DateTimeOffset OccurredAt = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthorizedCleanAttachmentShouldBeIncludedWithMetadataOnlyReferences()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView association = Association("association-001", 7);
        ProjectConversationItemView attachment = Attachment("attachment-001", 8) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context", "open-governed-file"],
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [association, attachment], CorrelationId),
            TestContext.Current.CancellationToken);

        package.TenantId.ShouldStartWith("tenant:");
        package.TenantId.ShouldNotContain(Tenant, Case.Sensitive);
        package.ProjectId.ShouldBe(Project);
        package.PolicySnapshotId.ShouldBe("association-thresholds.m0.default.v1");
        package.RedactionDecision.ShouldBe("metadata_only");
        package.RetentionClass.ShouldBe("collaboration_input");
        package.ProviderReuseSetting.ShouldBe("disabled");
        package.SchemaVersion.ShouldBe(ProjectAiContextPackage.SchemaVersionValue);
        package.SourceVersion.ShouldBe(8);
        package.SourceEvidenceReferences.ShouldContain("conversation-001");
        ProjectAiContextPackageFile included = package.IncludedFiles.ShouldHaveSingleItem();
        included.FolderId.ShouldBe("folder-001");
        included.FileId.ShouldBe("file-001");
        included.SourceProviderAttachmentId.ShouldBe("provider-attachment-001");
        included.RedactionState.ShouldBe("metadata_only");
        included.RetentionClass.ShouldBe("collaboration_input");
        package.ExcludedFiles.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(ProjectConversationAttachmentStatus.Pending, "pending", "pending-scan")]
    [InlineData(ProjectConversationAttachmentStatus.Unsafe, "not-eligible", "unsafe")]
    [InlineData(ProjectConversationAttachmentStatus.Rejected, "not-eligible", "rejected")]
    [InlineData(ProjectConversationAttachmentStatus.Failed, "not-eligible", "failed")]
    [InlineData(ProjectConversationAttachmentStatus.Unavailable, "not-eligible", "unavailable")]
    [InlineData(ProjectConversationAttachmentStatus.Retryable, "not-eligible", "retryable")]
    public async Task IneligibleAttachmentsShouldBeExcludedWithStableReason(
        ProjectConversationAttachmentStatus scanStatus,
        string eligibility,
        string expectedReason)
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView attachment = Attachment($"attachment-{expectedReason}", 4) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = scanStatus,
            AttachmentAiContextEligibility = eligibility,
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [Association("association-001", 3), attachment], CorrelationId),
            TestContext.Current.CancellationToken);

        package.IncludedFiles.ShouldBeEmpty();
        package.ExcludedFiles.ShouldHaveSingleItem().ReasonCode.ShouldBe(expectedReason);
    }

    [Fact]
    public async Task RedactedAttachmentShouldNotLeakFolderFileOrProviderReferences()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView attachment = Attachment("attachment-redacted", 4) with
        {
            RedactionState = "redacted",
            AttachmentRedactionState = "redacted",
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "redacted",
            AttachmentFolderId = "folder-secret",
            AttachmentFileId = "file-secret",
            SourceProviderAttachmentId = "provider-secret",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [Association("association-001", 3), attachment], CorrelationId),
            TestContext.Current.CancellationToken);

        package.IncludedFiles.ShouldBeEmpty();
        ProjectAiContextPackageExclusion exclusion = package.ExcludedFiles.ShouldHaveSingleItem();
        exclusion.ReasonCode.ShouldBe("redacted");
        exclusion.ReferenceToken.ShouldNotContain("folder-secret", Case.Sensitive);
        exclusion.ReferenceToken.ShouldNotContain("file-secret", Case.Sensitive);
        exclusion.ReferenceToken.ShouldNotContain("provider-secret", Case.Sensitive);
    }

    [Fact]
    public async Task EmptyAuthorizedProjectShouldReturnWellFormedEmptyPackage()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [], CorrelationId),
            TestContext.Current.CancellationToken);

        package.IncludedFiles.ShouldBeEmpty();
        package.ExcludedFiles.ShouldBeEmpty();
        package.TenantId.ShouldStartWith("tenant:");
        package.TenantId.ShouldNotContain(Tenant, Case.Sensitive);
        package.ProjectId.ShouldBe(Project);
        package.PolicySnapshotId.ShouldBe("unavailable");
        package.RedactionDecision.ShouldBe("metadata_only");
        package.RetentionClass.ShouldBe("collaboration_input");
        package.ProviderReuseSetting.ShouldBe("disabled");
        package.CorrelationId.ShouldBe(CorrelationId);
    }

    [Fact]
    public async Task PolicyAuthorizationAndReadinessGatesShouldUseStableExclusionReasons()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView policyDenied = Attachment("attachment-policy-denied", 4) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = [],
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-policy-denied",
        };
        ProjectConversationItemView unauthorized = Attachment("attachment-unauthorized", 5) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context"],
            AttachmentFolderId = "folder-002",
            AttachmentFileId = null,
            SourceProviderAttachmentId = "provider-attachment-unauthorized",
        };
        ProjectConversationItemView notYetEligible = Attachment("attachment-not-yet-eligible", 6) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Pending,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context"],
            AttachmentFolderId = "folder-003",
            AttachmentFileId = "file-003",
            SourceProviderAttachmentId = "provider-attachment-not-yet-eligible",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(
                Tenant,
                Project,
                [Association("association-001", 3), policyDenied, unauthorized, notYetEligible],
                CorrelationId),
            TestContext.Current.CancellationToken);

        package.IncludedFiles.ShouldBeEmpty();
        package.ExcludedFiles.Select(static exclusion => exclusion.ReasonCode).ShouldBe(
            ["not-yet-eligible", "policy-denied", "unauthorized"],
            ignoreOrder: true);
        ProjectAiContextPackageExclusion unauthorizedExclusion = package.ExcludedFiles
            .Single(static exclusion => exclusion.ReasonCode == "unauthorized");
        unauthorizedExclusion.SourceEvidenceReference.ShouldBeNull();
        unauthorizedExclusion.ReferenceToken.ShouldNotContain("folder-002", Case.Sensitive);
        unauthorizedExclusion.ReferenceToken.ShouldNotContain("provider-attachment-unauthorized", Case.Sensitive);
    }

    [Fact]
    public async Task MaterializedPackagesShouldCarryNfr9FieldsAndExcludedFileReasons()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView pending = Attachment("attachment-pending", 4) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Pending,
            AttachmentAiContextEligibility = "pending",
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [Association("association-001", 3), pending], CorrelationId),
            TestContext.Current.CancellationToken);

        package.TenantId.ShouldNotBeNullOrWhiteSpace();
        package.ProjectId.ShouldBe(Project);
        package.PolicySnapshotId.ShouldNotBeNullOrWhiteSpace();
        package.RedactionDecision.ShouldBe("metadata_only");
        package.RetentionClass.ShouldBe("collaboration_input");
        package.ProviderReuseSetting.ShouldBe("disabled");
        package.PackageId.ShouldNotBeNullOrWhiteSpace();
        package.PackageVersion.ShouldBe("v1");
        package.SchemaVersion.ShouldBe(ProjectAiContextPackage.SchemaVersionValue);
        package.SourceVersion.ShouldBeGreaterThan(0);
        package.CorrelationId.ShouldBe(CorrelationId);
        package.SourceEvidenceReferences.ShouldContain("conversation-001");
        package.ExcludedFiles.ShouldHaveSingleItem().ReasonCode.ShouldBe("pending-scan");
    }

    [Fact]
    public async Task AssemblyShouldUseLatestPolicyAndRetentionMetadata()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView stalePolicy = Association("association-001", 3) with
        {
            ItemId = "z-policy-carrier",
            PolicySnapshotVersion = "policy-stale",
            RetentionClass = "stale_retention",
        };
        ProjectConversationItemView latestPolicy = Association("association-002", 9) with
        {
            ItemId = "a-policy-carrier",
            PolicySnapshotVersion = "policy-current",
            RetentionClass = "collaboration_input",
        };
        ProjectConversationItemView attachment = Attachment("attachment-001", 8) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context"],
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [stalePolicy, latestPolicy, attachment], CorrelationId),
            TestContext.Current.CancellationToken);

        package.PolicySnapshotId.ShouldBe("policy-current");
        package.RetentionClass.ShouldBe("collaboration_input");
        package.IncludedFiles.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AttachmentWithoutSourceEvidenceShouldStayExcluded()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView attachment = Attachment("attachment-without-evidence", 8) with
        {
            IntakeId = string.Empty,
            SourceConversationId = string.Empty,
            SourceThreadId = null,
            EvidenceReferenceSummary = [],
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context"],
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };

        ProjectAiContextPackage package = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [Association("association-001", 3), attachment], CorrelationId),
            TestContext.Current.CancellationToken);

        package.IncludedFiles.ShouldBeEmpty();
        ProjectAiContextPackageExclusion exclusion = package.ExcludedFiles.ShouldHaveSingleItem();
        exclusion.ReasonCode.ShouldBe("not-yet-eligible");
        exclusion.SourceEvidenceReference.ShouldBeNull();
    }

    [Fact]
    public void DefaultAssemblerShouldRemainPureAndDependencyFree()
    {
        typeof(DefaultProjectAiContextPackageAssembler)
            .GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .ShouldHaveSingleItem()
            .GetParameters()
            .ShouldBeEmpty();
        typeof(DefaultProjectAiContextPackageAssembler)
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task AssemblyShouldBeTenantScopedIdempotentAndLastWriterWins()
    {
        DefaultProjectAiContextPackageAssembler assembler = new();
        ProjectConversationItemView staleClean = Attachment("attachment-001", 7) with
        {
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentAiContextEligibility = "eligible",
            AttachmentAllowedActions = ["add-to-ai-context"],
            AttachmentFolderId = "folder-001",
            AttachmentFileId = "file-001",
            SourceProviderAttachmentId = "provider-attachment-001",
        };
        ProjectConversationItemView newerUnsafe = staleClean with
        {
            SourceVersion = 9,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Unsafe,
            AttachmentAiContextEligibility = "not-eligible",
            AttachmentAllowedActions = [],
            AttachmentFolderId = null,
            AttachmentFileId = null,
        };
        ProjectConversationItemView foreignTenant = staleClean with
        {
            TenantId = OtherTenant,
            SourceVersion = 10,
            AttachmentFileId = "foreign-file",
        };

        ProjectAiContextPackage first = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [Association("association-001", 3), staleClean, newerUnsafe, foreignTenant], CorrelationId),
            TestContext.Current.CancellationToken);
        ProjectAiContextPackage second = await assembler.AssembleAsync(
            new ProjectAiContextPackageAssemblyRequest(Tenant, Project, [foreignTenant, newerUnsafe, Association("association-001", 3), staleClean, newerUnsafe], CorrelationId),
            TestContext.Current.CancellationToken);

        first.IncludedFiles.ShouldBeEmpty();
        first.ExcludedFiles.ShouldHaveSingleItem().ReasonCode.ShouldBe("unsafe");
        second.PackageId.ShouldBe(first.PackageId);
        second.PolicySnapshotId.ShouldBe(first.PolicySnapshotId);
        second.SourceVersion.ShouldBe(first.SourceVersion);
        second.IncludedFiles.Select(static file => file.ReferenceToken).ShouldBe(first.IncludedFiles.Select(static file => file.ReferenceToken), ignoreOrder: false);
        second.ExcludedFiles.Select(static file => file.ReasonCode).ShouldBe(first.ExcludedFiles.Select(static file => file.ReasonCode), ignoreOrder: false);
    }

    private static ProjectConversationItemView Association(string itemId, long sourceVersion)
        => Item(itemId, ProjectConversationItemKind.EmailDerived, sourceVersion) with
        {
            PolicySnapshotVersion = "association-thresholds.m0.default.v1",
            EvidenceReferenceSummary = ["conversation-001"],
        };

    private static ProjectConversationItemView Attachment(string itemId, long sourceVersion)
        => Item(itemId, ProjectConversationItemKind.Attachment, sourceVersion) with
        {
            ActorKind = ProjectConversationActorKind.MailboxAttachment,
            ActorLabel = "Mailbox attachment",
            AttachmentCaptureStatus = ProjectConversationAttachmentStatus.Captured,
            AttachmentStorageStatus = ProjectConversationAttachmentStatus.Pending,
            AttachmentScanStatus = ProjectConversationAttachmentStatus.Pending,
            AttachmentAiContextEligibility = "pending",
            AttachmentAllowedActions = [],
            AttachmentRedactionState = "metadata_only",
        };

    private static ProjectConversationItemView Item(string itemId, ProjectConversationItemKind kind, long sourceVersion)
        => new(
            Tenant,
            Project,
            null,
            itemId,
            "intake-001",
            kind,
            ProjectConversationActorKind.Mailbox,
            "Mailbox event",
            OccurredAt,
            LifecycleState.Associated,
            AssociationThresholdBand.Auto,
            0.91,
            "association-001",
            "controlled-mailbox-001",
            "provider-message-001",
            null,
            "conversation-001",
            "thread-001",
            null,
            null,
            null,
            null,
            null,
            "m365-mailbox-intake",
            "metadata_only",
            "collaboration_input",
            ProjectConversationItemView.CurrentSchemaVersion,
            sourceVersion,
            CorrelationId);
}
