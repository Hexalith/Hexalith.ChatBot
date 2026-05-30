using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.ChatBot.Client.Generated;

namespace Hexalith.ChatBot.Server.Gateway;

internal sealed class CommandSubmissionWireRequest
{
    [JsonPropertyName("commandId")]
    public string? CommandId { get; set; }

    [JsonPropertyName("commandType")]
    public string? CommandType { get; set; }

    [JsonPropertyName("command")]
    public JsonElement Command { get; set; }

    [JsonPropertyName("requestSchemaVersion")]
    public string? RequestSchemaVersion { get; set; }

    public CommandSubmissionRequest ToGeneratedRequest()
        => new()
        {
            CommandId = CommandId ?? string.Empty,
            CommandType = CommandType ?? string.Empty,
            Command = Command,
            RequestSchemaVersion = CommandSubmissionRequestRequestSchemaVersion.V1,
        };
}
