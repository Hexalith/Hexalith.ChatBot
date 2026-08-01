namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Serializes the destructive recovery topology so resource commands and fixed DAPR ports never overlap.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class LiveRecoveryValidationCollection
{
    public const string Name = "live-recovery-validation";
}
