using VTS.Core.Examples.Advanced.Models;
using VTS.Core.Examples.Advanced.Services;

namespace VTS.Core.Examples.Advanced;

public class Plugin(IServiceProvider services, VTSLogger logger, PluginInfo pluginInfo)
{
    public async void Start()
    {
        WebSocketImpl websocket = new(logger);
        NewtonsoftJsonUtilityImpl jsonUtility = new();
        TokenStorageImpl tokenStorage = new("");
        CoreVTSPlugin plugin = new(logger, pluginInfo.Value.UpdateInterval, pluginInfo.Value.PluginName, pluginInfo.Value.PluginAuthor, pluginInfo.Value.PluginIcon);
        logger.Log($"Plugin Version: {pluginInfo.Value.PluginVersion}");
        try {
            await plugin.InitializeAsync(websocket, jsonUtility, tokenStorage, () => logger.LogWarning("Disconnected!")); 
            logger.Log("Connected!");
        } catch (VTSException e) {
            logger.LogError(e); // VTS probably isn't running
        }
        SubscribeToEvents(plugin, logger);
        
        await LogVtsInfo(plugin, logger);
    }
    private static async Task LogVtsInfo(CoreVTSPlugin plugin, VTSLogger logger)
    {
        var apiState = await plugin.GetAPIState();
        logger.Log($"Using VTubeStudio {apiState.data.vTubeStudioVersion}");

        var currentModel = await plugin.GetCurrentModel();
        logger.Log($"The current model is: {currentModel.data.modelName}");
    }
    
    private void SubscribeToEvents(CoreVTSPlugin plugin, VTSLogger logger)
    {
        plugin.SubscribeToBackgroundChangedEvent((backgroundInfo) =>
        {
            logger.Log($"The background was changed to: {backgroundInfo.data.backgroundName}");
        });
    }
}