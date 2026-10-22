namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>A control result with response-observation status and a metadata-only cause.</summary>
/// <param name="Succeeded">Whether the command reached durable and projected state.</param>
/// <param name="Unobserved">Whether the command POST produced no response.</param>
/// <param name="Cause">The stable cause token when the control did not succeed.</param>
internal sealed record ScopedOutageControlAvailability(bool Succeeded, bool Unobserved, string? Cause);
