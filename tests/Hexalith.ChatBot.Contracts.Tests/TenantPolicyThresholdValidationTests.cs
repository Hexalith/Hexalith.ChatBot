using Hexalith.ChatBot.Contracts.Commands;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public sealed class TenantPolicyThresholdValidationTests
{
    [Theory]
    [InlineData(0.85, 0.90, true)]
    [InlineData(0.90, 0.90, false)]
    [InlineData(0.95, 0.90, false)]
    public void ThresholdPairHasTheSameResultInBothOrders(double low, double high, bool valid)
    {
        TenantPolicyValue lowValue = new(TenantPolicyKnobIds.AssociationTLow, NumberValue: low);
        TenantPolicyValue highValue = new(TenantPolicyKnobIds.AssociationTHigh, NumberValue: high);
        TenantPolicyValidationResult lowFirst = TenantPolicySchema.Validate(new([lowValue, highValue]));
        TenantPolicyValidationResult highFirst = TenantPolicySchema.Validate(new([highValue, lowValue]));

        lowFirst.IsValid.ShouldBe(valid);
        highFirst.IsValid.ShouldBe(valid);
        lowFirst.Errors.ShouldBe(highFirst.Errors);
        if (!valid)
        {
            lowFirst.Errors.ShouldBe(["range_invalid:association.t-low"]);
        }

        TenantPolicySchema.Validate(new([lowValue])).IsValid.ShouldBeTrue();
        TenantPolicySchema.Validate(new([highValue])).IsValid.ShouldBeTrue();
    }
}
