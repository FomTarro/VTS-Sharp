using System.Threading.Tasks;

namespace VTS.Core.Examples {
    public static class MyFirstPlugin {
        public static async Task Main(string[] args) {
            var logger = new ConsoleVTSLoggerImpl();
            var websocket = new WebSocketImpl(logger);
            var jsonUtility = new NewtonsoftJsonUtilityImpl();
            var tokenStorage = new TokenStorageImpl("");

            var plugin = new CoreVTSPlugin(logger, 100, "My first plugin", "My Name", "");

            try {
                await plugin.InitializeAsync(websocket, jsonUtility, tokenStorage, () => logger.LogWarning("Disconnected!"));
                logger.Log("Connected!");

                var apiState = await plugin.GetAPIState();
                logger.Log("Using VTubeStudio " + apiState.data.vTubeStudioVersion);

                var currentModel = await plugin.GetCurrentModel();
                logger.Log("The current model is: " + currentModel.data.modelName);
            } catch (VTSException e) {
                logger.LogError(e);
            }
        }
    }
}