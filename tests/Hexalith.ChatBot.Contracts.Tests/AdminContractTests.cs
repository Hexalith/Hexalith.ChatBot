using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Identities;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class AdminContractTests
{
    [Theory]
    [InlineData(AdminRole.TenantAdmin, "tenant-admin")]
    [InlineData(AdminRole.MailboxAdmin, "mailbox-admin")]
    [InlineData(AdminRole.PolicyAdmin, "policy-admin")]
    [InlineData(AdminRole.ComplianceAdmin, "compliance-admin")]
    [InlineData(AdminRole.OperationsAdmin, "operations-admin")]
    public static void AdminRoleWireTokensShouldRoundTrip(AdminRole role, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AdminRoles.ToWireValue(role).ShouldBe(token);
        AdminRoles.TryFromWireValue(token, out AdminRole parsed).ShouldBeTrue();
        parsed.ShouldBe(role);
        AdminRoles.TryFromWireValue($" {token.ToUpperInvariant()} ", out parsed).ShouldBeTrue();
        parsed.ShouldBe(role);
    }

    [Theory]
    [InlineData(AdminScope.SeeOnly, "see-only")]
    [InlineData(AdminScope.Operate, "operate")]
    [InlineData(AdminScope.Policy, "policy")]
    [InlineData(AdminScope.Mailbox, "mailbox")]
    [InlineData(AdminScope.Compliance, "compliance")]
    [InlineData(AdminScope.AuditObligation, "audit-obligation")]
    public static void AdminScopeWireTokensShouldRoundTrip(AdminScope scope, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AdminScopes.ToWireValue(scope).ShouldBe(token);
        AdminScopes.TryFromWireValue(token, out AdminScope parsed).ShouldBeTrue();
        parsed.ShouldBe(scope);
    }

    [Fact]
    public static void TenantAdminShouldBeUnionAndFinerRolesShouldBeProperSubsets()
    {
        IReadOnlySet<AdminScope> tenantAdmin = AdminScopes.ScopesForRole(AdminRole.TenantAdmin);
        tenantAdmin.SetEquals(AdminScopes.All).ShouldBeTrue();

        foreach (AdminRole role in AdminRoles.All.Where(static role => role != AdminRole.TenantAdmin))
        {
            IReadOnlySet<AdminScope> finerRole = AdminScopes.ScopesForRole(role);
            finerRole.ShouldNotBeEmpty();
            finerRole.IsProperSubsetOf(tenantAdmin).ShouldBeTrue(role.ToString());
            finerRole.ShouldContain(AdminScope.SeeOnly);
            finerRole.ShouldContain(AdminScope.AuditObligation);
        }

        AdminScopes.ScopesForRole(AdminRole.OperationsAdmin).ShouldContain(AdminScope.Operate);
        AdminScopes.ScopesForRole(AdminRole.MailboxAdmin).ShouldNotContain(AdminScope.Operate);
        AdminScopes.ScopesForRole(AdminRole.PolicyAdmin).ShouldNotContain(AdminScope.Operate);
        AdminScopes.ScopesForRole(AdminRole.ComplianceAdmin).ShouldNotContain(AdminScope.Operate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tenant-owner")]
    [InlineData("tenant-admin,policy-admin")]
    public static void UnknownAdminTokensShouldDenyByDefault(string? token)
        => AdminRoles.TryFromWireValue(token, out _).ShouldBeFalse();

    [Fact]
    public static void AdminOperationContractsShouldSerializeMetadataOnly()
    {
        AdminOperationReference auditRef = new(
            "admin-alpha",
            "human",
            AdminScope.Operate,
            "queue:failure",
            ["item:001"],
            1,
            "dependency-degraded",
            "policy-snapshot:admin:v1",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            12,
            "metadata_only");
        AdminQueueSummary summary = new(
            "queue:failure",
            ChatBotHealthStatus.Degraded,
            [new AdminQueueSummaryBucket("retryable", "operations", 1, 60)],
            [new AdminQueueSummaryItemRef("item:001", "retryable", "operations", ["insufficient-authority"])],
            auditRef,
            "chatbot.admin-queue-summary.v1",
            "correlation-alpha");
        ExecuteAdminQueueOperation command = new(
            "operation-001",
            AdminQueueOperation.Retry,
            AdminScope.Operate,
            "queue:failure",
            ["item:001"],
            1,
            "dependency-degraded",
            "policy-snapshot:admin:v1",
            12,
            "metadata_only");
        AssignTenantAdminRole assignment = new(
            "assignment-001",
            "actor-beta",
            AdminRole.TenantAdmin,
            "security-owner-request",
            "policy-snapshot:admin:v1",
            3);

        string json = JsonSerializer.Serialize(new { assignment, command, summary }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("tenant-admin", Case.Insensitive);
        json.ShouldContain("operate");
        json.ShouldContain("retry");
        json.ShouldNotContain("projectName", Case.Insensitive);
        json.ShouldNotContain("evidenceContent", Case.Insensitive);
        json.ShouldNotContain("fileMetadata", Case.Insensitive);
        json.ShouldNotContain("auditReason", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("bearer", Case.Insensitive);
    }

    [Fact]
    public static void TenantPolicySchemaShouldDeclareClosedM0AndM1PolicyKnobs()
    {
        TenantPolicySchema.Definitions.Select(static definition => definition.KnobId).ShouldBe(
            [
                TenantPolicyKnobIds.AdminPermissionScopes,
                TenantPolicyKnobIds.AiActionLowRiskAllowed,
                TenantPolicyKnobIds.AllowlistVersionPin,
                TenantPolicyKnobIds.ApprovalRouting,
                TenantPolicyKnobIds.AssociationTHigh,
                TenantPolicyKnobIds.AssociationTLow,
                TenantPolicyKnobIds.AttachmentsUnsafeHandling,
                TenantPolicyKnobIds.ClassifierExplanationLayerEnabled,
                TenantPolicyKnobIds.InboundAuthenticityStrictness,
                TenantPolicyKnobIds.MailboxRoutingRules,
            ],
            ignoreOrder: false);

        TenantPolicySchema.DefaultM0Values.ShouldContain(static value => value.KnobId == TenantPolicyKnobIds.AssociationTHigh && value.NumberValue == 0.90);
        TenantPolicySchema.DefaultM0Values.ShouldContain(static value => value.KnobId == TenantPolicyKnobIds.AssociationTLow && value.NumberValue == 0.60);
        TenantPolicyValue aiDefault = TenantPolicySchema.DefaultM0Values.Single(static value => value.KnobId == TenantPolicyKnobIds.AiActionLowRiskAllowed);
        aiDefault.AiActionLowRiskAllowed.ShouldNotBeNull().Values.ShouldAllBe(static allowed => allowed == false);
        aiDefault.AiActionLowRiskAllowed.Keys.ShouldBe(TenantPolicySchema.RequiredAiActionClasses, ignoreOrder: false);
    }

    [Fact]
    public static void TenantPolicySchemaShouldRejectUnknownWrongRangeAndIncompleteAiMaps()
    {
        TenantPolicySchema.Validate(new TenantPolicyChangeSet(
            [
                new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.95),
                new(TenantPolicyKnobIds.AssociationTLow, NumberValue: 0.60),
                new(TenantPolicyKnobIds.AiActionLowRiskAllowed, AiActionLowRiskAllowed: TenantPolicySchema.RequiredAiActionClasses.ToDictionary(static value => value, static _ => false)),
            ])).IsValid.ShouldBeTrue();

        TenantPolicySchema.Validate(new TenantPolicyChangeSet([new("custom.knob", StringValue: "unsafe")]))
            .Errors.ShouldContain("unknown_knob:custom.knob");
        TenantPolicySchema.Validate(new TenantPolicyChangeSet([new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: double.NaN)]))
            .Errors.ShouldContain("wrong_value_type:association.t-high");
        TenantPolicySchema.Validate(new TenantPolicyChangeSet(
            [
                new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.82),
                new(TenantPolicyKnobIds.AssociationTLow, NumberValue: 0.82),
            ])).Errors.ShouldContain("range_invalid:association.t-low");
        TenantPolicySchema.Validate(new TenantPolicyChangeSet(
            [
                new(TenantPolicyKnobIds.AiActionLowRiskAllowed, AiActionLowRiskAllowed: new Dictionary<AiActionRiskActionClass, bool>
                {
                    [AiActionRiskActionClass.ModifiesState] = true,
                }),
            ])).Errors.ShouldContain("ai_action_low_risk_map_invalid");
        TenantPolicySchema.Validate(new TenantPolicyChangeSet(
            [
                new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.90),
                new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.91),
            ])).Errors.ShouldContain("policy_knob_id_invalid");
        TenantPolicySchemaVersions.IsKnown(TenantPolicySchemaVersions.M0).ShouldBeTrue();
        TenantPolicySchemaVersions.IsKnown("tenant-policy-schema.custom.v1").ShouldBeFalse();
    }

    [Fact]
    public static void SummarySafeContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName",
            "EvidenceContent",
            "FileMetadata",
            "AuditReason",
            "MailboxBody",
            "MailboxSubject",
            "ProviderPayload",
            "RawClaim",
            "Header",
            "Token",
            "Secret",
        ];
        Type[] contractTypes =
        [
            typeof(AdminOperationReference),
            typeof(AdminQueueSummary),
            typeof(AdminQueueSummaryBucket),
            typeof(AdminQueueSummaryItemRef),
            typeof(GetAdminQueueSummary),
            typeof(ExecuteAdminQueueOperation),
            typeof(AssignTenantAdminRole),
            typeof(SubmitTenantPolicyChange),
            typeof(ApproveTenantPolicyChange),
            typeof(TenantPolicySnapshotMetadata),
        ];

        foreach (Type contractType in contractTypes)
        {
            string[] propertyNames = contractType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToArray();

            foreach (string blocked in blockedNameFragments)
            {
                propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), contractType.Name);
            }
        }
    }
}
