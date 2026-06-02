using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class EscalationPolicyContractTests
{
    [Theory]
    [InlineData(EscalationSeverity.Low, "low")]
    [InlineData(EscalationSeverity.Medium, "medium")]
    [InlineData(EscalationSeverity.High, "high")]
    public static void EscalationSeverityWireTokensShouldRoundTrip(EscalationSeverity severity, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        EscalationSeverities.ToWireValue(severity).ShouldBe(token);
        EscalationSeverities.TryFromWireValue(token, out EscalationSeverity parsed).ShouldBeTrue();
        parsed.ShouldBe(severity);
        EscalationSeverities.TryFromWireValue($" {token.ToUpperInvariant()} ", out parsed).ShouldBeTrue();
        parsed.ShouldBe(severity);
    }

    [Fact]
    public static void EscalationSeverityOrderingShouldBeDeterministic()
    {
        EscalationSeverities.Rank(EscalationSeverity.Low).ShouldBeLessThan(EscalationSeverities.Rank(EscalationSeverity.Medium));
        EscalationSeverities.Rank(EscalationSeverity.Medium).ShouldBeLessThan(EscalationSeverities.Rank(EscalationSeverity.High));
        EscalationSeverities.MeetsOrExceeds(EscalationSeverity.High, EscalationSeverity.Medium).ShouldBeTrue();
        EscalationSeverities.MeetsOrExceeds(EscalationSeverity.Medium, EscalationSeverity.Medium).ShouldBeTrue();
        EscalationSeverities.MeetsOrExceeds(EscalationSeverity.Low, EscalationSeverity.Medium).ShouldBeFalse();
    }

    [Fact]
    public static void RiskProxyShouldMapToFiniteSeverityLadder()
    {
        EscalationSeverities.FromRisk("low").ShouldBe(EscalationSeverity.Low);
        EscalationSeverities.FromRisk("HIGH").ShouldBe(EscalationSeverity.High);
        // Unknown/missing risk strings collapse to the medium default — never compared as free-form text.
        EscalationSeverities.FromRisk("catastrophic").ShouldBe(EscalationSeverity.Medium);
        EscalationSeverities.FromRisk(null).ShouldBe(EscalationSeverity.Medium);
    }

    [Fact]
    public static void EscalationSchemaShouldRestrictToTheFiveEscalatableStateClasses()
    {
        EscalationPolicySchema.EscalatableStateClasses.Count.ShouldBe(5);
        EscalationPolicySchema.EscalatableStateClasses.ShouldContain(NotificationStateClass.ReviewNeeded);
        EscalationPolicySchema.EscalatableStateClasses.ShouldContain(NotificationStateClass.ApprovalPending);
        EscalationPolicySchema.EscalatableStateClasses.ShouldContain(NotificationStateClass.Failure);
        EscalationPolicySchema.EscalatableStateClasses.ShouldContain(NotificationStateClass.Degraded);
        EscalationPolicySchema.EscalatableStateClasses.ShouldContain(NotificationStateClass.Quarantine);

        // `retry` is transient and deliberately excluded from escalation.
        EscalationPolicySchema.EscalatableStateClasses.ShouldNotContain(NotificationStateClass.Retry);
        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Retry, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public static void EscalationMapShouldValidateClosedDeclaredEntries()
    {
        EscalationPolicySchema.Validate(ValidChangeSet()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void EscalationMapShouldRejectDuplicateStateClassScopeKeys()
    {
        EscalationPolicyChangeSet duplicate = new(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 60, EscalationSeverity.Low, AdminRole.TenantAdmin, NotificationChannel.Email),
        ]);

        EscalationPolicyValidationResult result = EscalationPolicySchema.Validate(duplicate);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("escalation_policy_duplicate_key");
    }

    [Fact]
    public static void EscalationMapShouldRejectUndeclaredValuesAndOutOfRangeAge()
    {
        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry((NotificationStateClass)99, AdminScope.Policy, 3600, EscalationSeverity.High, AdminRole.PolicyAdmin, NotificationChannel.InApp),
        ])).IsValid.ShouldBeFalse();

        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, (EscalationSeverity)42, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ])).IsValid.ShouldBeFalse();

        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, (NotificationChannel)42),
        ])).IsValid.ShouldBeFalse();

        // Escalation-target roles must be declared AdminRole values (AC5); an undeclared role is rejected with a safe reason code.
        EscalationPolicyValidationResult undeclaredRole = EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, (AdminRole)99, NotificationChannel.OperatorAlert),
        ]));
        undeclaredRole.IsValid.ShouldBeFalse();
        undeclaredRole.Errors.ShouldContain("escalation_policy_target_role_invalid");

        // Negative and over-bound age thresholds are rejected with a safe reason code.
        EscalationPolicyValidationResult negative = EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, -1, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ]));
        negative.IsValid.ShouldBeFalse();
        negative.Errors.ShouldContain("escalation_policy_age_threshold_invalid");

        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet(
        [
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, EscalationPolicySchema.MaxAgeThresholdSeconds + 1, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
        ])).IsValid.ShouldBeFalse();

        EscalationPolicySchema.Validate(new EscalationPolicyChangeSet([])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public static void EscalationMapShouldRejectMoreEntriesThanTheSchemaBound()
    {
        EscalationPolicyEntry entry = new(
            NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert);
        EscalationPolicyChangeSet overflowing = new(
            [.. Enumerable.Repeat(entry, EscalationPolicySchema.MaxEntries + 1)]);

        EscalationPolicyValidationResult result = EscalationPolicySchema.Validate(overflowing);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("escalation_policy_entries_invalid");
    }

    [Fact]
    public static void EscalationSchemaVersionShouldBeKnownAndBounded()
    {
        EscalationPolicySchemaVersions.IsKnown(EscalationPolicySchemaVersions.V1).ShouldBeTrue();
        EscalationPolicySchemaVersions.IsKnown("escalation-policy-schema.custom").ShouldBeFalse();
        EscalationPolicySchemaVersions.IsKnown(null).ShouldBeFalse();
    }

    [Fact]
    public static void SubmitEscalationPolicyChangeShouldSerializeMetadataOnly()
    {
        SubmitEscalationPolicyChange command = ValidCommand();

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("review-needed");
        json.ShouldContain("operator-alert");
        json.ShouldContain("operations-admin");
        json.ShouldContain("high");
        json.ShouldNotContain("projectName", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public static void EscalationContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName", "EvidenceContent", "FileMetadata", "AuditReason",
            "MailboxBody", "MailboxSubject", "ProviderPayload", "RawClaim",
            "Header", "Token", "Secret", "Address", "RecipientAddress",
        ];
        Type[] contractTypes =
        [
            typeof(EscalationPolicyEntry),
            typeof(EscalationPolicyChangeSet),
            typeof(EscalationPolicySnapshotMetadata),
            typeof(SubmitEscalationPolicyChange),
            typeof(EscalationPolicySummaryRow),
            typeof(EscalationPolicySummary),
            typeof(GetEscalationPolicySummary),
        ];

        foreach (Type contractType in contractTypes)
        {
            string[] propertyNames = contractType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToArray();

            foreach (string blocked in blockedNameFragments)
            {
                propertyNames.ShouldNotContain(
                    name => name.Contains(blocked, StringComparison.Ordinal),
                    contractType.Name);
            }
        }
    }

    private static EscalationPolicyChangeSet ValidChangeSet()
        => new(
        [
            new EscalationPolicyEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, 86400, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            new EscalationPolicyEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, 43200, EscalationSeverity.Medium, AdminRole.PolicyAdmin, NotificationChannel.Email),
            new EscalationPolicyEntry(NotificationStateClass.Failure, AdminScope.Operate, 3600, EscalationSeverity.High, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new EscalationPolicyEntry(NotificationStateClass.Degraded, AdminScope.Operate, 7200, EscalationSeverity.Medium, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new EscalationPolicyEntry(NotificationStateClass.Quarantine, AdminScope.Compliance, 1800, EscalationSeverity.High, AdminRole.ComplianceAdmin, NotificationChannel.Email),
        ]);

    private static SubmitEscalationPolicyChange ValidCommand()
        => new(
            "escalation-change-001",
            "escalation-snapshot-current",
            "escalation-snapshot-proposed",
            4,
            ValidChangeSet(),
            "escalation-update",
            "admin-requester",
            EscalationPolicySchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:escalationold",
            "sha256:escalationnew");
}
