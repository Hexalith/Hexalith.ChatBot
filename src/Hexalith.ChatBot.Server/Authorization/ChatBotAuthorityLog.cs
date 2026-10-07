namespace Hexalith.ChatBot.Server.Authorization;

/// <summary>Emits bounded owner diagnostics without exception messages or owner data.</summary>
internal static partial class ChatBotAuthorityLog
{
    /// <summary>Records a normalized owner failure using metadata only.</summary>
    [LoggerMessage(1301, LogLevel.Warning, "Owner {Owner} unavailable for {Operation}: {ExceptionType}")]
    public static partial void OwnerUnavailable(ILogger logger, string owner, string operation, string exceptionType);
}
