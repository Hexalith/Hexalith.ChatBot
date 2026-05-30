namespace Hexalith.ChatBot.Server.Audit;

internal interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
