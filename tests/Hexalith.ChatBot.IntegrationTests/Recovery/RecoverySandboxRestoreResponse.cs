using System.Text.Json;

namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Reads the composite sandbox Restore body (<c>prior</c> + <c>current</c>) produced by Story 12.15 chunk 1c.
/// </summary>
internal static class RecoverySandboxRestoreResponse
{
    /// <summary>Returns whether the post-clear <c>current</c> snapshot reports the boundary as still faulted.</summary>
    public static bool IsCurrentlyFaulted(JsonElement root)
    {
        if (root.TryGetProperty("current", out JsonElement current))
        {
            return current.GetProperty("faulted").GetBoolean();
        }

        // Flat Restore bodies are rejected: the composite contract is mandatory after the chunk-1c decision.
        throw new InvalidOperationException(
            "The recovery sandbox Restore response must carry a composite 'current' snapshot.");
    }

    /// <summary>Returns whether the pre-clear <c>prior</c> snapshot reported the boundary as faulted.</summary>
    public static bool WasPreviouslyFaulted(JsonElement root)
    {
        if (root.TryGetProperty("prior", out JsonElement prior))
        {
            return prior.GetProperty("faulted").GetBoolean();
        }

        throw new InvalidOperationException(
            "The recovery sandbox Restore response must carry a composite 'prior' snapshot.");
    }
}
