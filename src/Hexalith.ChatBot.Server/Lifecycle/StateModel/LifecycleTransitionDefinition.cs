namespace Hexalith.ChatBot.Server.Lifecycle.StateModel;

internal sealed record LifecycleTransitionDefinition(string From, string To)
{
    public override string ToString()
        => $"{From}->{To}";
}
