using Hexalith.ChatBot.Contracts.Enums;

namespace Hexalith.ChatBot.Contracts.Commands;

/// <summary>
/// The versioned data-class inventory artifact (NFR23/NFR53). Carries <see cref="Owner"/>, <see cref="Version"/>,
/// <see cref="LastReviewedAtUtc"/> (quarterly-review obligation), <see cref="SchemaVersion"/>, and the complete
/// classification set — which <see cref="DataClassInventorySchema.Validate"/> asserts is a bijection over the
/// canonical class set (every class classified exactly once, none unclassified).
/// </summary>
public sealed record DataClassInventory(
    string Owner,
    string Version,
    DateTimeOffset LastReviewedAtUtc,
    string SchemaVersion,
    IReadOnlyList<DataClassClassification> Classifications);
