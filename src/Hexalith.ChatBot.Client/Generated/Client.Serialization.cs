using Newtonsoft.Json;
namespace Hexalith.ChatBot.Client.Generated;

/// <summary>
/// Partial client hook that installs the strict enum wire policy.
/// <para>
/// Ownership: hand-maintained beside NSwag output under <c>Generated/</c>. NSwag regenerates only
/// <c>HexalithChatBotClient.g.cs</c>; do not delete this file in a Generated wipe.
/// </para>
/// </summary>
public partial class Client
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.ContractResolver = new StrictEnumContractResolver();
        settings.Converters ??= [];
        settings.Converters.Add(new StrictStringEnumConverter());
    }
}
