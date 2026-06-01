namespace Hexalith.ChatBot.Contracts.Messages;

public static class ChatBotRefusalReasonCodes
{
    public const string TenantPolicyExceeded = "tenant-policy-exceeded";
    public const string ProjectAuthorizationDenied = "project-authorization-denied";
    public const string SenderAuthorityDenied = "sender-authority-denied";
    public const string ApprovedCommandScopeExceeded = "approved-command-scope-exceeded";
    public const string CommandNotAllowlisted = "command-not-allowlisted";
    public const string UnsupportedAction = "unsupported-action";
    public const string UnresolvedAssociation = "unresolved-association";
    public const string UnresolvedParticipant = "unresolved-participant";
    public const string MissingRequiredContext = "missing-required-context";
    public const string ContextPackageUnavailable = "context-package-unavailable";
    public const string EvidenceExpired = "evidence-expired";
    public const string PolicySnapshotUnavailable = "policy-snapshot-unavailable";
    public const string ApprovalStateInvalid = "approval-state-invalid";
    public const string CorrectedContextInvalidated = "corrected-context-invalidated";
    public const string DependencyDegraded = "dependency-degraded";

    public static IReadOnlyList<string> All { get; } =
    [
        TenantPolicyExceeded,
        ProjectAuthorizationDenied,
        SenderAuthorityDenied,
        ApprovedCommandScopeExceeded,
        CommandNotAllowlisted,
        UnsupportedAction,
        UnresolvedAssociation,
        UnresolvedParticipant,
        MissingRequiredContext,
        ContextPackageUnavailable,
        EvidenceExpired,
        PolicySnapshotUnavailable,
        ApprovalStateInvalid,
        CorrectedContextInvalidated,
        DependencyDegraded,
    ];

    public static string CatalogCodeFor(string reasonCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);

        return reasonCode switch
        {
            TenantPolicyExceeded => ChatBotMessageCodes.RefusalBlockedAction,
            ProjectAuthorizationDenied => ChatBotMessageCodes.AuthorizationDenied,
            SenderAuthorityDenied => ChatBotMessageCodes.AuthorizationDenied,
            ApprovedCommandScopeExceeded => ChatBotMessageCodes.RefusalBlockedAction,
            CommandNotAllowlisted => ChatBotMessageCodes.RefusalBlockedAction,
            UnsupportedAction => ChatBotMessageCodes.RefusalBlockedAction,
            UnresolvedAssociation => ChatBotMessageCodes.AssociationAmbiguousRouted,
            UnresolvedParticipant => ChatBotMessageCodes.UnresolvedParticipant,
            MissingRequiredContext => ChatBotMessageCodes.ProjectAiContextPackageUnavailable,
            ContextPackageUnavailable => ChatBotMessageCodes.ProjectAiContextPackageUnavailable,
            EvidenceExpired => ChatBotMessageCodes.AssociationEvidenceExpired,
            PolicySnapshotUnavailable => ChatBotMessageCodes.DependencyDegraded,
            ApprovalStateInvalid => ChatBotMessageCodes.InvalidLifecycleTransition,
            CorrectedContextInvalidated => ChatBotMessageCodes.AssociationAiContextBlocked,
            DependencyDegraded => ChatBotMessageCodes.DependencyDegraded,
            _ => ChatBotMessageCodes.RefusalBlockedAction,
        };
    }

    public static ChatBotMessageCatalogEntry CatalogEntryFor(string reasonCode)
        => ChatBotMessageCatalog.Resolve(CatalogCodeFor(reasonCode));
}
