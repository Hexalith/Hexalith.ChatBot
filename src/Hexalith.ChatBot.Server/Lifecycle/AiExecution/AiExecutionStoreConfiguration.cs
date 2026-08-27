namespace Hexalith.ChatBot.Server.Lifecycle.AiExecution;

internal static class AiExecutionStoreConfiguration
{
    public static void RequireDurableStore(bool isProduction, bool useDaprStateStores)
    {
        if (isProduction && !useDaprStateStores)
        {
            throw new InvalidOperationException(
                "Production ChatBot deployments require ChatBot:UseDaprStateStores=true so AI execution leases, "
                + "recovery state, and terminal delivery survive restart and replica failover.");
        }
    }
}
