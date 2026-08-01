namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Closed logical and EventStore-safe tenant names used only by the live recovery topology.</summary>
internal static class RecoveryValidationTopology
{
    public const string LogicalTenantRef = "replay-test:recovery-validation";

    /// <summary>
    /// The EventStore-safe physical tenant. Must remain exactly <see cref="LogicalTenantRef"/> with the
    /// <c>replay-test:</c> prefix stripped — <c>RecoverySandboxTopologyComposer</c> derives and enforces that
    /// relationship so the single test-tenant predicate covers the tenant the data is actually written to.
    /// </summary>
    public const string StorageTenantRef = "recovery-validation";

    public const string ControlTenantRef = "tenant-beta";

    public const string MailboxClientId = "recovery-validation-mailbox-client";
}
