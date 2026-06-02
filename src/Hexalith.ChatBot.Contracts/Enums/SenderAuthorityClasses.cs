namespace Hexalith.ChatBot.Contracts.Enums;

public static class SenderAuthorityClasses
{
    public static IReadOnlyList<string> AllWireValues { get; } =
    [
        "draft-only",
        "authenticated-user send",
        "shared-mailbox send",
        "send-on-behalf",
        "approved service-send",
    ];

    public static bool TryFromWireValue(string? value, out SenderAuthorityClass authorityClass)
    {
        authorityClass = SenderAuthorityClass.DraftOnly;
        switch (value?.Trim().ToLowerInvariant())
        {
            case "draft-only":
                authorityClass = SenderAuthorityClass.DraftOnly;
                return true;
            case "authenticated-user send":
                authorityClass = SenderAuthorityClass.AuthenticatedUserSend;
                return true;
            case "shared-mailbox send":
                authorityClass = SenderAuthorityClass.SharedMailboxSend;
                return true;
            case "send-on-behalf":
                authorityClass = SenderAuthorityClass.SendOnBehalf;
                return true;
            case "approved service-send":
                authorityClass = SenderAuthorityClass.ApprovedServiceSend;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(SenderAuthorityClass authorityClass)
        => authorityClass switch
        {
            SenderAuthorityClass.DraftOnly => "draft-only",
            SenderAuthorityClass.AuthenticatedUserSend => "authenticated-user send",
            SenderAuthorityClass.SharedMailboxSend => "shared-mailbox send",
            SenderAuthorityClass.SendOnBehalf => "send-on-behalf",
            SenderAuthorityClass.ApprovedServiceSend => "approved service-send",
            _ => "draft-only",
        };
}
