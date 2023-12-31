# VTS-Sharp v2.2.2
A C# client interface for creating VTube Studio Plugins with the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio), for use in Unity and other C# runtime environments!

## IMPORTANT! 
If you are updating your project from using a 1.x.x version of the library to using a 2.x.x version of the library, please read [the migration guide](#what-changed-in-200), as the project was restructured in version 2.0.0 to decouple it from the Unity Engine, allowing it to be more easily used in other C# environments!
 
## About
This library is maintained by Tom "Skeletom" Farro. If you need to contact him, the best way to do so is via [Twitter](https://www.twitter.com/fomtarro) or by leaving an issue ticket on this repo.

If you're more of an email-oriented person, you can contact his support email: [tom@skeletom.net](mailto:tom@skeletom.net).

This library can also be found on the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/vts-sharp-203218), and the core library (with no Unity-specific components) can be downloaded as a [NuGet Package](https://www.nuget.org/packages/VTS-Sharp/2.1.0), but this repo will always be the most up-to-date version.
 
## Usage
In order to start making a plugin, follow these simple steps:
1. [Check to see which packages from this library you need for your project based on your C# environment](#packages). 
    * If you are using Unity, make a class which extends `VTS.Unity.UnityVTSPlugin`. 
    * If you are using any other C# environment, make a class which extends `VTS.Core.CoreVTSPlugin`, or which has one as a member variable. 
2. In your class, call the [`Initialize`](#void-initialize) method on the plugin, which will attempt to connect to VTube Studio and authenticate the plugin. 
    
    This method has you pass in your preferred implementations of:
    * A [JSON utility](#interface-ijsonutility)
    * A [websocket](#interface-iwebsocket)
    * A [mechanism to store the authorization token](#interface-itokenstorage). 
    
    It also has you specify what happens when:
    * The plugin connects to VTube Studio successfully
    * The plugin disconnects from VTube Studio
    * The plugin encounters an error while attempting to connect to VTube Studio 
3. Once your plugin is authenticated, you can call any method found in the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio), which are built-in as appropriately named methods on the `VTSPlugin` class!

And that's it! In fact, if you're using Unity, these steps are all done for you already, out of the box, in the `MyFirstPlugin` class found in the `Examples/Unity` folder!.
 
You can find a video tutorial that demonstrates [how to get started in under 90 seconds here](https://www.youtube.com/watch?v=lUGeMEVzjAU).

## Design Pattern and Considerations
 
### Swappable Components
In order to afford the most flexibility (and to be as decoupled from Unity as possible), the underlying components of the [`VTSPlugin`](#interface-ivtsplugin) are all defined as interfaces. This allows you to swap out the built-in implementations with more robust or platform-compliant ones. By default, the `MyFirstPlugin` class features working implementations of all needed components, but if you want, you can pass in your own implementations via the [`Initialize`](#void-initialize) method.

### Asynchronous Design
Because the VTube Studio API is websocket-based, all calls to it are inherently asynchronous. As of version 2.1.0, there are now two design patterns included in this library. You can use the one that suits your perferences and needs the best!

This library also supports the [VTube Studio Event Subscription API](https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md). With this feature, you can subscribe to various events to make sure your plugin gets a message when something happens in VTube Studio. Event Subscription follows a similar asynchronous design pattern.

### Callback-based Design Pattern
The initial implementation of this library revolves around callbacks to achieve asynchronous results.

#### API Calls
Take, for example, the following method signature, found in the [`VTSPlugin`](#interface-ivtsplugin) class:
 
```
void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError)
```
The method accepts two callbacks, `onSuccess` and `onError`, but does not return a value.

Upon the request being processed by VTube Studio,
one of these two callbacks will be invoked, depending on if the request was successful or not. The callback accepts in a single, strongly-typed argument reflecting the response payload. You can find what to expect in each payload class in the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio).

#### Event Subscription
Take, for example, the following method signature, found in the [`VTSPlugin`](#interface-ivtsplugin) class:

```
void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
```
The method accepts an optional configuration class, and three callbacks, `onEvent`, `onSubscribe` and `onError`, but does not return a value.

Upon successfully subscribing to the event in VTube Studio, the `onSubscribe` callback will be invoked, and then `onEvent` will be invoked any time VTube Studio publishes an event of that type. If the subscription fails for any reason, `onError` will be invoked.

### Async/Await-based Design Pattern
As of version 2.1.0, the library now supports the `async` and `await` pattern for asynchronous code. 

#### API Calls
Take, for example, the following method signature, found in the [`VTSPlugin`](#interface-ivtsplugin) class:

```
async Task<VTSStateData> GetAPIState()
```

This method will can be called like so:
```
var stateData = await plugin.GetApiState();
```

Upon the request being processed by VTube Studio, the method will resolve into a payload of `VTSStateData` if the request was successful, or it will throw a `VTSException` if the request failed for any reason.

#### Event Subscription
Take, for example, the following method signature, found in the [`VTSPlugin`](#interface-ivtsplugin) class:

```
async Task<VTSEventSubscriptionResponseData> SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent)
```
The method accepts an optional configuration class, and one callback, `onEvent`.

Upon successfully subscribing to the event in VTube Studio, the method will resolve into a payload of `VTSEventSubscriptionResponseData` and then `onEvent` will be invoked any time VTube Studio publishes an event of that type. If the subscription fails for any reason, a `VTSException` will be thrown.

## What Changed in 2.0.0?

### Packages
As of version 2.0.0, the library has been split into two folders/packages: `VTS/Core` and `VTS/Unity`. The `VTS/Core` folder contains everything needed to build a plugin in any C# runtime environment, with no engine-specific code. The `VTS/Unity` folder contains Unity-specific wrappers for the core classes, allowing you to easily build a plugin as a Unity GameObject, following the original design of this library. If you are not looking to use Unity for your project, you can completely discard the `VTS/Unity` folder. However, if you *are* using Unity for your project, you will need both the `VTS/Core` and `VTS/Unity` folders, as the Unity components serve as wrappers for the Core library.
 
### Breaking Changes
As of version 2.0.0, a few fundamental and breaking changes have been introduced in the interest of decoupling the library from Unity. If you are updating your project from using a 1.x.x version of the library to using a 2.x.x version of the library, please completely remove the library from your project, and re-import it while being aware of the following changes:

* Namespaces have been totally reorganized. The two remaining namespaces are `VTS.Core` and `VTS.Unity`. These correspond to the aformentioned [packages](#packages).
* The `VTSPlugin` MonoBehaviour class has been renamed to `UnityVTSPlugin`, and moved into the `VTS.Unity` namespace. As such, please update your plugin classes to extend `VTS.Unity.UnityVTSPlugin`.
* The `VTSWebSocket` MonoBehaviour class has been totally removed. You may safely remove it from any game objects. This class now exists as a pure C# equivalent.
* Several of the built-in dependencies of the `Initialize` method have changed in various ways:
    * The `IJsonUtility` interface now has a `NewtonsoftJsonUtilityImpl` implementation that is suitable for all uses, leveraging the much more robust [Newtonsoft JSON library](#newtonsoft-jsonnet).
    * The existing `ITokenStorage` implementation `TokenStorageImpl` now requires a root path to be passed in via the constructor. For Unity applications, you likely want to use `Application.persistentDataPath`, ex `new TokenStorageImpl(Application.persistentDataPath)`.
    * The existing `IWebSocket` implementation `WebSocketSharpImpl` now requires an `IVTSLogger` to be passed in via the constructor. For Unity applications, you can use the builtin logger from the `VTSPlugin` MonoBehaviour, ex `new WebSocketSharpImpl(this.Logger)`
    * The `onError` callback method now accepts an `VTSErrorData` argument.

# API

## `interface IVTSPlugin`

### Provided Implementations
* `VTS.Core.CoreVTSPlugin`
* `VTS.Unity.UnityVTSPlugin`

### Properties
#### `string PluginName`
The name of this plugin. Required for authorization purposes.
#### `string PluginAuthor`
The name of this plugin's author. Required for authorization purposes.
#### `string PluginIcon`
The icon of this for this plugin, as a base64 string. Optional, must be exactly 128*128 pixels in size.
#### `VTSWebSocket Socket`
The underlying WebSocket for connecting to VTS.
#### `ITokenStorage TokenStorage`
The underlying Token Storage mechanism for connecting to VTS.
#### `IJsonUtility JsonUtility`
The underlying JSON serializer/deserializer implementation.
#### `IVTSLogger Logger`
The underlying Logger implementation.
#### `bool IsAuthenticated`
Is the plugin currently authenticated?


### Methods
#### `void Initialize`
Connects to VTube Studio, authenticates the plugin, and also selects the WebSocket, JSON Utility, and Token Storage implementations. Takes the following args:
* `IWebSocket webSocket`: The WebSocket implementation.
* `IJsonUtility jsonUtility`: The JSON serializer/deserializer implementation.
* `ITokenStorage tokenStorage`: The Token Storage implementation.
* `Action onConnect`: Callback executed upon successful initialization.
* `Action onDisconnect`: Callback executed upon disconnecting from VTS (accidental or otherwise).
* `Action<VTSErrorData> onError`: Callback executed upon failed initialization.

The plugin will attempt to intelligently choose a port to connect to, using the following criteria:
* It will first attempt to connect to the designated port (8001 by default, can be manually set with [SetPort](#bool-setport)).
* If that fails, it will attempt to connect to the first port discovered by UDP.
* If that takes too long and times out, it will attempt to connect to the default port (8001).

#### `Task InitializeAsync`
Connects to VTube Studio, authenticates the plugin, and also selects the WebSocket, JSON Utility, and Token Storage implementations. Takes the following args:
* `IWebSocket webSocket`: The WebSocket implementation.
* `IJsonUtility jsonUtility`: The JSON serializer/deserializer implementation.
* `ITokenStorage tokenStorage`: The Token Storage implementation.
* `Action onDisconnect`: Callback executed upon disconnecting from VTS (accidental or otherwise).

If this method fails to execute, it will throw a `VTSException`.

#### `void Disconnect`
Disconnects from VTube Studio. Will fire the onDisconnect callback set via the Initialize method.

#### `Dictionary<int, VTSStateBroadcastData> GetPorts`
Generates a dictionary indexed by port number containing information about all available VTube Studio ports.

For more info, see [API Server Discovery (UDP) on the official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio#api-server-discovery-udp).

#### `bool SetPort`
Sets the connection port to the given number. Returns true if the number is a valid VTube Studio port, returns false otherwise.

If the port number is changed while an active connection exists, you will need to reconnect. Takes the following args:
* `int port` The port number to set.

#### `bool SetIPAddress`
Sets the connection IP address to the given string. Returns true if the string is a valid IP Address format, returns false otherwise.

If the IP Address is changed while an active connection exists, you will need to reconnect. Takes the following args:
* `string ipString` The string form of the IP address, in dotted-quad notation for IPv4.

#### `VTube Studio API Requests`
Request methods can be inferred from the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio).

#### `VTube Studio API Events`
Event subscription methods can be inferred from the [official VTube Studio Event Subscription API](https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md).


## `interface IWebSocket`

### Provided Implementations
* `VTS.Core.WebSocketSharpImpl`
* `VTS.Core.WebSocketImpl`

### Methods
#### `string GetNextResponse`
Fetches the next response to process.
#### `void Start`
Connects to the given URL and executes the relevant callback on completion. Takes the following args:
* `string URL`: URL to connect to.
* `Action onConnect`: Callback executed upon connecting to the URL.
* `Action onDisconnect`: Callback executed upon disconnecting from the URL (accidental or otherwise).
* `Action<Exception> onError`: Callback executed upon receiving an error.
#### `void Stop`
Closes the websocket. Executes the `onDisconnect` callback as specified in the `Start` method call.
#### `bool IsConnecting`
Is the socket in the process of connecting?
#### `bool IsConnectionOpen`
Has the socket successfully connected?
#### `void Send`
Send a payload to the websocket server. Takes the following args:
* `string message`: The payload to send.


## `interface IJsonUtility`

### Provided Implementations
* `VTS.Core.NewtonsoftJsonUtilityImpl`
* `VTS.Unity.UnityJsonUtilityImpl` (deprecated)

### Methods
#### `T FromJson<T>`
Deserializes a JSON string into an object of the specified type. Takes the following args:
* `T`: The type to deserialize into.
* `string json`: The JSON string.
#### `string ToJson`
Converts an object into a JSON string. Takes the following args:
* `object obj`: The object to serialize.


## `interface ITokenStorage`

### Provided Implementations
* `VTS.Core.TokenStorageImpl`

### Methods
#### `string LoadToken`
Loads the auth token.
#### `void SaveToken`
Saves an auth token. Takes the following args:
* `string token`: The token to save.
#### `void DeleteToken`
Deletes the auth token.


## `interface IVTSLogger`

### Provided Implementations
* `VTS.Core.ConsoleVTSLoggerImpl`
* `VTS.Unity.UnityVTSLoggerImpl`

### Methods
#### `void Log`
Logs a message. Takes the following args:
* `string message`: The message to log.
#### `void LogWarning`
Logs a warning. Takes the following args:
* `string warning`: The warning to log.
#### `void LogError`
Logs an error. Takes the following args:
* `string error`: The error to log.
#### `void LogError`
Logs an error. Takes the following args:
* `Exception error`: The exception to log.


# Acknowledgements

## [DenchiSoft](https://github.com/DenchiSoft/VTubeStudio)
None of this would be possible without Denchi's tireless work on VTube Studio itself.

## [WebSocketSharp](https://github.com/sta/websocket-sharp)
An implementation of IWebSocket using WebSocketSharp has been included for use, adhering to the [library's MIT license](https://github.com/sta/websocket-sharp/blob/master/LICENSE.txt).

## [Newtonsoft JSON.NET](https://www.newtonsoft.com/json)
An implementation of IJsonUtility using Newtonsoft's JSON.NET has been included for use, adhering to the [library's MIT license](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).


# Made With VTS-Sharp
Below is a list of some plugins which were made using this library! If you have made something you would like included on this list, please [send Tom a message](#about).

| Plugin | Developer | Explanation |
| --- | --- | --- |
| [PentabInfoPickerForVTS](https://github.com/Ganeesya/PentabInfoPickerForVTS) ([Video](https://www.youtube.com/watch?v=kklcAzTequ)) | [Ganeesya](https://twitter.com/Ganeesya) | An app which allows users to control their model with a tablet and pen. |
| [VTS-ChangeEyeColor](https://yataya2000.booth.pm/items/3551421) ([Github](https://github.com/TaniNatsumi/VTS-ChangeEyeColor)) | [TaniNatsumi](https://twitter.com/BURAI_VC2008) | An app which allows users to change the eye colors of their model. Can change each eye color individually (heterochromia).|
| [VTSLive](https://github.com/fastestyukkuri/VTSLivePlugins) | [fastest_yukkuri](https://twitter.com/fastest_yukkuri) | An app which allows VTube Studio to reflect the movement of analog and digital clocks, the movement of the sun and moon, and weather information from around the world.|
|[VTS-Mod](https://github.com/MechaWolfVtuberShin/VTS-Mod/releases/tag/vts-mod)| [MechaWolfVtuberShin](https://twitter.com/ShinChannel3) | An app which allows users to change the surface color of the model including RGB. It can also change the rotation of the model.|
|[Twitch Integrated Throwing System (T.I.T.S)](https://remasuri3.itch.io/tits) ([Video](https://www.youtube.com/watch?v=hWOIZqv-u50)) | [Remasuri3](https://twitter.com/Remasuri32) | An app which allows your chat to bully you as much as possible >:D It can be used with or without VTube Studio to let people throw items at your face!|
|[VTS-Heartrate](https://github.com/FomTarro/vts-heartrate) ([Video](https://www.youtube.com/watch?v=tV1kK0uSjFE))| [Skeletom](https://twitter.com/FomTarro) | An app which allows users to connect their heartrate data to VTube Studio, to cause their model to become flushed under stress and breathe more heavily, among other things. |
|[ViewLink](https://kawaentertainment.itch.io/viewlink)| [Kawa Entertainment](https://twitter.com/kawa_entertain) | An app for translating 3D VR movement into Live2D motion tracking, allowing you to stream VR games with your Live2D model. |
|[VBridger](https://store.steampowered.com/app/1898830/VBridger/) ([Video](https://www.youtube.com/watch?v=mFUG4L4Lgfo)) | [PiPuProductions](https://twitter.com/PiPuProductions) | An app designed for VTube Studio and Live2D, which allows the user to make better use of iPhoneX ARKit tracking on their Live2D model. |
|[Audiomimi](https://artemiz.booth.pm/items/3800791/) ([Video](https://twitter.com/ArtemizComs/status/1522834869247651840)) | [Artemiz](https://twitter.com/ArtemizComs) | An app that allows you to use SFX based on VTS parameter movement. |
|[VTS Desktop Audio](https://lualucky.itch.io/vts-desktop-audio-plugin) ([Video](https://twitter.com/LuaVLucky/status/1592741024509853696)) | [Lua V. Lucky](https://twitter.com/LuaVLucky) | An app that allows you to control your model with your desktop audio! Converts various parts of the audio spectrum to custom tracking parameters. |
|[Twitch High Intensity Color Changer (T.H.I.C.C)](https://remasuri3.itch.io/thicc) | [Remasuri3](https://twitter.com/Remasuri32) | An app which allows your chat to change colors on your model via point redeems and other events!|
|[Winter Wonderland Twitch Overlay](https://lualucky.itch.io/winter-wonderland-twitch-overlay) ([Video](https://youtu.be/htP9_D6Z5Qg)) | [Lua V. Lucky](https://twitter.com/LuaVLucky) | An app which provides numerous festive elements for any stream. Decorate a Christmas tree, pelt the streamer with snowballs, and more!|
|[VInput](https://store.steampowered.com/app/2174170/VInput/) ([Video](https://twitter.com/xiaoye1997/status/1611374527988256770)) | [xiaoye1997](https://twitter.com/xiaoye1997) | An app for sending data to VTubeStudio, such as game controller inputs, racing steering wheel data, time data, hardware monitoring data and data derived from custom LUA scripting.|
|[VtubeStudioSimpleSETool](https://mononobe-monoko.booth.pm/items/3468381) ([Video](https://twitter.com/mononobe_monoko/status/1465310301570551808)) | [Mononobe Monoko](https://twitter.com/mononobe_monoko) | An app for playing Sound Effects (SE) and displaying particle effects based on your movement.|
|[Camera Optical Flow Tracking Into VTube Studio](https://github.com/BluePengcho/Camera-Optical-Flow-Tracking-Into-VTube-Studio) | [PengCho](https://twitter.com/Blue_Pengcho) | An app for real-time vlogging with just a single, forward-facing camera. It automatically points your model's gaze towards the direction that the video feed is moving using Optical Flow techniques! No tracking camera needed.|
