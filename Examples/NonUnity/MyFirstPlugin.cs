using VTS.Core;

namespace VTS.NonUnity;

public static class MyFirstPlugin
{
    public static async Task Main(string[] args)
    {
        var logger = new ConsoleVTSLoggerImpl();
        var websocket = new WebSocketNetCoreImpl(logger);
        var jsonUtility = new NewtonsoftJsonUtilityImpl();
        var tokenStorage = new TokenStorageImpl("");

        var plugin = new CoreVTSPlugin(logger, 100, "My first plugin", "My Name", "");

        try
        {
            await plugin.InitializeAsync(websocket, jsonUtility, tokenStorage, () => logger.LogWarning("Disconnected!"));

            logger.Log("Connected!");
        }
        catch (VTSException e)
        {
            logger.LogError(e);
        }
    }
}