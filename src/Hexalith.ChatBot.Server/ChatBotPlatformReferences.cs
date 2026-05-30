using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.ChatBot.Server;

internal static class ChatBotPlatformReferences
{
    public static Type EventPayloadContractType => typeof(IEventPayload);

    public static string SystemTenantId => TenantIdentity.DefaultTenantId;
}
