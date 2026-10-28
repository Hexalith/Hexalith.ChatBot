using Hexalith.ChatBot.UI.Components.Governed;

using Shouldly;

namespace Hexalith.ChatBot.UI.Tests;

/// <summary>Preserves optional metadata suppression at the shared rendering boundary.</summary>
public sealed class ChatBotMetadataRenderingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void OptionalRowsShouldSuppressBlankValues(string? value)
    {
        ChatBotMetadataRendering.CodeRowIf("code", value).ShouldBeNull();
        ChatBotMetadataRendering.TextRowIf("text", value).ShouldBeNull();
    }

    [Fact]
    public void OptionalRowsShouldRetainPresentValues()
    {
        ChatBotMetadataRendering.CodeRowIf("code", "0").ShouldNotBeNull();
        ChatBotMetadataRendering.TextRowIf("text", "0").ShouldNotBeNull();
    }
}
