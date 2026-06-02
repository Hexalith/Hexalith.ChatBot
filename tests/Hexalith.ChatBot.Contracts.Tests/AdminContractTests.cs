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

    [Theory]
    [InlineData(OperationalQueueFamily.AmbiguousAssociation, "ambiguous-association")]
    [InlineData(OperationalQueueFamily.UnresolvedParticipant, "unresolved-participant")]
    [InlineData(OperationalQueueFamily.PendingApproval, "pending-approval")]
    [InlineData(OperationalQueueFamily.FailedIngestion, "failed-ingestion")]
    [InlineData(OperationalQueueFamily.FailedAttachment, "failed-attachment")]
    [InlineData(OperationalQueueFamily.RetryableOperation, "retryable-operation")]
    public static void OperationalQueueFamilyWireTokensShouldBeFinite(OperationalQueueFamily family, string token)
    {
        OperationalQueueFamilies.ToWireValue(family).ShouldBe(token);
        OperationalQueueFamilies.TryFromWireValue(token, out OperationalQueueFamily parsed).ShouldBeTrue();
        parsed.ShouldBe(family);
        OperationalQueueFamilies.TryFromWireValue("custom-queue", out _).ShouldBeFalse();
    }

    [Fact]
    public static void OperationalQueueSearchContractsShouldValidatePagingTokensUtcAndSafeFilters()
    {
        SearchOperationalQueueItems valid = new(
            OperationalQueueFamily.AmbiguousAssociation,
            PageSize: 100,
            PageToken: "item:001",
            OperationalQueueSortKey.Priority,
            SortDescending: true,
            new OperationalQueueFilter(
                MinAgeSeconds: 1,
                MaxAgeSeconds: 3600,
                Risk: "high",
                MinConfidence: 0.1m,
                MaxConfidence: 0.9m,
                ProjectRef: "project:alpha",
                MailboxRef: "mailbox:ops",
                FailureState: "retryable",
                AssignedReviewerRef: "admin:reviewer",
                NextAction: "claim",
                ChangedAfterUtc: new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
                ChangedBeforeUtc: new DateTimeOffset(2026, 6, 2, 5, 0, 0, TimeSpan.Zero)));

        OperationalQueueContractValidator.Validate(valid).ShouldBeEmpty();

        OperationalQueueContractValidator.Validate(valid with { PageSize = 101 })
            .ShouldContain("page_size_invalid");
        OperationalQueueContractValidator.Validate(valid with { PageToken = "bearer-token" })
            .ShouldContain("page_token_invalid");
        OperationalQueueContractValidator.Validate(valid with
        {
            Filter = valid.Filter with
            {
                ProjectRef = "Project Alpha",
                ChangedAfterUtc = new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.FromHours(2)),
            },
        }).ShouldContain("project_filter_invalid");
        OperationalQueueContractValidator.Validate(valid with
        {
            Filter = valid.Filter with
            {
                ChangedAfterUtc = new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.FromHours(2)),
            },
        }).ShouldContain("changed_after_not_utc");
    }

    [Fact]
    public static void ClaimAssignAndPrioritizeCommandContractsShouldStayMetadataOnly()
    {
        ExecuteAdminQueueOperation command = new(
            "operation-assign-001",
            AdminQueueOperation.Assign,
            AdminScope.Operate,
            "queue:ambiguous",
            ["item:ambiguous-001"],
            1,
            "operator-assign",
            "policy-snapshot-admin-v1",
            12,
            "metadata_only",
            OperationalQueueFamily.AmbiguousAssociation,
            AssigneeRef: "admin:reviewer-a",
            ReviewerRef: "admin:operator-a",
            PreviousAssigneeRef: "admin:reviewer-b",
            CommandTimestampUtc: new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            OperationState: "waiting");

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("assign");
        json.ShouldContain("ambiguous-association");
        json.ShouldContain("admin:reviewer-a");
        json.ShouldNotContain("projectName", Case.Insensitive);
        json.ShouldNotContain("evidenceContent", Case.Insensitive);
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
                TenantPolicyKnobIds.ApprovalPriorityWeights,
                TenantPolicyKnobIds.ApprovalRouting,
                TenantPolicyKnobIds.AssociationTHigh,
                TenantPolicyKnobIds.AssociationTLow,
                TenantPolicyKnobIds.AttachmentsUnsafeHandling,
                TenantPolicyKnobIds.ClassifierExplanationLayerEnabled,
                TenantPolicyKnobIds.InboundAuthenticityStrictness,
                TenantPolicyKnobIds.MailboxRoutingRules,
                TenantPolicyKnobIds.ReviewerBacklogThreshold,
                TenantPolicyKnobIds.NotificationThrottleCeilings,
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
    public static void MailboxConfigurationSchemaShouldValidateMetadataOnlyPatternsRulesAndProviderRefs()
    {
        MailboxConfigurationChangeSet valid = MailboxChangeSet();

        MailboxConfigurationSchema.Validate(valid).IsValid.ShouldBeTrue();

        MailboxConfigurationSchema.Validate(valid with
        {
            MonitoredPatterns = [Pattern() with { MailboxId = "unsafe mailbox" }],
        }).Errors.ShouldContain("mailbox_id_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            RoutingRules = [Rule(), Rule()],
        }).Errors.ShouldContain("mailbox_routing_rule_duplicate");
        MailboxConfigurationSchema.Validate(valid with
        {
            RoutingRules = [Rule() with { Kind = MailboxRoutingRuleKind.Unknown }],
        }).Errors.ShouldContain("mailbox_routing_rule_kind_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            ProviderConnections = [Provider() with { ProviderKind = MailboxProviderKind.Unknown }],
        }).Errors.ShouldContain("mailbox_provider_kind_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            ProviderConnections = [Provider() with { Freshness = MailboxPermissionFreshnessState.Unknown }],
        }).Errors.ShouldContain("mailbox_permission_freshness_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            ProviderConnections = [Provider() with { CredentialFingerprint = "bearer.token.raw" }],
        }).Errors.ShouldContain("mailbox_provider_fingerprint_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            PermissionStatuses = [Permission() with { Permission = "Mail.ReadWrite" }],
        }).Errors.ShouldContain("mailbox_permission_invalid");
        MailboxConfigurationSchema.Validate(valid with
        {
            PermissionStatuses = [Permission() with { Freshness = MailboxPermissionFreshnessState.Unknown }],
        }).Errors.ShouldContain("mailbox_permission_freshness_invalid");
    }

    [Fact]
    public static void MailboxSourceDisableContractsShouldSerializeFiniteStateTokensAndMetadataOnlyFields()
    {
        SubmitMailboxSourceDisable submit = new(
            "mailbox-disable-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ApproveMailboxSourceDisable approve = new(
            "mailbox-disable-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Disabled,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        string json = JsonSerializer.Serialize(new { submit, approve }, options);

        // Finite enum wire tokens (not numeric ordinals) and a clean round-trip.
        json.ShouldContain("\"active\"");
        json.ShouldContain("\"disabled\"");
        JsonSerializer.Deserialize<SubmitMailboxSourceDisable>(JsonSerializer.Serialize(submit, options), options)
            .ShouldBe(submit);
        JsonSerializer.Deserialize<ApproveMailboxSourceDisable>(JsonSerializer.Serialize(approve, options), options)
            .ShouldBe(approve);

        // Metadata-only: no mailbox content, addresses, or secrets.
        json.ShouldNotContain("@", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);
        json.ShouldNotContain("accessToken", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);

        MailboxSourceControlSchemaVersions.IsKnown(MailboxSourceControlSchemaVersions.V1).ShouldBeTrue();
        MailboxSourceControlSchemaVersions.IsKnown("mailbox-source-control-schema.custom").ShouldBeFalse();
    }

    [Fact]
    public static void MailboxSourceQuarantineContractsShouldSerializeFiniteStateTokensAndMetadataOnlyFields()
    {
        SubmitMailboxSourceQuarantine submit = new(
            "mailbox-quarantine-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Quarantined,
            4,
            "admin-requester",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        ApproveMailboxSourceQuarantine approve = new(
            "mailbox-quarantine-001",
            "controlled-mailbox-001",
            "mailbox-source-unsafe-activity",
            "policy-snapshot-mailbox-v1",
            MailboxSourceControlState.Active,
            MailboxSourceControlState.Quarantined,
            5,
            "admin-requester",
            "admin-approver",
            MailboxSourceControlSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        string json = JsonSerializer.Serialize(new { submit, approve }, options);

        // Finite enum wire tokens (not numeric ordinals) and a clean round-trip.
        json.ShouldContain("\"active\"");
        json.ShouldContain("\"quarantined\"");
        JsonSerializer.Deserialize<SubmitMailboxSourceQuarantine>(JsonSerializer.Serialize(submit, options), options)
            .ShouldBe(submit);
        JsonSerializer.Deserialize<ApproveMailboxSourceQuarantine>(JsonSerializer.Serialize(approve, options), options)
            .ShouldBe(approve);

        // Metadata-only: no mailbox content, addresses, or secrets.
        json.ShouldNotContain("@", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);
        json.ShouldNotContain("accessToken", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public static void MailboxSourceRateLimitContractShouldSerializeBoundedBudgetWindowTokenAndMetadataOnlyFields()
    {
        SubmitMailboxSourceRateLimit submit = new(
            "mailbox-rate-limit-001",
            "controlled-mailbox-001",
            "mailbox-source-noisy-intake",
            "policy-snapshot-mailbox-v1",
            OldBudget: 0,
            NewBudget: 200,
            MailboxRateLimitWindow.RollingHour,
            4,
            "admin-requester",
            MailboxSourceRateLimitSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        string json = JsonSerializer.Serialize(submit, options);

        // Finite window wire token (not a numeric ordinal), budgets as integers, and a clean round-trip.
        json.ShouldContain("\"rolling-hour\"");
        json.ShouldContain("\"oldBudget\":0");
        json.ShouldContain("\"newBudget\":200");
        JsonSerializer.Deserialize<SubmitMailboxSourceRateLimit>(json, options).ShouldBe(submit);

        // Single-actor shape: no approver field and no control-state old/new-state fields.
        json.ShouldNotContain("approverRef", Case.Insensitive);
        json.ShouldNotContain("oldState", Case.Insensitive);
        json.ShouldNotContain("newState", Case.Insensitive);

        // Metadata-only: no mailbox content, addresses, or secrets.
        json.ShouldNotContain("@", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);

        // Closed bounds discipline (Story 7.9 mirror): out-of-bounds budget falls back to the safe default (the cap).
        MailboxSourceRateLimitSchemaVersions.IsKnown(MailboxSourceRateLimitSchemaVersions.V1).ShouldBeTrue();
        MailboxSourceRateLimitSchemaVersions.IsKnown("mailbox-source-rate-limit-schema.custom").ShouldBeFalse();
        new MailboxRateLimitBounds(MailboxRateLimitBounds.Maximum).IsWithinBounds.ShouldBeTrue();
        new MailboxRateLimitBounds(MailboxRateLimitBounds.Minimum).IsWithinBounds.ShouldBeTrue();
        new MailboxRateLimitBounds(MailboxRateLimitBounds.Maximum + 1).IsWithinBounds.ShouldBeFalse();
        new MailboxRateLimitBounds(-1).IsWithinBounds.ShouldBeFalse();
        MailboxRateLimitBounds.SafeDefaults.HourlyMessageBudget.ShouldBe(MailboxRateLimitBounds.Maximum);
    }

    [Fact]
    public static void MailboxConfigurationContractsShouldSerializeFiniteEnumsAndMetadataOnlyFields()
    {
        SubmitMailboxConfigurationChange command = new(
            "mailbox-change-001",
            "mailbox-config-current",
            "mailbox-config-proposed",
            3,
            MailboxChangeSet(),
            "mailbox-admin-update",
            "admin-requester",
            MailboxConfigurationSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:oldfingerprint001",
            "sha256:newfingerprint001");
        MailboxConfigurationSummary summary = new(
            "mailbox-config-active",
            MailboxConfigurationSchemaVersions.V1,
            [Pattern()],
            [Rule()],
            [Provider()],
            [Health()],
            "fresh",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW");

        string json = JsonSerializer.Serialize(new { command, summary }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("microsoft-graph");
        json.ShouldContain("degraded");
        json.ShouldContain("fresh");
        json.ShouldContain("Mail.Read");
        json.ShouldNotContain("accessToken", Case.Insensitive);
        json.ShouldNotContain("refreshToken", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
        json.ShouldNotContain("deltaToken", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);
        json.ShouldNotContain("headers", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
    }

    [Fact]
    public static void ComplianceAdministrationContractsShouldValidateSafeTokensAndBoundedRetentionWindows()
    {
        ComplianceAuditQueryFilters query = new(
            "audit-query-001",
            [new ComplianceAuditFilterRef("audit-filter-001", "actor", "actor-alpha")],
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero),
            100);
        RetentionConfigurationChangeSet retention = RetentionChangeSet();

        ComplianceAdministrationSchema.ValidateAuditQueryFilters(query).IsValid.ShouldBeTrue();
        ComplianceAdministrationSchema.ValidateRetentionChangeSet(retention).IsValid.ShouldBeTrue();
        ComplianceAdministrationSchema.ValidateAuditQueryFilters(query with
        {
            Filters = [new ComplianceAuditFilterRef("audit-filter-001", "raw-sql", "select-star")],
        }).Errors.ShouldContain("audit_filter_key_invalid");
        ComplianceAdministrationSchema.ValidateAuditQueryFilters(query with
        {
            Filters = [new ComplianceAuditFilterRef("audit-filter-001", "actor", "raw secret")],
        }).Errors.ShouldContain("audit_filter_value_invalid");
        ComplianceAdministrationSchema.ValidateRetentionChangeSet(retention with
        {
            Windows = [new RetentionWindow(ComplianceRetentionClassIds.AuditRecords, "audit-window", 30)],
        }).Errors.ShouldContain("audit_retention_window_bounds_invalid");
        ComplianceAdministrationSchema.ValidateRetentionChangeSet(retention with
        {
            Windows = [new RetentionWindow("custom-class", "custom-window", 365)],
        }).Errors.ShouldContain("retention_class_invalid");
        ComplianceAdministrationSchema.IsSafeFingerprint("sha256:retentionfingerprint001").ShouldBeTrue();
        ComplianceAdministrationSchema.IsSafeFingerprint("raw-secret").ShouldBeFalse();
    }

    [Fact]
    public static void ComplianceContractsShouldSerializeMetadataOnlyAuditAndRetentionRefs()
    {
        RequestComplianceInvestigation investigation = new(
            "investigation-001",
            "audit-query-001",
            ["audit-filter-001"],
            "compliance-investigation",
            "admin-requester",
            4,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            ComplianceAuditRedactionState.MetadataOnly,
            ComplianceEscalationStatus.NotRequested,
            ComplianceAdministrationSchemaVersions.V1);
        SubmitRetentionConfigurationChange retention = RetentionChange();
        ComplianceAuditResultRow row = new(
            "audit-record-001",
            "actor-alpha",
            "human",
            "SubmitRetentionConfigurationChange",
            "retention-change-001",
            "allow",
            "pre_commit_gate",
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "policy-snapshot-admin-v1",
            ComplianceAuditRedactionState.Restricted,
            ComplianceEscalationStatus.NotRequested,
            "request-access");

        string json = JsonSerializer.Serialize(new { investigation, retention, row }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("compliance-admin-schema.v1");
        json.ShouldContain("metadata-only");
        json.ShouldContain("source-email-metadata");
        json.ShouldNotContain("auditEnvelope", Case.Insensitive);
        json.ShouldNotContain("projectName", Case.Insensitive);
        json.ShouldNotContain("mailboxSubject", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("prompt", Case.Insensitive);
        json.ShouldNotContain("token", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
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
            typeof(SubmitMailboxConfigurationChange),
            typeof(SubmitMailboxSourceDisable),
            typeof(ApproveMailboxSourceDisable),
            typeof(SubmitMailboxSourceQuarantine),
            typeof(ApproveMailboxSourceQuarantine),
            typeof(SubmitMailboxSourceRateLimit),
            typeof(RecordMailboxProviderConnection),
            typeof(MailboxConfigurationChangeSet),
            typeof(MonitoredMailboxPattern),
            typeof(MailboxRoutingRule),
            typeof(MailboxProviderConnectionMetadata),
            typeof(MailboxPermissionStatus),
            typeof(MailboxHealthStatusRecord),
            typeof(MailboxConfigurationSummary),
            typeof(ComplianceAuditFilterRef),
            typeof(ComplianceAuditQueryFilters),
            typeof(ComplianceAuditResultRow),
            typeof(ComplianceAuditDetail),
            typeof(ComplianceAuditSearchResult),
            typeof(SearchComplianceAuditRecords),
            typeof(GetComplianceAuditDetail),
            typeof(RequestComplianceInvestigation),
            typeof(RequestComplianceEscalation),
            typeof(SubmitRetentionConfigurationChange),
            typeof(RetentionConfigurationChangeSet),
            typeof(RetentionWindow),
            typeof(RetentionSnapshotMetadata),
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

    private static MailboxConfigurationChangeSet MailboxChangeSet()
        => new(
            [Pattern()],
            [Rule()],
            [Provider()],
            [Permission()]);

    private static MonitoredMailboxPattern Pattern()
        => new("controlled-mailbox-001", "graph-message-v1", "provider-connection-001", true, "mailbox-pattern:001");

    private static MailboxRoutingRule Rule()
        => new("routing-rule-001", MailboxRoutingRuleKind.SourceContext, "graph-message-v1", "route-project-intake", 10, "mailbox-routing");

    private static MailboxProviderConnectionMetadata Provider()
        => new(
            "provider-connection-001",
            MailboxProviderKind.MicrosoftGraph,
            "sha256:credentialfingerprint001",
            "graph-permission-evidence-001",
            MailboxPermissionFreshnessState.Fresh,
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

    private static MailboxPermissionStatus Permission()
        => new(
            "permission-status-001",
            "provider-connection-001",
            "Mail.Read",
            MailboxPermissionFreshnessState.Fresh,
            "graph-permission-evidence-001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero),
            "permission-fresh");

    private static MailboxHealthStatusRecord Health()
        => new(
            "mailbox-health-001",
            "controlled-mailbox-001",
            MailboxProcessingHealth.Degraded,
            MailboxDegradationReasonCode.GraphTokenExpired,
            MailboxPermissionFreshnessState.Stale,
            "mailbox-admin",
            "reconnect",
            "Reconnect mailbox permission metadata.",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));

    private static RetentionConfigurationChangeSet RetentionChangeSet()
        => new(
            [
                new RetentionWindow(ComplianceRetentionClassIds.SourceEmailMetadata, "source-email-metadata-window", 365),
                new RetentionWindow(ComplianceRetentionClassIds.AuditRecords, "audit-records-window", 2555),
            ]);

    private static SubmitRetentionConfigurationChange RetentionChange()
        => new(
            "retention-change-001",
            "retention-snapshot-current",
            "retention-snapshot-proposed",
            4,
            RetentionChangeSet(),
            "compliance-retention-update",
            "admin-requester",
            ComplianceAdministrationSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "policy-snapshot-admin-v1",
            "sha256:oldretentionfingerprint001",
            "sha256:newretentionfingerprint001",
            new DateTimeOffset(2026, 6, 2, 4, 0, 0, TimeSpan.Zero));
}
