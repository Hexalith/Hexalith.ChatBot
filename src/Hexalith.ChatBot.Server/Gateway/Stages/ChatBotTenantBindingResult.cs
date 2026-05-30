namespace Hexalith.ChatBot.Server.Gateway.Stages;

internal sealed record ChatBotTenantBindingResult(ChatBotTenantBinding? Binding, string ReasonCode, bool IsBound)
{
    public static ChatBotTenantBindingResult Bound(ChatBotTenantBinding binding)
        => new(binding, string.Empty, true);

    public static ChatBotTenantBindingResult Denied(string reasonCode)
        => new(null, reasonCode, false);

    public static ChatBotTenantBindingResult Denied(ChatBotTenantBinding binding, string reasonCode)
        => new(binding, reasonCode, false);
}
