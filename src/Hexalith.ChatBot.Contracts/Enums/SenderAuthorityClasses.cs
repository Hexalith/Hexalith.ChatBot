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

    public static IReadOnlyList<SenderAuthorityClass> All { get; } =
    [
        SenderAuthorityClass.DraftOnly,
        SenderAuthorityClass.AuthenticatedUserSend,
        SenderAuthorityClass.SharedMailboxSend,
        SenderAuthorityClass.SendOnBehalf,
        SenderAuthorityClass.ApprovedServiceSend,
    ];

    /// <summary>
    /// Deterministic affected-party authority rank used by Story 7.8 prioritization:
    /// draft-only(0) &lt; authenticated-user-send(1) &lt; shared-mailbox-send(2) &lt; send-on-behalf(3) &lt;
    /// approved-service-send(4).
    /// </summary>
    public static int Rank(SenderAuthorityClass authorityClass)
        => authorityClass switch
        {
            SenderAuthorityClass.DraftOnly => 0,
            SenderAuthorityClass.AuthenticatedUserSend => 1,
            SenderAuthorityClass.SharedMailboxSend => 2,
            SenderAuthorityClass.SendOnBehalf => 3,
            SenderAuthorityClass.ApprovedServiceSend => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(authorityClass), authorityClass, "Unsupported sender authority class."),
        };

    /// <summary>Returns <see langword="true"/> when <paramref name="candidate"/> meets or exceeds <paramref name="threshold"/>.</summary>
    public static bool MeetsOrExceeds(SenderAuthorityClass candidate, SenderAuthorityClass threshold)
        => Rank(candidate) >= Rank(threshold);

    /// <summary>
    /// Resolves the authority class from a wire token, collapsing any unknown/undeclared value onto the lowest declared
    /// rank (<see cref="SenderAuthorityClass.DraftOnly"/>) — fail-safe, never fail-open to top priority.
    /// </summary>
    public static SenderAuthorityClass FromWireValueOrLowest(string? value)
        => TryFromWireValue(value, out SenderAuthorityClass authorityClass) ? authorityClass : SenderAuthorityClass.DraftOnly;

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
