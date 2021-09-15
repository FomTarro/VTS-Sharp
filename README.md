# VTS-Sharp
A Unity C# client interface for creating VTube Studio Plugins with the [official Vtube Studio API](https://github.com/DenchiSoft/VTubeStudio)!
 
## About
This library is maintained by Tom "Skeletom" Farro. If you need to contact him, the best way to do so is via [Twitter](https://www.twitter.com/fomtarro) or by leaving an issue ticket on this repo. Currently, this library is still in beta, but should be fully functional.
 
## Usage
 
In order to start making a plugin, simply make a class which extends `VTSPlugin`. In your class, call the `[Initialize](.#` method. Pass in your preferred implementations of a JSON utility, of a websocket, and of a mechanism to store the authorization token. Specify what happens on a successful or unsuccessful initialization. That's it. From there, you can call any method found in the [official Vtube Studio API](https://github.com/DenchiSoft/VTubeStudio).
 
You can find an example of custom plugin creation in the `Examples` folder, which also includes default implementations of the aforementioned initialization dependencies.
 
Because this library simply acts as an client interface for the official API, please check out the official API's readme for in-depth explanations about the API functionality.
 
## Design Pattern and Considerations
 



# API

## `class VTSPlugin`

### Properties
#### `string PluginName`: 
The name of this plugin. Required for authorization purposes.
#### `string PluginAuthor`: 
The name of this plugin's author. Required for authorization purposes.
#### `VTSWebSocket Socket`:  
The underlying WebSocket for connecting to VTS.
#### `ITokenStorage TokenStorage`: 
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
* `Action onDisconnect`: Callback executed upon disconnecting from VTS.
* `Action onError`: The Callback executed upon failed initialization.

All other methods can be inferred from the [official Vtube Studio API](https://github.com/DenchiSoft/VTubeStudio).

## `interface IWebSocket`

### Methods
#### `string GetNextResponse` 
Fetches the next response to process.
#### `Task Start`
Connects to the given URL and executes the relevant callback on completion. Takes the following args:
* `string URL`: URL to connect to.
* `Action onConnect`: Callback executed upon conencting to the URL.
* `Action onDisconnect`: Callback executed upon disconnecting from the URL.
* `Action onError`: Callback executed upon receiving an error.
#### `void Stop`
Closes the websocket. Executes the onDisconnect callback as specified in the Start method call.
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


### Swappable Components
In order to afford the most flexibility (and to be as decoupled from Unity as possible), the `VTSPlugin` class requires that implementations of a JSON utility, of a websocket, and of a token storage mechanism be provided. There are bare-bones examples provided by default, but the design pattern makes it easy for you to swap them out for better/more robust/more platform compliant alternatives. **In fact, it is highly encouraged that you do so**! Simply pass in your own implementations via the `Initialize` method.
 
### Asynchronous Design
Because the VTube Studio API is websocket-based, all calls to it are inherently asynchronous. Therefore, this library follows a callback-based design pattern.
 
Take, for example, the following method signature, found in the `VTSPlugin` class:
 
```
    public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError)
```
The method accepts two callbacks, `onSuccess` and `onError`. 
 
Upon the request being processed by Vtube Studio, 
one of these two callbacks will be invoked, depending on if the request was successful or not. 
 
Additionally, the callback will be passed a single argument, which represents the response data. Each method in the plugin API has a unique data type for its `onSuccess` callback, so the callbacks can be tailored for the type of call being made, in a strongly-typed way.