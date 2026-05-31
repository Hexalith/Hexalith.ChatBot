namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Validation summary and invalid-field association metadata for governed UI forms.
/// </summary>
/// <param name="SummaryId">Validation summary id.</param>
/// <param name="SummaryLabel">Validation summary accessible label.</param>
/// <param name="FocusTargetId">Element that receives focus after validation failure.</param>
/// <param name="AffectedFieldIds">Invalid field ids.</param>
/// <param name="FieldMessageIds">Mapping from invalid field id to field message id.</param>
/// <param name="SafeNextAction">Safe next action exposed with the validation failure.</param>
public sealed record ChatBotValidationErrorContract(
    string SummaryId,
    string SummaryLabel,
    string FocusTargetId,
    IReadOnlyList<string> AffectedFieldIds,
    IReadOnlyDictionary<string, string> FieldMessageIds,
    string SafeNextAction)
{
    /// <summary>Gets a value indicating whether invalid controls must carry aria-invalid.</summary>
    public bool RequiresInvalidFields => AffectedFieldIds is { Count: > 0 };

    /// <summary>Gets a value indicating whether invalid controls must reference field messages.</summary>
    public bool RequiresMessageAssociation
        => AffectedFieldIds is { Count: > 0 }
            && FieldMessageIds is { Count: > 0 }
            && AffectedFieldIds.All(fieldId =>
                !string.IsNullOrWhiteSpace(fieldId)
                && FieldMessageIds.TryGetValue(fieldId, out string? messageId)
                && !string.IsNullOrWhiteSpace(messageId));

    /// <summary>Gets a value indicating whether validation focus and field metadata is complete.</summary>
    public bool IsComplete
        => !string.IsNullOrWhiteSpace(SummaryId)
            && !string.IsNullOrWhiteSpace(SummaryLabel)
            && FocusTargetId == SummaryId
            && RequiresInvalidFields
            && RequiresMessageAssociation
            && !string.IsNullOrWhiteSpace(SafeNextAction);
}
