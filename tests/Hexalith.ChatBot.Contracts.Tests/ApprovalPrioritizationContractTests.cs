using System.Reflection;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public static class ApprovalPrioritizationContractTests
{
    [Theory]
    [InlineData(RiskClass.None, "none")]
    [InlineData(RiskClass.Low, "low")]
    [InlineData(RiskClass.Medium, "medium")]
    [InlineData(RiskClass.High, "high")]
    [InlineData(RiskClass.Blocked, "blocked")]
    public static void RiskClassWireTokensShouldRoundTrip(RiskClass riskClass, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        RiskClasses.ToWireValue(riskClass).ShouldBe(token);
        RiskClasses.TryFromWireValue(token, out RiskClass parsed).ShouldBeTrue();
        parsed.ShouldBe(riskClass);
        RiskClasses.TryFromWireValue($" {token.ToUpperInvariant()} ", out parsed).ShouldBeTrue();
        parsed.ShouldBe(riskClass);
    }

    [Fact]
    public static void RiskClassRankShouldBeDeterministicAndOrdered()
    {
        RiskClasses.Rank(RiskClass.None).ShouldBeLessThan(RiskClasses.Rank(RiskClass.Low));
        RiskClasses.Rank(RiskClass.Low).ShouldBeLessThan(RiskClasses.Rank(RiskClass.Medium));
        RiskClasses.Rank(RiskClass.Medium).ShouldBeLessThan(RiskClasses.Rank(RiskClass.High));
        RiskClasses.Rank(RiskClass.High).ShouldBeLessThan(RiskClasses.Rank(RiskClass.Blocked));
        RiskClasses.MeetsOrExceeds(RiskClass.High, RiskClass.Medium).ShouldBeTrue();
        RiskClasses.MeetsOrExceeds(RiskClass.Low, RiskClass.High).ShouldBeFalse();
    }

    [Fact]
    public static void RiskProxyShouldMapToFiniteRiskLadderFailSafeLowest()
    {
        RiskClasses.FromRiskProxy("critical").ShouldBe(RiskClass.High);
        RiskClasses.FromRiskProxy("HIGH").ShouldBe(RiskClass.High);
        RiskClasses.FromRiskProxy("medium").ShouldBe(RiskClass.Medium);
        RiskClasses.FromRiskProxy("low").ShouldBe(RiskClass.Low);
        // Unknown/missing risk strings collapse to the lowest rank — fail-safe, never fail-open to top priority.
        RiskClasses.FromRiskProxy("catastrophic").ShouldBe(RiskClass.None);
        RiskClasses.FromRiskProxy(null).ShouldBe(RiskClass.None);
    }

    [Fact]
    public static void SenderAuthorityRankShouldBeDeterministicAndOrdered()
    {
        SenderAuthorityClasses.Rank(SenderAuthorityClass.DraftOnly)
            .ShouldBeLessThan(SenderAuthorityClasses.Rank(SenderAuthorityClass.AuthenticatedUserSend));
        SenderAuthorityClasses.Rank(SenderAuthorityClass.AuthenticatedUserSend)
            .ShouldBeLessThan(SenderAuthorityClasses.Rank(SenderAuthorityClass.SharedMailboxSend));
        SenderAuthorityClasses.Rank(SenderAuthorityClass.SharedMailboxSend)
            .ShouldBeLessThan(SenderAuthorityClasses.Rank(SenderAuthorityClass.SendOnBehalf));
        SenderAuthorityClasses.Rank(SenderAuthorityClass.SendOnBehalf)
            .ShouldBeLessThan(SenderAuthorityClasses.Rank(SenderAuthorityClass.ApprovedServiceSend));
    }

    [Fact]
    public static void SenderAuthorityUnknownShouldCollapseToLowestRank()
    {
        SenderAuthorityClasses.FromWireValueOrLowest("undeclared-authority").ShouldBe(SenderAuthorityClass.DraftOnly);
        SenderAuthorityClasses.FromWireValueOrLowest(null).ShouldBe(SenderAuthorityClass.DraftOnly);
        SenderAuthorityClasses.FromWireValueOrLowest("send-on-behalf").ShouldBe(SenderAuthorityClass.SendOnBehalf);
    }

    [Fact]
    public static void ApprovalPriorityWeightsSafeDefaultsShouldBeBounded()
    {
        ApprovalPriorityWeights.SafeDefaults.IsWithinBounds.ShouldBeTrue();
        ApprovalPriorityWeights.SafeDefaults.RiskWeight.ShouldBe(1.0);
        ApprovalPriorityWeights.SafeDefaults.AuthorityWeight.ShouldBe(1.0);
        ApprovalPriorityWeights.SafeDefaults.TimeInQueueWeight.ShouldBe(1.0);
    }

    [Theory]
    [InlineData(-0.1, 1.0, 1.0)]
    [InlineData(1.0, 1000.0, 1.0)]
    [InlineData(double.NaN, 1.0, 1.0)]
    [InlineData(1.0, double.PositiveInfinity, 1.0)]
    public static void ApprovalPriorityWeightsOutOfBoundsShouldFailBoundsCheck(double risk, double authority, double age)
        => new ApprovalPriorityWeights(risk, authority, age).IsWithinBounds.ShouldBeFalse();

    [Fact]
    public static void PriorityWeightsKnobShouldBeDeclaredInM1AndStandardSensitivity()
    {
        TenantPolicySchema.TryGetDefinition(TenantPolicyKnobIds.ApprovalPriorityWeights, out TenantPolicyKnobDefinition definition)
            .ShouldBeTrue();
        definition.Type.ShouldBe(TenantPolicyKnobType.ApprovalPriorityWeights);
        definition.SchemaVersion.ShouldBe(TenantPolicySchemaVersions.M1Preview);
        // Standard triage-tuning knob — NOT security-sensitive (no blanket two-person rule).
        TenantPolicySchema.IsSensitive(TenantPolicyKnobIds.ApprovalPriorityWeights).ShouldBeFalse();
    }

    [Fact]
    public static void PriorityWeightsKnobShouldAcceptBoundedWeights()
        => TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: new ApprovalPriorityWeights(2.5, 0.0, 1.0)),
        ])).IsValid.ShouldBeTrue();

    [Fact]
    public static void PriorityWeightsKnobShouldRejectOutOfRangeWeights()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: new ApprovalPriorityWeights(-1.0, 1.0, 1.0)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"range_invalid:{TenantPolicyKnobIds.ApprovalPriorityWeights}");
    }

    [Fact]
    public static void PriorityWeightsKnobShouldRejectNaNAndInfinity()
    {
        foreach (ApprovalPriorityWeights weights in new[]
                 {
                     new ApprovalPriorityWeights(double.NaN, 1.0, 1.0),
                     new ApprovalPriorityWeights(1.0, double.PositiveInfinity, 1.0),
                 })
        {
            TenantPolicySchema.Validate(new TenantPolicyChangeSet(
            [
                new TenantPolicyValue(TenantPolicyKnobIds.ApprovalPriorityWeights, ApprovalPriorityWeightsValue: weights),
            ])).IsValid.ShouldBeFalse();
        }
    }

    [Fact]
    public static void PriorityWeightsKnobShouldRejectWrongValueType()
    {
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.ApprovalPriorityWeights, NumberValue: 1.0),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.ApprovalPriorityWeights}");
    }

    [Fact]
    public static void PriorityWeightsValueShouldNotRideOnOtherKnobTypes()
    {
        // The closed-schema invariant: the weight set cannot be smuggled onto a Double knob.
        TenantPolicyValidationResult result = TenantPolicySchema.Validate(new TenantPolicyChangeSet(
        [
            new TenantPolicyValue(TenantPolicyKnobIds.AssociationTHigh, NumberValue: 0.9, ApprovalPriorityWeightsValue: new ApprovalPriorityWeights(1, 1, 1)),
        ]));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain($"wrong_value_type:{TenantPolicyKnobIds.AssociationTHigh}");
    }

    [Fact]
    public static void ApprovalPriorityWeightsShouldNotExposeSecretBearingProperties()
    {
        string[] blockedNameFragments =
        [
            "ProjectName", "EvidenceContent", "FileMetadata", "AuditReason",
            "MailboxBody", "MailboxSubject", "ProviderPayload", "RawClaim",
            "Header", "Token", "Secret", "Address", "RecipientAddress", "Prompt", "CommandBody",
        ];

        string[] propertyNames = typeof(ApprovalPriorityWeights)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(static property => property.Name)
            .ToArray();

        foreach (string blocked in blockedNameFragments)
        {
            propertyNames.ShouldNotContain(name => name.Contains(blocked, StringComparison.Ordinal), blocked);
        }
    }
}
