namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>
/// Runs the tracked realm-template mutation alone, so no parallel AppHost start reads the temporarily mutated file.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RealmTemplateMutationCollection
{
    public const string Name = "realm-template-mutation";
}
