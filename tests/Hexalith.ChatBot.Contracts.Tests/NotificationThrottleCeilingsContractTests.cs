using System.Reflection;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class NotificationThrottleCeilingsContractTests
{
    [Fact]
    public static void SafeDefaultsShouldEqualTheNfr46GovernanceMaximums()
    {
        // The declared safe defaults are the hard NFR46 cap: 8 immediate pushes/hour, 30/day.
        NotificationThrottleCeilings.SafeDefaults.HourlyCeiling.ShouldBe(8);
        NotificationThrottleCeilings.SafeDefaults.DailyCeiling.ShouldBe(30);
        NotificationThrottleCeilings.HourlyMaximum.ShouldBe(8);
        NotificationThrottleCeilings.DailyMaximum.ShouldBe(30);
        NotificationThrottleCeilings.Minimum.ShouldBe(0);
        NotificationThrottleCeilings.SafeDefaults.IsWithinBounds.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(8, 30)]
    [InlineData(4, 15)]
    public static void CeilingsAtOrBelowTheMaximumShouldBeWithinBounds(int hourly, int daily)
        => new NotificationThrottleCeilings(hourly, daily).IsWithinBounds.ShouldBeTrue();

    [Theory]
    [InlineData(9, 30)]   // hourly above the NFR46 cap
    [InlineData(8, 31)]   // daily above the NFR46 cap
    [InlineData(-1, 30)]  // hourly below the floor
    [InlineData(8, -1)]   // daily below the floor
    public static void CeilingsOutsideTheBoundsShouldFailBoundsCheck(int hourly, int daily)
        => new NotificationThrottleCeilings(hourly, daily).IsWithinBounds.ShouldBeFalse();

    [Fact]
    public static void ThrottleCeilingsKnobShouldBeDeclaredInM1AndStandardSensitivity()
    {
        TenantPolicySchema.TryGetDefinition(TenantPolicyKnobIds.NotificationThrottleCeilings, out TenantPolicyKnobDefinition definition)
            .ShouldBeTrue();
        definition.Type.ShouldBe(TenantPolicyKnobType.NotificationThrottleCeilings);
        definition.SchemaVersion.ShouldBe(TenantPolicySchemaVersions.M1Preview);
        // Standard triage-tuning knob — NOT security-sensitive (no blanket two-person rule).
        TenantPolicySchema.IsSensitive(TenantPolicyKnobIds.NotificationThrottleCeilings).ShouldBeFalse();
    }

    [Fact]
    public static void ThrottleCeilingsKnobShouldAcceptCeilingsAtOrBelowTheCap()
        => TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.NotificationThrottleCeilings, NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(4, 15)),
        ])).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(9, 30)]
    [InlineData(8, 31)]
    [InlineData(-1, 15)]
    public static void ThrottleCeilingsKnobShouldRejectAboveMaximumOrOutOfRange(int hourly, int daily)
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.NotificationThrottleCeilings, NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(hourly, daily)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"range_invalid:{TenantPolicyKnobIds.NotificationThrottleCeilings}");
    }

    [Fact]
    public static void ThrottleCeilingsKnobShouldRejectWrongValueType()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.NotificationThrottleCeilings, NumberValue: 8.0),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.NotificationThrottleCeilings}");
    }

    [Fact]
    public static void ThrottleCeilingsKnobShouldRejectUndeclaredKnobId()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue("notification.throttle-ceilings.weekly", NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(4, 15)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("unknown_knob:notification.throttle-ceilings.weekly");
    }

    [Fact]
    public static void ThrottleCeilingsValueShouldNotRideOnOtherKnobTypes()
    {
        // The closed-schema invariant: the ceiling set cannot be smuggled onto a Double knob.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.9, NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(4, 15)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.AssociationTHigh}");
    }

    [Fact]
    public static void ThrottleCeilingsValueShouldNotRideOnApprovalPriorityWeightsKnob()
    {
        // Two closed knobs in the same schema version cannot smuggle each other's value field.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: new ApprovalPriorityWeights(1, 1, 1), NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(4, 15)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.ApprovalPriorityWeights}");
    }

    [Fact]
    public static void NotificationThrottleCeilingsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName", "EvidenceContent", "FileMetadata", "AuditReason",
            "MailboxBody", "MailboxSubject", "ProviderPayload", "RawClaim",
            "Header", "Token", "Secret", "Address", "RecipientAddress", "Prompt", "CommandBody",
        ];

        string[] propertyNames = typeof(NotificationThrottleCeilings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        foreach (string blocked in blockedNameFragments)
        {
            propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), blocked);
        }
    }
}
