namespace Hexalith.ChatBot.Server.Adapters.OwnerServices;

/// <summary>Reports bounded owner authentication failures without credential or claim values.</summary>
internal static partial class OwnerServiceAuthenticationLog
{
    /// <summary>Records only the owner name and failed token check.</summary>
    [LoggerMessage(1311, LogLevel.Warning, "Owner {Owner} token rejected at {Check}")]
    internal static partial void TokenRejected(ILogger logger, string owner, string check);

    /// <summary>Records only the owner name and acquisition exception type.</summary>
    [LoggerMessage(1312, LogLevel.Warning, "Owner {Owner} token acquisition failed with {ExceptionType}")]
    internal static partial void AcquisitionFailed(ILogger logger, string owner, string exceptionType);
}
