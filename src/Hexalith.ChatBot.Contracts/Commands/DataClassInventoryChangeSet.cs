using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The proposed classification set for a <see cref="SubmitDataClassInventoryChange"/>. Mirrors
/// <see cref="RetentionConfigurationChangeSet"/>.
/// </summary>
public sealed record DataClassInventoryChangeSet(
    IReadOnlyList<DataClassClassification> Classifications);
