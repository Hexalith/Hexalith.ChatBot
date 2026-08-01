using Newtonsoft.Json;
namespace Hexalith.ChatBot.Client.Generated;

public partial class Client
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.ContractResolver = new StrictEnumContractResolver();
        settings.Converters.Add(new StrictStringEnumConverter());
    }
}
