namespace Hexalith.ChatBot.StoryEvidenceGate;

/// <summary>
/// Identifies one explicit completion transition and its evidence contract.
/// </summary>
/// <param name="StoryPath">The explicit story path.</param>
/// <param name="ContractPath">The matching evidence contract path.</param>
/// <param name="StoryKey">The explicit contract story key.</param>
public sealed record TransitionRecord(string StoryPath, string ContractPath, string StoryKey);
