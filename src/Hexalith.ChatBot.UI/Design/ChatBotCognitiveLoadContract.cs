namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// UX-DR41 cognitive-load contract for governed workflow rows and panels.
/// </summary>
public sealed record ChatBotCognitiveLoadContract(
    string SummaryText,
    string RawIdentifier,
    IReadOnlyList<string> FieldsInDisplayOrder,
    IReadOnlyList<ChatBotWorkflowItemActionContract> Actions,
    string ActiveFilterSummary,
    int? ResultCount,
    string ConsolidatedStateMessage)
{
    public static IReadOnlyList<string> CanonicalFieldOrder { get; } = ["Evidence", "Risk", "Status", "Actor", "Timestamp"];

    public static IReadOnlyList<string> AppliesToSurfaces { get; } = ["candidate rows", "proposals", "queues", "audit entries"];

    /// <summary>Gets a value indicating whether exactly one primary action exists.</summary>
    public bool HasExactlyOnePrimaryAction
        => Actions is not null
            && Actions.Count(action => action.Kind is ChatBotWorkflowItemActionKind.Primary) == 1;

    /// <summary>Gets a value indicating whether action groups appear primary, secondary, then destructive.</summary>
    public bool HasCanonicalActionGrouping
        => Actions is not null
            && Actions.All(static action => action.IsComplete)
            && Actions.Select(static action => action.Kind).SequenceEqual(
                Actions.Select(static action => action.Kind).OrderBy(static kind => kind));

    /// <summary>Gets a value indicating whether plain-language summary text is present before raw identifiers.</summary>
    public bool HasSummaryBeforeRawIdentifier
        => !string.IsNullOrWhiteSpace(SummaryText)
            && !string.IsNullOrWhiteSpace(RawIdentifier)
            && !SummaryText.Contains(RawIdentifier, StringComparison.Ordinal);

    /// <summary>Gets a value indicating whether canonical field order is preserved.</summary>
    public bool HasCanonicalFieldOrder
        => FieldsInDisplayOrder is not null
            && FieldsInDisplayOrder.Take(CanonicalFieldOrder.Count).SequenceEqual(CanonicalFieldOrder, StringComparer.Ordinal);

    /// <summary>Gets a value indicating whether the cognitive-load contract is complete.</summary>
    public bool IsComplete
        => HasExactlyOnePrimaryAction
            && HasCanonicalActionGrouping
            && HasSummaryBeforeRawIdentifier
            && HasCanonicalFieldOrder
            && !string.IsNullOrWhiteSpace(ActiveFilterSummary)
            && ResultCount >= 0
            && !string.IsNullOrWhiteSpace(ConsolidatedStateMessage);
}
