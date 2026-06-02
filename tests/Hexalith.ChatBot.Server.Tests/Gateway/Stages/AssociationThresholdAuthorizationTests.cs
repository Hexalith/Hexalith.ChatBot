using System.Security.Claims;
using System.Text.Json;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

using ContractMailboxConfigurationChangeSet = Hexalith.ChatBot.Contracts.Commands.MailboxConfigurationChangeSet;
using ContractMailboxPermissionStatus = Hexalith.ChatBot.Contracts.Commands.MailboxPermissionStatus;
using ContractMailboxProviderConnectionMetadata = Hexalith.ChatBot.Contracts.Commands.MailboxProviderConnectionMetadata;
using ContractMailboxRoutingRule = Hexalith.ChatBot.Contracts.Commands.MailboxRoutingRule;
using ContractMonitoredMailboxPattern = Hexalith.ChatBot.Contracts.Commands.MonitoredMailboxPattern;
using ContractRecordMailboxProviderConnection = Hexalith.ChatBot.Contracts.Commands.RecordMailboxProviderConnection;
using ContractSubmitMailboxConfigurationChange = Hexalith.ChatBot.Contracts.Commands.SubmitMailboxConfigurationChange;
using ContractMailboxPermissionFreshnessState = Hexalith.ChatBot.Contracts.Enums.MailboxPermissionFreshnessState;
using ContractMailboxProviderKind = Hexalith.ChatBot.Contracts.Enums.MailboxProviderKind;
using ContractMailboxRoutingRuleKind = Hexalith.ChatBot.Contracts.Enums.MailboxRoutingRuleKind;
using ContractRequestComplianceEscalation = Hexalith.ChatBot.Contracts.Commands.RequestComplianceEscalation;
using ContractRequestComplianceInvestigation = Hexalith.ChatBot.Contracts.Commands.RequestComplianceInvestigation;
using ContractRetentionConfigurationChangeSet = Hexalith.ChatBot.Contracts.Commands.RetentionConfigurationChangeSet;
using ContractRetentionWindow = Hexalith.ChatBot.Contracts.Commands.RetentionWindow;
using ContractSubmitRetentionConfigurationChange = Hexalith.ChatBot.Contracts.Commands.SubmitRetentionConfigurationChange;

namespace Hexalith.ChatBot.Server.Tests.Gateway.Stages;

public sealed class AssociationThresholdAuthorizationTests
{
    [Fact]
    public async Task ThresholdMutationShouldRequireTenantAdminHumanActor()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult serviceDenied = await stage.AuthorizeAsync(
            Submission(),
            Actor("service", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        serviceDenied.IsAllowed.ShouldBeFalse();
        serviceDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);

        ChatBotAuthorizationResult adminHuman = await stage.AuthorizeAsync(
            Submission(),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        adminHuman.IsAllowed.ShouldBeTrue();

        ChatBotAuthorizationResult policyAdminHuman = await stage.AuthorizeAsync(
            Submission(),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        policyAdminHuman.IsAllowed.ShouldBeTrue();

        ChatBotAuthorizationResult operationsAdminDenied = await stage.AuthorizeAsync(
            Submission(),
            Actor("human", "operations-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        operationsAdminDenied.IsAllowed.ShouldBeFalse();
        operationsAdminDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
    }

    [Fact]
    public async Task AdminAssignmentShouldRequireHumanTenantAdmin()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult tenantAdminHuman = await stage.AuthorizeAsync(
            Submission(AdminAssignment()),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        tenantAdminHuman.IsAllowed.ShouldBeTrue();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("human", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(AdminAssignment()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }
    }

    [Fact]
    public async Task AdminQueueOperationShouldRequireHumanOperateScope()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "operations-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(AdminQueueOperationCommand()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "mailbox-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(AdminQueueOperationCommand()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task TenantPolicyChangeShouldRequireHumanPolicyScopeAndValidClosedSchema()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "policy-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(PolicyChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "operations-admin"),
                     Actor("human", "mailbox-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(PolicyChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }

        ChatBotAuthorizationResult invalid = await stage.AuthorizeAsync(
            Submission(PolicyChange() with
            {
                ChangedKnobIds = ["custom.knob"],
                ChangeSet = new Hexalith.ChatBot.Contracts.Commands.TenantPolicyChangeSet([new("custom.knob", StringValue: "unsafe")]),
            }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        invalid.IsAllowed.ShouldBeFalse();

        ChatBotAuthorizationResult duplicateKnob = await stage.AuthorizeAsync(
            Submission(PolicyChange() with
            {
                ChangedKnobIds = [TenantPolicyKnobIds.AssociationTHigh],
                ChangeSet = new Hexalith.ChatBot.Contracts.Commands.TenantPolicyChangeSet(
                [
                    new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.92),
                    new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.93),
                ]),
            }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        duplicateKnob.IsAllowed.ShouldBeFalse();

        ChatBotAuthorizationResult unknownSchema = await stage.AuthorizeAsync(
            Submission(PolicyChange() with { SchemaVersion = "tenant-policy-schema.custom.v1" }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        unknownSchema.IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task TenantPolicyApprovalShouldRequireDistinctRequesterAndApproverRefs()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(PolicyApproval()),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        ChatBotAuthorizationResult selfApproval = await stage.AuthorizeAsync(
            Submission(PolicyApproval() with { ApproverRef = "admin-requester" }),
            Actor("human", "policy-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        selfApproval.IsAllowed.ShouldBeFalse();

        ChatBotAuthorizationResult unsafeKnob = await stage.AuthorizeAsync(
            Submission(PolicyApproval() with { ChangedKnobIds = ["policy.json"] }),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        unsafeKnob.IsAllowed.ShouldBeFalse();

        ChatBotAuthorizationResult unknownSchema = await stage.AuthorizeAsync(
            Submission(PolicyApproval() with { SchemaVersion = "tenant-policy-schema.custom.v1" }),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        unknownSchema.IsAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task AdminAssignmentShouldRequireAuditObligationFields()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (AssignTenantAdminRole command in new[]
                 {
                     AdminAssignment() with { AssignmentId = "" },
                     AdminAssignment() with { TargetActorId = "" },
                     AdminAssignment() with { ReasonCode = "" },
                     AdminAssignment() with { ReasonCode = "secret-reason" },
                     AdminAssignment() with { PolicySnapshotId = "" },
                     AdminAssignment() with { PolicySnapshotId = "policy.json" },
                     AdminAssignment() with { SourceVersion = -1 },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(command),
                Actor("human", "tenant-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.ThresholdPolicyUnauthorized);
        }
    }

    [Fact]
    public async Task AdminQueueOperationShouldRequireAffectedItemsAndAuditObligationFields()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ExecuteAdminQueueOperation command in new[]
                 {
                     AdminQueueOperationCommand() with { OperationId = "" },
                     AdminQueueOperationCommand() with { QueueRef = "" },
                     AdminQueueOperationCommand() with { QueueRef = "queue:secret" },
                     AdminQueueOperationCommand() with { ItemRefs = [], ItemCount = 0 },
                     AdminQueueOperationCommand() with { ItemRefs = ["file-secret.txt"], ItemCount = 1 },
                     AdminQueueOperationCommand() with { ItemRefs = ["item:001"], ItemCount = 2 },
                     AdminQueueOperationCommand() with { ReasonCode = "" },
                     AdminQueueOperationCommand() with { ReasonCode = "free-form-reason" },
                     AdminQueueOperationCommand() with { PolicySnapshotId = "" },
                     AdminQueueOperationCommand() with { PolicySnapshotId = "policy.json" },
                     AdminQueueOperationCommand() with { SourceVersion = -1 },
                     AdminQueueOperationCommand() with { RedactionState = "" },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(command),
                Actor("human", "operations-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    [Fact]
    public async Task MailboxConfigurationChangeShouldRequireHumanMailboxScopeAndValidMetadataOnlyPayload()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "mailbox-admin"),
                 })
        {
            ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
                Submission(MailboxChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            allowed.IsAllowed.ShouldBeTrue();
        }

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "operations-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "compliance-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("service", "mailbox-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(MailboxChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        foreach (ContractSubmitMailboxConfigurationChange invalid in new[]
                 {
                     MailboxChange() with { SourceVersion = -1 },
                     MailboxChange() with { SchemaVersion = "mailbox-config-schema.custom" },
                     MailboxChange() with { ReasonCode = "unsafe reason" },
                     MailboxChange() with
                     {
                         ChangeSet = MailboxChangeSet() with
                         {
                            ProviderConnections = [Provider() with { ProviderKind = ContractMailboxProviderKind.Unknown }],
                         },
                     },
                     MailboxChange() with
                     {
                         ChangeSet = MailboxChangeSet() with
                         {
                             MonitoredPatterns = [Pattern() with { MailboxId = "tenant-alpha:secret/mailbox" }],
                         },
                     },
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(invalid),
                Actor("human", "mailbox-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        ChatBotAuthorizationResult missingRoutingKindDenied = await stage.AuthorizeAsync(
            Submission(MailboxChangeJsonMissingRoutingKind(), "SubmitMailboxConfigurationChange"),
            Actor("human", "mailbox-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        missingRoutingKindDenied.IsAllowed.ShouldBeFalse();
        missingRoutingKindDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
    }

    [Fact]
    public async Task MailboxProviderConnectionShouldRequireHumanMailboxScope()
    {
        ParticipantAuthorizationStage stage = new();

        ChatBotAuthorizationResult allowed = await stage.AuthorizeAsync(
            Submission(ProviderConnectionCommand()),
            Actor("human", "mailbox-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();

        ChatBotAuthorizationResult serviceDenied = await stage.AuthorizeAsync(
            Submission(ProviderConnectionCommand()),
            Actor("service", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        serviceDenied.IsAllowed.ShouldBeFalse();
        serviceDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);

        ChatBotAuthorizationResult invalidSecret = await stage.AuthorizeAsync(
            Submission(ProviderConnectionCommand() with { CredentialFingerprint = "raw-secret-token" }),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        invalidSecret.IsAllowed.ShouldBeFalse();

        ChatBotAuthorizationResult missingFreshness = await stage.AuthorizeAsync(
            Submission(ProviderConnectionJsonMissingFreshness(), "RecordMailboxProviderConnection"),
            Actor("human", "tenant-admin"),
            new ChatBotTenantBinding("tenant-alpha"),
            TestContext.Current.CancellationToken);
        missingFreshness.IsAllowed.ShouldBeFalse();
        missingFreshness.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
    }

    [Fact]
    public async Task ComplianceCommandsShouldRequireHumanComplianceScopeAndValidMetadataOnlyPayloads()
    {
        ParticipantAuthorizationStage stage = new();

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "tenant-admin"),
                     Actor("human", "compliance-admin"),
                 })
        {
            ChatBotAuthorizationResult investigationAllowed = await stage.AuthorizeAsync(
                Submission(ComplianceInvestigation()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            investigationAllowed.IsAllowed.ShouldBeTrue();

            ChatBotAuthorizationResult escalationAllowed = await stage.AuthorizeAsync(
                Submission(ComplianceEscalation()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            escalationAllowed.IsAllowed.ShouldBeTrue();

            ChatBotAuthorizationResult retentionAllowed = await stage.AuthorizeAsync(
                Submission(RetentionChange()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            retentionAllowed.IsAllowed.ShouldBeTrue();
        }

        foreach (ChatBotAuthenticatedActor actor in new[]
                 {
                     Actor("human", "operations-admin"),
                     Actor("human", "policy-admin"),
                     Actor("human", "mailbox-admin"),
                     Actor("service", "tenant-admin"),
                     Actor("ai", "tenant-admin"),
                     Actor("service", "compliance-admin"),
                 })
        {
            ChatBotAuthorizationResult denied = await stage.AuthorizeAsync(
                Submission(ComplianceInvestigation()),
                actor,
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            denied.IsAllowed.ShouldBeFalse();
            denied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }

        foreach (object invalid in new object[]
                 {
                     ComplianceInvestigation() with { InvestigationId = "" },
                     ComplianceInvestigation() with { FilterRefs = ["raw secret"] },
                     ComplianceInvestigation() with { SourceVersion = -1 },
                     ComplianceInvestigation() with { RedactionState = Hexalith.ChatBot.Contracts.Enums.ComplianceAuditRedactionState.Unknown },
                     ComplianceEscalation() with { EscalationTargetRef = "project secret" },
                     ComplianceEscalation() with { SourceVersion = -1 },
                     ComplianceEscalation() with { EscalationStatus = Hexalith.ChatBot.Contracts.Enums.ComplianceEscalationStatus.Unknown },
                     RetentionChange() with { SourceVersion = -1 },
                     RetentionChange() with { OldRetentionSnapshotFingerprint = "raw-secret" },
                     RetentionChange() with
                     {
                         ChangeSet = new ContractRetentionConfigurationChangeSet(
                         [
                             new ContractRetentionWindow(ComplianceRetentionClassIds.AuditRecords, "audit-records-window", 30),
                         ]),
                     },
                 })
        {
            ChatBotAuthorizationResult invalidDenied = await stage.AuthorizeAsync(
                Submission(invalid, invalid.GetType().Name),
                Actor("human", "compliance-admin"),
                new ChatBotTenantBinding("tenant-alpha"),
                TestContext.Current.CancellationToken);
            invalidDenied.IsAllowed.ShouldBeFalse();
            invalidDenied.ReasonCode.ShouldBe(ChatBotAuthorizationReasonCodes.AuthorizationDenied);
        }
    }

    private static ChatBotCommandSubmission Submission(object? command = null, string? commandType = null)
    {
        command ??= new Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds("association", 0.9, 0.6, "policy-v1", null, null);
        return new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = commandType ?? command.GetType().Name,
                Command = command,
                RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
            },
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            null,
            ChatBotSurfaceOrigin.Ui);
    }

    private static AssignTenantAdminRole AdminAssignment()
        => new(
            "assignment-001",
            "actor-beta",
            AdminRole.OperationsAdmin,
            "security-owner-request",
            "policy-snapshot:admin:v1",
            1);

    private static ExecuteAdminQueueOperation AdminQueueOperationCommand()
        => new(
            "operation-001",
            Hexalith.ChatBot.Contracts.Enums.AdminQueueOperation.Retry,
            AdminScope.Operate,
            "queue:failure",
            ["item:001"],
            1,
            "dependency-degraded",
            "policy-snapshot:admin:v1",
            7,
            "metadata_only");

    private static Hexalith.ChatBot.Contracts.Commands.SubmitTenantPolicyChange PolicyChange()
        => new(
            "policy-change-001",
            "policy-snapshot-current",
            "policy-snapshot-proposed",
            4,
            [TenantPolicyKnobIds.AssociationTHigh, TenantPolicyKnobIds.AssociationTLow],
            new Hexalith.ChatBot.Contracts.Commands.TenantPolicyChangeSet(
            [
                new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.92),
                new(TenantPolicyKnobIds.AssociationTLow, NumberValue: 0.61),
            ]),
            "security-owner-request",
            "admin-requester",
            TenantPolicySchemaVersions.M0,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "old-fingerprint-001",
            "new-fingerprint-001");

    private static Hexalith.ChatBot.Contracts.Commands.ApproveTenantPolicyChange PolicyApproval()
        => new(
            "policy-change-001",
            "policy-snapshot-proposed",
            "policy-snapshot-active",
            5,
            [TenantPolicyKnobIds.AssociationTHigh],
            "second-admin-approval",
            "admin-requester",
            "admin-approver",
            TenantPolicySchemaVersions.M0,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

    private static ContractSubmitMailboxConfigurationChange MailboxChange()
        => new(
            "mailbox-change-001",
            "mailbox-config-current",
            "mailbox-config-proposed",
            4,
            MailboxChangeSet(),
            "mailbox-admin-update",
            "admin-requester",
            MailboxConfigurationSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:oldfingerprint001",
            "sha256:newfingerprint001");

    private static ContractRecordMailboxProviderConnection ProviderConnectionCommand()
        => new(
            "provider-connection-change-001",
            "provider-connection-001",
            ContractMailboxProviderKind.MicrosoftGraph,
            "sha256:credentialfingerprint001",
            "graph-permission-evidence-001",
            ContractMailboxPermissionFreshnessState.Fresh,
            "mailbox-provider-refresh",
            "admin-requester",
            4,
            MailboxConfigurationSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1");

    private static ContractRequestComplianceInvestigation ComplianceInvestigation()
        => new(
            "investigation-001",
            "audit-query-001",
            ["audit-filter-001"],
            "compliance-investigation",
            "admin-requester",
            4,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            Hexalith.ChatBot.Contracts.Enums.ComplianceAuditRedactionState.MetadataOnly,
            Hexalith.ChatBot.Contracts.Enums.ComplianceEscalationStatus.NotRequested,
            ComplianceAdministrationSchemaVersions.V1);

    private static ContractRequestComplianceEscalation ComplianceEscalation()
        => new(
            "escalation-001",
            "investigation-001",
            "audit-record-001",
            "compliance-access-request",
            "admin-requester",
            "project-owner-group",
            4,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            Hexalith.ChatBot.Contracts.Enums.ComplianceAuditRedactionState.EscalationRequired,
            Hexalith.ChatBot.Contracts.Enums.ComplianceEscalationStatus.Requested,
            ComplianceAdministrationSchemaVersions.V1);

    private static ContractSubmitRetentionConfigurationChange RetentionChange()
        => new(
            "retention-change-001",
            "retention-snapshot-current",
            "retention-snapshot-proposed",
            4,
            new ContractRetentionConfigurationChangeSet(
            [
                new ContractRetentionWindow(ComplianceRetentionClassIds.SourceEmailMetadata, "source-email-metadata-window", 365),
                new ContractRetentionWindow(ComplianceRetentionClassIds.AuditRecords, "audit-records-window", 2555),
            ]),
            "compliance-retention-update",
            "admin-requester",
            ComplianceAdministrationSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:oldretentionfingerprint001",
            "sha256:newretentionfingerprint001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

    private static JsonElement MailboxChangeJsonMissingRoutingKind()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "configurationChangeId": "mailbox-change-001",
              "sourceConfigurationSnapshotId": "mailbox-config-current",
              "proposedConfigurationSnapshotId": "mailbox-config-proposed",
              "sourceVersion": 4,
              "changeSet": {
                "monitoredPatterns": [
                  {
                    "mailboxId": "controlled-mailbox-001",
                    "sourceContext": "graph-message-v1",
                    "providerConnectionRef": "provider-connection-001",
                    "isEnabled": true,
                    "patternRef": "mailbox-pattern-001"
                  }
                ],
                "routingRules": [
                  {
                    "routingRuleId": "routing-rule-001",
                    "sourceContext": "graph-message-v1",
                    "targetRef": "route-project-intake",
                    "priority": 10,
                    "reasonCode": "mailbox-routing"
                  }
                ],
                "providerConnections": [
                  {
                    "providerConnectionRef": "provider-connection-001",
                    "providerKind": "microsoft-graph",
                    "credentialFingerprint": "sha256:credentialfingerprint001",
                    "permissionEvidenceRef": "graph-permission-evidence-001",
                    "freshness": "fresh",
                    "lastCheckedAt": "2026-06-02T04:00:00+00:00"
                  }
                ],
                "permissionStatuses": [
                  {
                    "permissionStatusRef": "permission-status-001",
                    "providerConnectionRef": "provider-connection-001",
                    "permission": "Mail.Read",
                    "freshness": "fresh",
                    "permissionEvidenceRef": "graph-permission-evidence-001",
                    "lastCheckedAt": "2026-06-02T04:00:00+00:00",
                    "reasonCode": "permission-fresh"
                  }
                ]
              },
              "reasonCode": "mailbox-admin-update",
              "requesterRef": "admin-requester",
              "schemaVersion": "mailbox-config-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "oldConfigurationFingerprint": "sha256:oldfingerprint001",
              "newConfigurationFingerprint": "sha256:newfingerprint001"
            }
            """);
        return document.RootElement.Clone();
    }

    private static JsonElement ProviderConnectionJsonMissingFreshness()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            {
              "providerConnectionChangeId": "provider-connection-change-001",
              "providerConnectionRef": "provider-connection-001",
              "providerKind": "microsoft-graph",
              "credentialFingerprint": "sha256:credentialfingerprint001",
              "permissionEvidenceRef": "graph-permission-evidence-001",
              "reasonCode": "mailbox-provider-refresh",
              "requesterRef": "admin-requester",
              "sourceVersion": 4,
              "schemaVersion": "mailbox-config-schema.v1",
              "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAW",
              "policySnapshotId": "policy-snapshot-admin-v1"
            }
            """);
        return document.RootElement.Clone();
    }

    private static ContractMailboxConfigurationChangeSet MailboxChangeSet()
        => new(
            [Pattern()],
            [new ContractMailboxRoutingRule("routing-rule-001", ContractMailboxRoutingRuleKind.SourceContext, "graph-message-v1", "route-project-intake", 10, "mailbox-routing")],
            [Provider()],
            [new ContractMailboxPermissionStatus("permission-status-001", "provider-connection-001", "Mail.Read", ContractMailboxPermissionFreshnessState.Fresh, "graph-permission-evidence-001", new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero), "permission-fresh")]);

    private static ContractMonitoredMailboxPattern Pattern()
        => new("controlled-mailbox-001", "graph-message-v1", "provider-connection-001", true, "mailbox-pattern-001");

    private static ContractMailboxProviderConnectionMetadata Provider()
        => new(
            "provider-connection-001",
            ContractMailboxProviderKind.MicrosoftGraph,
            "sha256:credentialfingerprint001",
            "graph-permission-evidence-001",
            ContractMailboxPermissionFreshnessState.Fresh,
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

    private static ChatBotAuthenticatedActor Actor(string actorType, string role)
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [
                new Claim("sub", "actor-alpha"),
                new Claim(ParticipantAuthorizationStage.ActorTypeClaim, actorType),
                new Claim(ParticipantAuthorizationStage.TenantRoleClaim, role),
            ],
            "test"));
        return new ChatBotAuthenticatedActor("actor-alpha", principal);
    }
}
