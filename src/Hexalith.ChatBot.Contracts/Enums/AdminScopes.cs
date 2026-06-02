namespace Hexalith.ChatBot.Contracts.Enums;

public static class AdminScopes
{
    public const string SeeOnly = "see-only";
    public const string Operate = "operate";
    public const string Policy = "policy";
    public const string Mailbox = "mailbox";
    public const string Compliance = "compliance";
    public const string AuditObligation = "audit-obligation";

    public static IReadOnlyList<AdminScope> All { get; } =
    [
        AdminScope.SeeOnly,
        AdminScope.Operate,
        AdminScope.Policy,
        AdminScope.Mailbox,
        AdminScope.Compliance,
        AdminScope.AuditObligation,
    ];

    public static bool TryFromWireValue(string? value, out AdminScope scope)
    {
        scope = AdminScope.SeeOnly;
        switch (value?.Trim().ToLowerInvariant())
        {
            case SeeOnly:
                scope = AdminScope.SeeOnly;
                return true;
            case Operate:
                scope = AdminScope.Operate;
                return true;
            case Policy:
                scope = AdminScope.Policy;
                return true;
            case Mailbox:
                scope = AdminScope.Mailbox;
                return true;
            case Compliance:
                scope = AdminScope.Compliance;
                return true;
            case AuditObligation:
                scope = AdminScope.AuditObligation;
                return true;
            default:
                return false;
        }
    }

    public static IReadOnlySet<AdminScope> ScopesForRole(AdminRole role)
        => role switch
        {
            AdminRole.TenantAdmin => All.ToHashSet(),
            AdminRole.MailboxAdmin => new HashSet<AdminScope>
            {
                AdminScope.SeeOnly,
                AdminScope.Mailbox,
                AdminScope.AuditObligation,
            },
            AdminRole.PolicyAdmin => new HashSet<AdminScope>
            {
                AdminScope.SeeOnly,
                AdminScope.Policy,
                AdminScope.AuditObligation,
            },
            AdminRole.ComplianceAdmin => new HashSet<AdminScope>
            {
                AdminScope.SeeOnly,
                AdminScope.Compliance,
                AdminScope.AuditObligation,
            },
            AdminRole.OperationsAdmin => new HashSet<AdminScope>
            {
                AdminScope.SeeOnly,
                AdminScope.Operate,
                AdminScope.AuditObligation,
            },
            _ => new HashSet<AdminScope>(),
        };

    public static string ToWireValue(AdminScope scope)
        => scope switch
        {
            AdminScope.SeeOnly => SeeOnly,
            AdminScope.Operate => Operate,
            AdminScope.Policy => Policy,
            AdminScope.Mailbox => Mailbox,
            AdminScope.Compliance => Compliance,
            AdminScope.AuditObligation => AuditObligation,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported admin scope."),
        };
}
