namespace Hexalith.ChatBot.Server.Gateway;

internal interface IUserFacingMessageTelemetry
{
    void RecordUncategorizedMessage(string catalogVersion, string fallbackCode);
}
