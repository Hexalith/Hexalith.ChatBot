namespace Hexalith.ChatBot.IntegrationTests.Recovery;

/// <summary>Shares the local realm's validator request between token acquisition and the Identity outage probe.</summary>
internal static class RecoveryValidatorIdentity
{
    /// <summary>Creates an independently owned form for the dedicated local-development fixture identity.</summary>
    /// <returns>The token endpoint's validator form fields.</returns>
    public static Dictionary<string, string> CreateTokenRequestFields()
        => new()
        {
            ["grant_type"] = "password",
            ["client_id"] = "hexalith-chatbot",
            ["username"] = "recovery-validator",
            ["password"] = "recovery-validator-pass",
            ["scope"] = "openid",
        };
}
