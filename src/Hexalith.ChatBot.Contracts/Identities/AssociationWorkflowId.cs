namespace Hexalith.ChatBot.Contracts.Identities;

/// <summary>
/// ChatBot-owned ULID identity for one association scoring workflow.
/// </summary>
public readonly record struct AssociationWorkflowId
{
    private AssociationWorkflowId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AssociationWorkflowId New()
        => new(ChatBotIdentity.NewUlid());

    public static bool TryParse(string? value, out AssociationWorkflowId associationId)
    {
        if (ChatBotIdentity.TryNormalizeUlid(value, out string? normalized))
        {
            associationId = new AssociationWorkflowId(normalized);
            return true;
        }

        associationId = default;
        return false;
    }
}
