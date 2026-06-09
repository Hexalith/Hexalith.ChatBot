using System.Runtime.Serialization;

using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Pins the health-status wire contract (Story 1.6, AC5): every closed member round-trips through its stable
/// token (<c>healthy</c>/<c>degraded</c>/<c>failed</c>/<c>unknown</c>), an absent/blank/unknown declaration
/// collapses to the fail-safe <see cref="ChatBotHealthStatus.Unknown"/> (never a fabricated healthy), and the
/// helper tokens match the enum's <see cref="EnumMemberAttribute"/> wire values. This is the binding the
/// <c>/health/chatbot</c> endpoint uses instead of a magic string, so a status value can only ever be an
/// explicit member of the closed contract.
/// </summary>
public static class ChatBotHealthStatusesTests
{
    public static TheoryData<ChatBotHealthStatus, string> HealthWireTokens =>
        new()
        {
            { ChatBotHealthStatus.Healthy, "healthy" },
            { ChatBotHealthStatus.Degraded, "degraded" },
            { ChatBotHealthStatus.Failed, "failed" },
            { ChatBotHealthStatus.Unknown, "unknown" },
        };

    [Fact]
    public static void DefaultWireValueShouldBeUnknown()
        => ChatBotHealthStatuses.DefaultWireValue.ShouldBe("unknown");

    [Theory]
    [MemberData(nameof(HealthWireTokens))]
    public static void ToWireValueShouldReturnTheStableTokenForEveryMember(ChatBotHealthStatus status, string expectedToken)
        => ChatBotHealthStatuses.ToWireValue(status).ShouldBe(expectedToken);

    [Theory]
    [MemberData(nameof(HealthWireTokens))]
    public static void FromWireValueShouldResolveEveryStableToken(ChatBotHealthStatus expected, string token)
        => ChatBotHealthStatuses.FromWireValueOrUnknown(token).ShouldBe(expected);

    [Theory]
    [MemberData(nameof(HealthWireTokens))]
    public static void WireTokensShouldRoundTripThroughBothDirections(ChatBotHealthStatus status, string token)
    {
        ChatBotHealthStatuses.FromWireValueOrUnknown(ChatBotHealthStatuses.ToWireValue(status)).ShouldBe(status);
        ChatBotHealthStatuses.ToWireValue(ChatBotHealthStatuses.FromWireValueOrUnknown(token)).ShouldBe(token);
    }

    [Theory]
    [InlineData("HEALTHY")]
    [InlineData("Healthy")]
    [InlineData(" healthy ")]
    [InlineData("\tHEALTHY\n")]
    public static void FromWireValueShouldBeCaseAndWhitespaceInsensitive(string declared)
        => ChatBotHealthStatuses.FromWireValueOrUnknown(declared).ShouldBe(ChatBotHealthStatus.Healthy);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("totally-unknown-status")]
    [InlineData("ok")]
    public static void FromWireValueShouldCollapseAbsentOrUnknownDeclarationToTheFailSafeUnknown(string? declared)
        => ChatBotHealthStatuses.FromWireValueOrUnknown(declared).ShouldBe(ChatBotHealthStatus.Unknown);

    [Theory]
    [MemberData(nameof(HealthWireTokens))]
    public static void EnumMemberAttributesShouldMatchTheWireTokens(ChatBotHealthStatus status, string expectedToken)
        => typeof(ChatBotHealthStatus)
            .GetField(status.ToString())
            .ShouldNotBeNull()
            .GetCustomAttributes(typeof(EnumMemberAttribute), inherit: false)
            .OfType<EnumMemberAttribute>()
            .Single()
            .Value
            .ShouldBe(expectedToken);
}
