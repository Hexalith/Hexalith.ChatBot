using System.Security.Claims;

using Hexalith.ChatBot.Client.Generated;
using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Server.Gateway;
using Hexalith.ChatBot.Server.Gateway.Stages;

using Shouldly;

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

    private static ChatBotCommandSubmission Submission(object? command = null)
    {
        command ??= new Hexalith.ChatBot.Contracts.Commands.SetAssociationConfidenceThresholds("association", 0.9, 0.6, "policy-v1", null, null);
        return new(
            new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "actor-alpha")], "test")),
            new CommandSubmissionRequest
            {
                CommandId = "01ARZ3NDEKTSV4RRFFQ69G5FAY",
                CommandType = command.GetType().Name,
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
