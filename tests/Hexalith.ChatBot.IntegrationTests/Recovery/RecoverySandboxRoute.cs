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
        if (action is not ("fault" or "restore" or "status" or "process"))
        {
            throw new InvalidOperationException(
                $"Scoped-outage action '{action}' is not in the closed set fault|restore|status|process.");
        }

        bool isProcess = string.Equals(action, "process", StringComparison.Ordinal);
        bool hasCorrelation = !string.IsNullOrWhiteSpace(correlationId);
        if (isProcess != hasCorrelation)
        {
            throw new InvalidOperationException(
                isProcess
                    ? "Scoped-outage process routes require a correlation locator."
                    : "Only scoped-outage process routes accept a correlation locator.");
        }

        string path = $"/recovery/{Uri.EscapeDataString(tenantRef)}/scoped-outage/" +
            $"{Uri.EscapeDataString(dependency)}/{action}";
        return isProcess ? $"{path}/{Uri.EscapeDataString(correlationId!)}" : path;
    }
}
