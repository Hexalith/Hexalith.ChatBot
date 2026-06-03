namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

/// <summary>
/// The closed set of triggers that land a workflow item in the terminal <see cref="LifecycleStates.Skipped"/>
/// state (M1 lifecycle completion). Both are resolved through the transition guard to a guard-validated
/// <c>Received-&gt;Skipped</c> edge so no surface fabricates the skip edge with a magic string.
/// </summary>
internal enum LifecycleSkipTrigger
{
    /// <summary>
    /// A duplicate provider message suppressed by message-intake idempotency
    /// (<c>tenant_id + mailbox_id + provider_message_id</c>, Story 2.9). Reprocessing creates a new instance.
    /// </summary>
    DuplicateSuppression,

    /// <summary>
    /// A message whose mailbox is not a governed participant for the tenant (FR18 mailbox-participation rule).
    /// </summary>
    OutOfScopeMailbox,
}
