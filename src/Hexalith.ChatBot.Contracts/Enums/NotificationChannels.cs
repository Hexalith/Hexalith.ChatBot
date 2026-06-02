namespace Hexalith.ChatBot.Contracts.Enums;

/// <summary>
/// Wire-token companion for <see cref="NotificationChannel"/>. The declared channel tokens are the only ones the
/// routing schema accepts after the trust boundary.
/// </summary>
public static class NotificationChannels
{
    public const string InApp = "in-app";
    public const string Email = "email";
    public const string Webhook = "webhook";
    public const string OperatorAlert = "operator-alert";

    public static IReadOnlyList<NotificationChannel> All { get; } =
    [
        NotificationChannel.InApp,
        NotificationChannel.Email,
        NotificationChannel.Webhook,
        NotificationChannel.OperatorAlert,
    ];

    public static bool TryFromWireValue(string? value, out NotificationChannel channel)
    {
        channel = NotificationChannel.InApp;
        switch (value?.Trim().ToLowerInvariant())
        {
            case InApp:
                channel = NotificationChannel.InApp;
                return true;
            case Email:
                channel = NotificationChannel.Email;
                return true;
            case Webhook:
                channel = NotificationChannel.Webhook;
                return true;
            case OperatorAlert:
                channel = NotificationChannel.OperatorAlert;
                return true;
            default:
                return false;
        }
    }

    public static string ToWireValue(NotificationChannel channel)
        => channel switch
        {
            NotificationChannel.InApp => InApp,
            NotificationChannel.Email => Email,
            NotificationChannel.Webhook => Webhook,
            NotificationChannel.OperatorAlert => OperatorAlert,
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, "Unsupported notification channel."),
        };
}
