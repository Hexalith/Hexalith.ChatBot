namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>A fully parsed metadata record from one category in the deterministic recovery dataset.</summary>
internal sealed record RecoveryDatasetRecord(
    string Kind,
    string Reference,
    string StructuralState,
    long? Sequence = null,
    long? SourceVersion = null);
