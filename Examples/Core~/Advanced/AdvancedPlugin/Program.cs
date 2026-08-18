using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VTS.Core.Examples.Advanced;
using VTS.Core.Examples.Advanced.Models;
using VTS.Core.Examples.Advanced.Services;

// Not yet AOT compatible

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args); // Create a new HostBuilder

// Configuration is optional, but recommended
builder.Configuration.AddJsonFile("appsettings.json", optional: true);

builder.Services.AddSingleton<VTSLogger>(); // Add the ILogger implementation to show what is going on inside the plugin
builder.Services.AddSingleton<PluginInfo>(); // Add the PluginInfo implementation adds your plugin's information to the plugin list in VTube Studio
builder.Services.AddSingleton<Plugin>(); // Add the Plugin implementation to start your plugin

var host = builder.Build(); // Build the host

var plugin = host.Services.GetRequiredService<Plugin>(); // Get the plugin from the service provider
plugin.Start(); // Start the plugin

await host.RunAsync(); // This will keep the program running until the user presses Ctrl+C, or kills the process


