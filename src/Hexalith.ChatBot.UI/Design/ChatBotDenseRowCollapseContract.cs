namespace Hexalith.ChatBot.UI.Design;

/// <summary>
/// Dense-row collapse contract that keeps safety-critical governed labels reachable.
/// </summary>
public static class ChatBotDenseRowCollapseContract
{
    /// <summary>Gets labels that must remain visible or move to an explicit detail surface.</summary>
    public static IReadOnlyList<string> RequiredSafetyLabels { get; } =
    [
        "Project",
        "Actor",
        "Risk",
        "State",
        "Confidence",
        "Time",
        "Reason",
        "Next action",
    ];

    /// <summary>Gets default dense row field retention metadata for future governed row fixtures.</summary>
    public static IReadOnlyList<ChatBotDenseRowField> DefaultFields { get; } =
    [
        new("Project", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Actor", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Risk", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("State", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Confidence", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Time", ChatBotDenseRowFieldRetention.MustMoveToDetail),
        new("Reason", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Next action", ChatBotDenseRowFieldRetention.MustKeepVisible),
        new("Raw ID", ChatBotDenseRowFieldRetention.CollapseFirst),
        new("Secondary timestamp", ChatBotDenseRowFieldRetention.CollapseFirst),
        new("Repeated context", ChatBotDenseRowFieldRetention.CollapseFirst),
    ];

    /// <summary>Determines whether the field may be dropped from the phone collapsed row.</summary>
    /// <param name="field">Dense row field.</param>
    /// <returns><see langword="true"/> when the field is a first collapse candidate.</returns>
    public static bool CanDropFromPhoneRow(ChatBotDenseRowField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.Retention is ChatBotDenseRowFieldRetention.CollapseFirst;
    }
}
