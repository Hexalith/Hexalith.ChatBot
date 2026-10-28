namespace Hexalith.ChatBot.Client.Tests;

/// <summary>
/// Models initialized mutable dictionaries populated through writable and get-only properties.
/// </summary>
internal sealed class InitializedEnumDictionaryCarrier
{
    /// <summary>
    /// Gets the initialized dictionary that must be populated without a property setter.
    /// </summary>
    public IDictionary<PermissiveWireEnum, string> GetOnlyStates { get; } =
        new Dictionary<PermissiveWireEnum, string> { [PermissiveWireEnum.Primary] = "retained" };

    /// <summary>
    /// Gets or sets the initialized dictionary that the default creation policy reuses.
    /// </summary>
    public IDictionary<PermissiveWireEnum, string> WritableStates { get; set; } =
        new Dictionary<PermissiveWireEnum, string> { [PermissiveWireEnum.Primary] = "retained" };
}
