namespace Hexalith.ChatBot.Server.Tests;

/// <summary>Shared no-leak markers for the Story 9.4 replay-isolation tests (mirrors the Story 9.1 WORM leak suite).</summary>
internal static class ReplayIsolationTestData
{
    public static readonly string[] BannedMarkers =
        ["secret", "password", "bearer", "token", "exception", ".txt", ".json", ".xml"];
}
