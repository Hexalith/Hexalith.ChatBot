namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Builds the closed recovery-sandbox route vocabulary without permitting shape drift.</summary>
internal static class RecoverySandboxRoute
{
    /// <summary>Builds one scoped-outage route and enforces correlation locators only on process operations.</summary>
    public static string ScopedOutage(
        string tenantRef,
        string dependency,
        string action,
        string? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantRef);
        ArgumentException.ThrowIfNullOrWhiteSpace(dependency);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        bool isProcess = string.Equals(action, "process", StringComparison.Ordinal);
        if (isProcess != !string.IsNullOrWhiteSpace(correlationId) ||
            action is not ("fault" or "restore" or "status" or "process"))
        {
            throw new InvalidOperationException("Only scoped-outage process routes accept a correlation locator.");
        }

        string path = $"/recovery/{Uri.EscapeDataString(tenantRef)}/scoped-outage/" +
            $"{Uri.EscapeDataString(dependency)}/{action}";
        return isProcess ? $"{path}/{Uri.EscapeDataString(correlationId!)}" : path;
    }
}
