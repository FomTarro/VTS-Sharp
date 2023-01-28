# VTS-Sharp
A Unity C# client interface for creating VTube Studio Plugins with the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio)!
 
## About
This library is maintained by Tom "Skeletom" Farro. If you need to contact him, the best way to do so is via [Twitter](https://www.twitter.com/fomtarro) or by leaving an issue ticket on this repo.

If you're more of an email-oriented person, you can contact his support email: [tom@skeletom.net](mailto:tom@skeletom.net).

This library can also be found on the [Unity Asset Store](https://assetstore.unity.com/packages/tools/integration/vts-sharp-203218), but this repo will always be the most up-to-date version.
 
## Usage
In order to start making a plugin, simply make a class which extends `VTSPlugin`. In your class, call the [`Initialize`](#void-initialize) method. Pass in your preferred implementations of a [JSON utility](#interface-ijsonutility), of a [websocket](#interface-iwebsocket), and of a [mechanism to store the authorization token](#interface-itokenstorage). Specify what happens on a successful or unsuccessful initialization. That's it. In fact, these steps are all done for you already, out of the box, in the `MyFirstPlugin` class! From there, you can call any method found in the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio).
 
You can find an example of custom plugin creation in the `Examples` folder, which also includes default implementations of the aforementioned initialization dependencies. 

You can find a video tutorial that demonstrates [how to get started in under 90 seconds here](https://www.youtube.com/watch?v=lUGeMEVzjAU).
 
Because this library simply acts as an client interface for the official API, please check out the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio) for in-depth explanations about the API functionality.
 
## Design Pattern and Considerations
 
### Swappable Components
In order to afford the most flexibility (and to be as decoupled from Unity as possible), the underlying components of the [`VTSPlugin`](#class-vtsplugin) are all defined as interfaces. This allows you to swap out the built-in implementations with more robust or platform-compliant ones. By default, the `MyFirstPlugin` class features working implemntations of all needed components, but if you want, you can pass in your own implementations via the [`Initialize`](#void-initialize) method.

### Asynchronous Design
Because the VTube Studio API is websocket-based, all calls to it are inherently asynchronous. Therefore, this library follows a callback-based design pattern.
Take, for example, the following method signature, found in the [`VTSPlugin`](#class-vtsplugin) class:
 
```
void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError)
```
The method accepts two callbacks, `onSuccess` and `onError`, but does not return a value. 

Upon the request being processed by VTube Studio, 
one of these two callbacks will be invoked, depending on if the request was successful or not. The callback accepts in a single, strongly-typed argument reflecting the response payload. You can find what to expect in each payload class in the [official VTube Studio API](https://github.com/DenchiSoft/VTubeStudio).

This library also supports the [VTube Studio Event Subscription API](https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md). With this feature, you can subscribe to various events to make sure your plugin gets a message when something happens in VTube Studio. Event Subscription follows a similar asynchronous design pattern. 
Take, for example, the following method signature, found in the [`VTSPlugin`](#class-vtsplugin) class:

```
 void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
```
The method accepts an optional configuration class, and three callbacks, `onEvent`, `onSubscribe` and `onError`, but does not return a value.


Upon successfully subscribing to the event in VTube Studio, the `onSubscribe` callback will be invoked, and then `onEvent` will be invoked any time VTube Studio publishes an event of that type. If the subscription fails for any reason, `onError` will be invoked. 

# API

## `class VTSPlugin`

### Properties
#### `string PluginName`
The name of this plugin. Required for authorization purposes.
#### `string PluginAuthor`
The name of this plugin's author. Required for authorization purposes.
#### `Texture2D PluginIcon`
The icon of this for this plugin. Optional, must be exactly 128*128 pixels in size.
#### `VTSWebSocket Socket`
The underlying WebSocket for connecting to VTS.
#### `ITokenStorage TokenStorage`
The underlying Token Storage mechanism for connecting to VTS.
#### `bool IsAuthenticated`
Is the plugin currently authenticated?

### Methods
#### `void Initialize`
Authenticates the plugin as well as selects the Websocket, JSON utility, and Token Storage implementations. Takes the following args:
* `IWebSocket webSocket`: The websocket implementation.
* `IJsonUtility jsonUtility`: The JSON serializer/deserializer implementation.
* `ITokenStorage tokenStorage`: The Token Storage implementation.
* `Action onConnect`: Callback executed upon successful initialization.
* `Action onDisconnect`: Callback executed upon disconnecting from VTS (accidental or otherwise).
* `Action onError`: The Callback executed upon failed initialization.

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

### `VTube Studio API Events`

Event subscription methods can be inferred from the [official VTube Studio Event Subscription API](https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md).

## `interface IWebSocket`

### Methods
#### `string GetNextResponse` 
Fetches the next response to process.
#### `void Start`
Connects to the given URL and executes the relevant callback on completion. Takes the following args:
* `string URL`: URL to connect to.
* `Action onConnect`: Callback executed upon connecting to the URL.
* `Action onDisconnect`: Callback executed upon disconnecting from the URL (accidental or otherwise).
* `Action onError`: Callback executed upon receiving an error.
#### `void Stop`
Closes the websocket. Executes the `onDisconnect` callback as specified in the `Start` method call.
#### `bool IsConnecting`
Is the socket in the process of connecting?
#### `bool IsConnectionOpen` 
Has the socket successfully connected?
#### `void Send` 
Send a payload to the websocket server. Takes the following args:
* `string message`: The payload to send.

## `interface ITokenStorage`

### Methods
#### `string LoadToken` 
Loads the auth token.
#### `void SaveToken` 
Saves an auth token. Takes the following args:
* `string token`: The token to save.
#### `void DeleteToken` 
Deletes the auth token.

## `interface IJsonUtility`

### Methods
#### `T FromJson<T>`
Deserializes a JSON string into an object of the specified type. Takes the following args:
* `T`: The type to deserialize into.
* `string json`: The JSON string.
#### `string ToJson`
Converts an object into a JSON string. Takes the following args:
* `object obj`: The object to serialized.

# Acknowledgements

## [DenchiSoft](https://github.com/DenchiSoft/VTubeStudio)
None of this would be possible without Denchi's tireless work on VTube Studio itself. 

## [WebSocketSharp](https://github.com/sta/websocket-sharp)
An implementation of Websocket using WebSocketSharp has been included for use, adhering to the [library's MIT license](https://github.com/sta/websocket-sharp/blob/master/LICENSE.txt).

# Made With VTS-Sharp

Below is a list of some plugins which were made using this library! If you have made something you would like included on this list, please [send Tom a message](#about).

| Plugin | Developer | Explanation |
| --- | --- | --- |
| [PentabInfoPickerForVTS](https://github.com/Ganeesya/PentabInfoPickerForVTS) ([Video](https://www.youtube.com/watch?v=kklcAzTequ)) | [Ganeesya](https://twitter.com/Ganeesya) | An app which allows users to control their model with a tablet and pen. |
| [VTS-ChangeEyeColor](https://yataya2000.booth.pm/items/3551421) ([Github](https://github.com/TaniNatsumi/VTS-ChangeEyeColor)) | [TaniNatsumi](https://twitter.com/BURAI_VC2008) | An app which allows users to change the eye colors of their model. Can change each eye color individually (heterochromia).|
| [VTSLive](https://github.com/fastestyukkuri/VTSLivePlugins) | [fastest_yukkuri](https://twitter.com/fastest_yukkuri) | An app which allows VTube Studio to reflect the movement of analog and digital clocks, the movement of the sun and moon, and weather information from around the world.|
|[VTS-Mod](https://github.com/MechaWolfVtuberShin/VTS-Mod/releases/tag/vts-mod)| [MechaWolfVtuberShin](https://twitter.com/ShinChannel3) | An app which allows users to change the surface color of the model including RGB. It can also change the rotation of the model.|
|[Twitch Integrated Throwing System (T.I.T.S)](https://remasuri3.itch.io/tits) ([Video](https://www.youtube.com/watch?v=hWOIZqv-u50)) | [Remasuri3](https://twitter.com/Remasuri32) | An app which allows your chat to bully you as much as possible >:D It can be used with or without VTube Studio to let people throw items at your face!|
|[VTS-Heartrate](https://github.com/FomTarro/vts-heartrate)| [Skeletom](https://twitter.com/FomTarro) | An app which allows users to connect their heartrate data to VTube Studio, to cause their model to become flushed under stress and breathe more heavily, among other things. |
|[ViewLink](https://kawaentertainment.com/view-link-vr-to-live2-d/)| [Kawa Entertainment](https://twitter.com/kawa_entertain) | An app for translating 3D VR movement into Live2D motion tracking, allowing you to stream VR games with your Live2D model. |
|[VBridger](https://store.steampowered.com/app/1898830/VBridger/) ([Video](https://www.youtube.com/watch?v=mFUG4L4Lgfo)) | [PiPuProductions](https://twitter.com/PiPuProductions) | An app designed for VTube Studio and Live2D, which allows the user to make better use of iPhoneX ARKit tracking on their Live2D model. |
|[Audiomimi](https://artemiz.booth.pm/items/3800791/) ([Video](https://twitter.com/ArtemizComs/status/1522834869247651840)) | [Artemiz](https://twitter.com/ArtemizComs) | An app that allows you to use SFX based on VTS parameter movement. |
|[VTS Desktop Audio](https://lualucky.itch.io/vts-desktop-audio-plugin) ([Video](https://twitter.com/LuaVLucky/status/1592741024509853696)) | [Lua V. Lucky](https://twitter.com/LuaVLucky) | An app that allows you to control your model with your desktop audio! Converts various parts of the audio spectrum to custom tracking parameters. |
