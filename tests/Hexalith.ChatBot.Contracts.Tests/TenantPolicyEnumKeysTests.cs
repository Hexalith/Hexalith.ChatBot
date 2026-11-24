using System.Text.Json;

using Hexalith.ChatBot.Contracts.Commands;
using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

public sealed class TenantPolicyEnumKeysTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PolicyMapRoundTripsUsingDeclaredEnumKeys()
    {
        TenantPolicyValue value = TenantPolicySchema.DefaultM0Values.Single(
            item => item.KnobId == TenantPolicyKnobIds.AiActionLowRiskAllowed);
        string json = JsonSerializer.Serialize(value, Options);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement map = document.RootElement.GetProperty("aiActionLowRiskAllowed");
        map.GetProperty("modifies-state").GetBoolean().ShouldBeFalse();
        map.EnumerateObject().Count().ShouldBe(TenantPolicySchema.RequiredAiActionClasses.Count);

        TenantPolicyValue restored = JsonSerializer.Deserialize<TenantPolicyValue>(json, Options).ShouldNotBeNull();
        restored.AiActionLowRiskAllowed.ShouldNotBeNull().Keys.ShouldBe(TenantPolicySchema.RequiredAiActionClasses);
        TenantPolicySchema.Validate(new([restored])).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("ModifiesState")]
    [InlineData("unknown-action")]
    public void PolicyMapRejectsUndefinedEnumKeys(string key)
    {
        string json = $$"""{"{{key}}":false}""";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Dictionary<AiActionRiskActionClass, bool>>(json, Options));
    }

    [Fact]
    public void PolicyMapRejectsWritingAnUndefinedEnumKey()
        => Should.Throw<JsonException>(() => JsonSerializer.Serialize(
            new Dictionary<AiActionRiskActionClass, bool> { [(AiActionRiskActionClass)100] = false }, Options));
}
