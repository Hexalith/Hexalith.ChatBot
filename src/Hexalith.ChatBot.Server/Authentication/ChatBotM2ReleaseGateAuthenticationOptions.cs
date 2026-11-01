using Microsoft.AspNetCore.Authentication;

namespace Hexalith.ChatBot.Server.Authentication;

/// <summary>Options for the shared-secret scheme that protects the M2 release-gate endpoint.</summary>
internal sealed class ChatBotM2ReleaseGateAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Gets or sets the configured release-gate secret; no request authenticates while it is blank.</summary>
    public string? Token { get; set; }
}
