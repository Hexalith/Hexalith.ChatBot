namespace Hexalith.ChatBot.Contracts.Enums;

public static class AdminRoles
{
    public const string TenantAdmin = "tenant-admin";
    public const string MailboxAdmin = "mailbox-admin";
    public const string PolicyAdmin = "policy-admin";
    public const string ComplianceAdmin = "compliance-admin";
    public const string OperationsAdmin = "operations-admin";

    public static IReadOnlyList<AdminRole> All { get; } =
    [
        AdminRole.TenantAdmin,
        AdminRole.MailboxAdmin,
        AdminRole.PolicyAdmin,
        AdminRole.ComplianceAdmin,
        AdminRole.OperationsAdmin,
    ];

    public static bool TryFromWireValue(string? value, out AdminRole role)
    {
        role = AdminRole.TenantAdmin;
        switch (value?.Trim().ToLowerInvariant())
        {
            case TenantAdmin:
                role = AdminRole.TenantAdmin;
                return true;
            case MailboxAdmin:
                role = AdminRole.MailboxAdmin;
                return true;
            case PolicyAdmin:
                role = AdminRole.PolicyAdmin;
                return true;
            case ComplianceAdmin:
                role = AdminRole.ComplianceAdmin;
                return true;
            case OperationsAdmin:
                role = AdminRole.OperationsAdmin;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(AdminRole role)
        => role switch
        {
            AdminRole.TenantAdmin => TenantAdmin,
            AdminRole.MailboxAdmin => MailboxAdmin,
            AdminRole.PolicyAdmin => PolicyAdmin,
            AdminRole.ComplianceAdmin => ComplianceAdmin,
            AdminRole.OperationsAdmin => OperationsAdmin,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported admin role."),
        };
}
