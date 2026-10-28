using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Models generated scalar and collection converter attributes alongside recursive collection shapes.
/// </summary>
internal sealed class RecursiveEnumCarrier
{
    /// <summary>
    /// Gets or sets the scalar state.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public PermissiveWireEnum State { get; set; }

    /// <summary>
    /// Gets or sets the states with a generated item converter.
    /// </summary>
    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<PermissiveWireEnum?>? States { get; set; }

    /// <summary>
    /// Gets or sets dictionary values with a generated item converter.
    /// </summary>
    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public IDictionary<string, PermissiveWireEnum>? ByName { get; set; }

    /// <summary>
    /// Gets or sets recursive enum-keyed values.
    /// </summary>
    public IDictionary<PermissiveWireEnum, List<PermissiveWireEnum?>>? ByState { get; set; }
}
