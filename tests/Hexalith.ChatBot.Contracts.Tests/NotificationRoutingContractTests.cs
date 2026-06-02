using System.Reflection;
using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;
using Hexalith.ChatBot.Contracts.Queries;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class NotificationRoutingContractTests
{
    [Theory]
    [InlineData(NotificationStateClass.ReviewNeeded, "review-needed")]
    [InlineData(NotificationStateClass.ApprovalPending, "approval-pending")]
    [InlineData(NotificationStateClass.Failure, "failure")]
    [InlineData(NotificationStateClass.Degraded, "degraded")]
    [InlineData(NotificationStateClass.Quarantine, "quarantine")]
    [InlineData(NotificationStateClass.Retry, "retry")]
    public static void NotificationStateClassWireTokensShouldRoundTrip(NotificationStateClass stateClass, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        NotificationStateClasses.ToWireValue(stateClass).ShouldBe(token);
        NotificationStateClasses.TryFromWireValue(token, out NotificationStateClass parsed).ShouldBeTrue();
        parsed.ShouldBe(stateClass);
        NotificationStateClasses.TryFromWireValue($" {token.ToUpperInvariant()} ", out parsed).ShouldBeTrue();
        parsed.ShouldBe(stateClass);
    }

    [Fact]
    public static void NotificationStateClassesShouldDeclareExactlySixClasses()
    {
        NotificationStateClasses.All.Count.ShouldBe(6);
        NotificationStateClasses.TryFromWireValue("escalation", out _).ShouldBeFalse();
        NotificationStateClasses.TryFromWireValue(null, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(NotificationChannel.InApp, "in-app")]
    [InlineData(NotificationChannel.Email, "email")]
    [InlineData(NotificationChannel.Webhook, "webhook")]
    [InlineData(NotificationChannel.OperatorAlert, "operator-alert")]
    public static void NotificationChannelWireTokensShouldRoundTrip(NotificationChannel channel, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        NotificationChannels.ToWireValue(channel).ShouldBe(token);
        NotificationChannels.TryFromWireValue(token, out NotificationChannel parsed).ShouldBeTrue();
        parsed.ShouldBe(channel);
        NotificationChannels.TryFromWireValue("sms", out _).ShouldBeFalse();
    }

    [Fact]
    public static void RoutingMapShouldValidateClosedDeclaredEntries()
    {
        NotificationRoutingSchema.Validate(ValidChangeSet()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public static void RoutingMapShouldRejectDuplicateStateClassScopeKeys()
    {
        NotificationRoutingChangeSet duplicate = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.TenantAdmin, NotificationChannel.Email),
        ]);

        NotificationRoutingValidationResult result = NotificationRoutingSchema.Validate(duplicate);
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("notification_routing_duplicate_key");
    }

    [Fact]
    public static void RoutingMapShouldRejectUndeclaredValues()
    {
        NotificationRoutingChangeSet undeclaredState = new(
        [
            new NotificationRoutingEntry((NotificationStateClass)99, AdminScope.Policy, AdminRole.PolicyAdmin, NotificationChannel.InApp),
        ]);
        NotificationRoutingSchema.Validate(undeclaredState).IsValid.ShouldBeFalse();

        NotificationRoutingChangeSet undeclaredChannel = new(
        [
            new NotificationRoutingEntry(NotificationStateClass.Retry, AdminScope.Operate, AdminRole.OperationsAdmin, (NotificationChannel)42),
        ]);
        NotificationRoutingSchema.Validate(undeclaredChannel).IsValid.ShouldBeFalse();

        NotificationRoutingSchema.Validate(new NotificationRoutingChangeSet([])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public static void RoutingMapShouldRejectMoreEntriesThanTheSchemaBound()
    {
        NotificationRoutingEntry entry = new(
            NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert);
        NotificationRoutingChangeSet overflowing = new(
            [.. Enumerable.Repeat(entry, NotificationRoutingSchema.MaxEntries + 1)]);

        NotificationRoutingValidationResult result = NotificationRoutingSchema.Validate(overflowing);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("notification_routing_entries_invalid");
    }

    [Fact]
    public static void RoutingSchemaVersionShouldBeKnownAndBounded()
    {
        NotificationRoutingSchemaVersions.IsKnown(NotificationRoutingSchemaVersions.V1).ShouldBeTrue();
        NotificationRoutingSchemaVersions.IsKnown("notification-routing-schema.custom").ShouldBeFalse();
        NotificationRoutingSchemaVersions.IsKnown(null).ShouldBeFalse();
    }

    [Fact]
    public static void SubmitNotificationRoutingChangeShouldSerializeMetadataOnly()
    {
        SubmitNotificationRoutingChange command = ValidCommand();

        string json = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("review-needed");
        json.ShouldContain("operator-alert");
        json.ShouldContain("operations-admin");
        json.ShouldNotContain("projectName", Case.Insensitive);
        json.ShouldNotContain("mailboxBody", Case.Insensitive);
        json.ShouldNotContain("providerPayload", Case.Insensitive);
        json.ShouldNotContain("secret", Case.Insensitive);
    }

    [Fact]
    public static void RoutingContractsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName", "EvidenceContent", "FileMetadata", "AuditReason",
            "MailboxBody", "MailboxSubject", "ProviderPayload", "RawClaim",
            "Header", "Token", "Secret", "Address", "RecipientAddress",
        ];
        Type[] contractTypes =
        [
            typeof(NotificationRoutingEntry),
            typeof(NotificationRoutingChangeSet),
            typeof(NotificationRoutingSnapshotMetadata),
            typeof(SubmitNotificationRoutingChange),
            typeof(NotificationRoutingSummaryRow),
            typeof(NotificationRoutingSummary),
            typeof(GetNotificationRoutingSummary),
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

    private static NotificationRoutingChangeSet ValidChangeSet()
        => new(
        [
            new NotificationRoutingEntry(NotificationStateClass.ReviewNeeded, AdminScope.SeeOnly, AdminRole.OperationsAdmin, NotificationChannel.InApp),
            new NotificationRoutingEntry(NotificationStateClass.ApprovalPending, AdminScope.Policy, AdminRole.PolicyAdmin, NotificationChannel.Email),
            new NotificationRoutingEntry(NotificationStateClass.Failure, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new NotificationRoutingEntry(NotificationStateClass.Degraded, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.OperatorAlert),
            new NotificationRoutingEntry(NotificationStateClass.Quarantine, AdminScope.Compliance, AdminRole.ComplianceAdmin, NotificationChannel.Email),
            new NotificationRoutingEntry(NotificationStateClass.Retry, AdminScope.Operate, AdminRole.OperationsAdmin, NotificationChannel.InApp),
        ]);

    private static SubmitNotificationRoutingChange ValidCommand()
        => new(
            "routing-change-001",
            "routing-snapshot-current",
            "routing-snapshot-proposed",
            4,
            ValidChangeSet(),
            "routing-update",
            "admin-requester",
            NotificationRoutingSchemaVersions.V1,
            "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            "sha256:routingold",
            "sha256:routingnew");
}
