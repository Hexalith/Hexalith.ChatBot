using System.Runtime.Serialization;

using Hexalith.ChatBot.Contracts.Enums;

using Shouldly;

namespace Hexalith.ChatBot.Contracts.Tests;

/// <summary>
/// Pins the surface-origin wire contract (FR85 / S7): every closed member round-trips through its stable
/// token, and an absent/blank/unknown declaration collapses to the safe <see cref="ChatBotSurfaceOrigin.Api"/>
/// default so an unattributed or malformed origin is still audited rather than rejected or trusted as a rewrite.
/// </summary>
public static class ChatBotSurfaceOriginsTests
{
    public static TheoryData<ChatBotSurfaceOrigin, string> OriginWireTokens =>
        new()
        {
            { ChatBotSurfaceOrigin.Api, "api" },
            { ChatBotSurfaceOrigin.Ui, "ui" },
            { ChatBotSurfaceOrigin.Cli, "cli" },
            { ChatBotSurfaceOrigin.Mcp, "mcp" },
            { ChatBotSurfaceOrigin.Worker, "worker" },
            { ChatBotSurfaceOrigin.Mailbox, "mailbox" },
            { ChatBotSurfaceOrigin.Ai, "ai" },
        };

    [Fact]
    public static void DefaultWireValueShouldBeApi()
        => ChatBotSurfaceOrigins.DefaultWireValue.ShouldBe("api");

    [Fact]
    public static void ApiShouldBeTheZeroValueSoUnsetDefaultsToTheSafeSurface()
        => ((int)ChatBotSurfaceOrigin.Api).ShouldBe(0);

    [Theory]
    [MemberData(nameof(OriginWireTokens))]
    public static void ToWireValueShouldReturnTheStableTokenForEveryMember(ChatBotSurfaceOrigin origin, string expectedToken)
        => ChatBotSurfaceOrigins.ToWireValue(origin).ShouldBe(expectedToken);

    [Theory]
    [MemberData(nameof(OriginWireTokens))]
    public static void FromWireValueShouldResolveEveryStableToken(ChatBotSurfaceOrigin expected, string token)
        => ChatBotSurfaceOrigins.FromWireValueOrDefault(token).ShouldBe(expected);

    [Theory]
    [MemberData(nameof(OriginWireTokens))]
    public static void WireTokensShouldRoundTripThroughBothDirections(ChatBotSurfaceOrigin origin, string token)
    {
        ChatBotSurfaceOrigins.FromWireValueOrDefault(ChatBotSurfaceOrigins.ToWireValue(origin)).ShouldBe(origin);
        ChatBotSurfaceOrigins.ToWireValue(ChatBotSurfaceOrigins.FromWireValueOrDefault(token)).ShouldBe(token);
    }

    [Theory]
    [InlineData("UI")]
    [InlineData("Ui")]
    [InlineData(" ui ")]
    [InlineData("\tUI\n")]
    public static void FromWireValueShouldBeCaseAndWhitespaceInsensitive(string declared)
        => ChatBotSurfaceOrigins.FromWireValueOrDefault(declared).ShouldBe(ChatBotSurfaceOrigin.Ui);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("totally-unknown-surface")]
    [InlineData("admin")]
    public static void FromWireValueShouldCollapseAbsentOrUnknownDeclarationToTheSafeDefault(string? declared)
        => ChatBotSurfaceOrigins.FromWireValueOrDefault(declared).ShouldBe(ChatBotSurfaceOrigin.Api);

    [Theory]
    [MemberData(nameof(OriginWireTokens))]
    public static void EnumMemberAttributesShouldMatchTheWireTokens(ChatBotSurfaceOrigin origin, string expectedToken)
        => typeof(ChatBotSurfaceOrigin)
            .GetField(origin.ToString())
            .ShouldNotBeNull()
            .GetCustomAttributes(typeof(EnumMemberAttribute), inherit: false)
            .OfType<EnumMemberAttribute>()
            .Single()
            .Value
            .ShouldBe(expectedToken);
}
