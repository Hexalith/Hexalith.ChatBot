using System.Reflection;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ReviewerBacklogThresholdContractTests
{
    [Fact]
    public static void SafeDefaultShouldEqualTheNfr46GovernanceMaximum()
    {
        // The declared safe default is the hard NFR46 cap: alert when a reviewer holds > 25 open approval items.
        ReviewerBacklogThreshold.SafeDefault.OpenItemThreshold.ShouldBe(25);
        ReviewerBacklogThreshold.Maximum.ShouldBe(25);
        ReviewerBacklogThreshold.Minimum.ShouldBe(0);
        ReviewerBacklogThreshold.SafeDefault.IsWithinBounds.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    [InlineData(10)]
    public static void ThresholdAtOrBelowTheMaximumShouldBeWithinBounds(int threshold)
        => new ReviewerBacklogThreshold(threshold).IsWithinBounds.ShouldBeTrue();

    [Theory]
    [InlineData(26)]   // above the NFR46 cap — would suppress alerts and hide a backlog
    [InlineData(100)]
    [InlineData(-1)]   // below the floor
    public static void ThresholdOutsideTheBoundsShouldFailBoundsCheck(int threshold)
        => new ReviewerBacklogThreshold(threshold).IsWithinBounds.ShouldBeFalse();

    [Fact]
    public static void BacklogThresholdKnobShouldBeDeclaredInM1AndStandardSensitivity()
    {
        TenantPolicySchema.TryGetDefinition(TenantPolicyKnobIds.ReviewerBacklogThreshold, out TenantPolicyKnobDefinition definition)
            .ShouldBeTrue();
        definition.Type.ShouldBe(TenantPolicyKnobType.ReviewerBacklogThreshold);
        definition.SchemaVersion.ShouldBe(TenantPolicySchemaVersions.M1Preview);
        definition.Maximum.ShouldBe(25);
        definition.Minimum.ShouldBe(0);
        // Standard triage-tuning knob — NOT security-sensitive (no blanket two-person rule).
        TenantPolicySchema.IsSensitive(TenantPolicyKnobIds.ReviewerBacklogThreshold).ShouldBeFalse();
    }

    [Fact]
    public static void BacklogThresholdKnobShouldAcceptAThresholdAtOrBelowTheCap()
        => TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ReviewerBacklogThreshold, ReviewerBacklogThresholdValue: new ReviewerBacklogThreshold(10)),
        ])).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(26)]
    [InlineData(1000)]
    [InlineData(-1)]
    public static void BacklogThresholdKnobShouldRejectAboveMaximumOrOutOfRange(int threshold)
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ReviewerBacklogThreshold, ReviewerBacklogThresholdValue: new ReviewerBacklogThreshold(threshold)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"range_invalid:{TenantPolicyKnobIds.ReviewerBacklogThreshold}");
    }

    [Fact]
    public static void BacklogThresholdKnobShouldRejectWrongValueType()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ReviewerBacklogThreshold, NumberValue: 25.0),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.ReviewerBacklogThreshold}");
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public static void BacklogThresholdKnobShouldRejectNaNOrInfinityNumberValue(double number)
    {
        // NFR46/AC6: NaN/Infinity (a numeric value on a closed record-typed knob) is rejected with a safe reason code —
        // the closed schema accepts only the bounded ReviewerBacklogThreshold record, never a free-floating number.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ReviewerBacklogThreshold, NumberValue: number),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.ReviewerBacklogThreshold}");
    }

    [Fact]
    public static void BacklogThresholdKnobShouldRejectUndeclaredKnobId()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue("notification.reviewer-backlog-threshold.weekly", ReviewerBacklogThresholdValue: new ReviewerBacklogThreshold(10)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain("unknown_knob:notification.reviewer-backlog-threshold.weekly");
    }

    [Fact]
    public static void BacklogThresholdValueShouldNotRideOnOtherKnobTypes()
    {
        // The closed-schema invariant: the threshold cannot be smuggled onto a Double knob.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.9, ReviewerBacklogThresholdValue: new ReviewerBacklogThreshold(10)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.AssociationTHigh}");
    }

    [Fact]
    public static void BacklogThresholdValueShouldNotRideOnNotificationThrottleCeilingsKnob()
    {
        // Two closed knobs in the same schema version cannot smuggle each other's value field.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.NotificationThrottleCeilings, NotificationThrottleCeilingsValue: new NotificationThrottleCeilings(4, 15), ReviewerBacklogThresholdValue: new ReviewerBacklogThreshold(10)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.NotificationThrottleCeilings}");
    }

    [Fact]
    public static void ReviewerBacklogThresholdShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName", "EvidenceContent", "FileMetadata", "AuditReason",
            "MailboxBody", "MailboxSubject", "ProviderPayload", "RawClaim",
            "Header", "Token", "Secret", "Address", "RecipientAddress", "Prompt", "CommandBody",
        ];

        string[] propertyNames = typeof(ReviewerBacklogThreshold)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        foreach (string blocked in blockedNameFragments)
        {
            propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), blocked);
        }
    }
}
