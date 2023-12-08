using Microsoft.Extensions.Hosting;
using VTS.Core;

// This is a simple example of how to use the VTS plugin in C#.
// You can use this as a starting point for your own plugin implementation

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args); // Create a host builder so the program doesn't exit immediately

ConsoleVTSLoggerImpl logger = new(); // Create a logger to log messages to the console (you can use your own logger implementation here like in the Advanced example)

CoreVTSPlugin plugin = new(logger, 100, "My simple plugin", "Perfect Programmer", "");
try 
{
    await plugin.InitializeAsync(
        new WebSocketImpl(logger), 
        new NewtonsoftJsonUtilityImpl(), 
        new TokenStorageImpl(""), 
        () => logger.LogWarning("Disconnected!"));
    logger.Log("Connected!");
    var apiState = await plugin.GetAPIState();
    
    logger.Log("Using VTubeStudio " + apiState.data.vTubeStudioVersion);
    var currentModel = await plugin.GetCurrentModel();
    
    logger.Log("The current model is: " + currentModel.data.modelName);
    
    // Subscribe to your events here using the plugin.SubscribeTo* methods
    await plugin.SubscribeToBackgroundChangedEvent((backgroundInfo) => {
        logger.Log("The background was changed to: " + backgroundInfo.data.backgroundName);
    });
    // To unsubscribe, use the plugin.UnsubscribeFrom* methods
} 
catch (VTSException error) {
    logger.LogError(error); // Log any errors that occur during initialization
}

var host = builder.Build(); // Build the host
await host.RunAsync(); // This will keep the program running until the user presses Ctrl+C, or kills the process