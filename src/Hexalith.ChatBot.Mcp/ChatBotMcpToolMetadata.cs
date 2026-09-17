namespace Hexalith.ChatBot.Mcp;

public sealed record ChatBotMcpToolMetadata(
    string Name,
    string ContractName,
    bool StateChanging,
    IReadOnlyList<string> RequiredArguments,
    IReadOnlyList<string> OptionalArguments,
    string Description)
{
    public const string ExposureMarker = "mcp-exposed";

    public IReadOnlyList<string> Tags { get; } = [ExposureMarker];

    public bool AllowsArgument(string argument)
        => RequiredArguments.Contains(argument, StringComparer.Ordinal)
            || OptionalArguments.Contains(argument, StringComparer.Ordinal);
}
