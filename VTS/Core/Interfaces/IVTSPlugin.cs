using System;
using System.Collections.Generic;

namespace VTS {

	public interface IVTSPlugin {
		/// <summary>
		/// The name of this plugin. Required for authorization purposes.
		/// </summary>
		/// <value></value>
		string PluginName { get; }
		/// <summary>
		/// The name of this plugin's author. Required for authorization purposes.
		/// </summary>
		/// <value></value>
		string PluginAuthor { get; }
		/// <summary>
		/// The icon for this plugin, encoded as a base64 string.
		/// </summary>
		/// <value></value>
		string PluginIcon { get; }
		/// <summary>
		/// Is the plugin currently authenticated?
		/// </summary>
		/// <value></value>
		bool IsAuthenticated { get; }

		/// <summary>
		/// The underlying Json Utility implementation.
		/// </summary>
		/// <value></value>
		IJsonUtility JsonUtility { get; }
		/// <summary>
		/// The underlying Token Storage implementation.
		/// </summary>
		/// <value></value>
		ITokenStorage TokenStorage { get; }
		/// <summary>
		/// The underlying Logger implementation.
		/// </summary>
		/// <value></value>
		IVTSLogger Logger { get; }

		/// <summary>
		/// Adds a custom parameter to the currently loaded VTS model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters</a>
		/// </summary>
		/// <param name="parameter">Information about the parameter to add. Parameter name must be 4-32 characters, alphanumeric.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Alters the properties of the item of the specified ID based on the provided options.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations">https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations</a>
		/// <param name="itemInstanceID">The ID of the item to move.</param>
		/// <param name="options">Configuration options about the request.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Disconnects from VTube Studio. Will fire the onDisconnect callback set via the Initialize method.
		/// </summary>
		void Disconnect();
		/// <summary>
		/// Gets the current state of the VTS API.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#status">https://github.com/DenchiSoft/VTubeStudio#status</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets a list of all available art meshes in the current VTS model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets the list of all available VTS models.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models">https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets information about the currently loaded VTS model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model">https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets physics information about the currently loaded model.
		/// 
		/// For more info, see
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-physics-settings-of-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#getting-physics-settings-of-currently-loaded-vts-model</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Requests a list of the states of all expressions in the currently loaded model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-current-expression-state-list">https://github.com/DenchiSoft/VTubeStudio#requesting-current-expression-state-list</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Checks to see if a face is being tracked.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker">https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets the list of VTS folders.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders">https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets a list of available hotkeys.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model</a>
		/// </summary>
		/// <param name="modelID">Optional, the model ID to get hotkeys for.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets a list of available hotkeys for the specified Live2D Item.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model</a>
		/// </summary>
		/// <param name="live2DItemFileName">Optional, the Live 2D item to get hotkeys for.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets a list of input parameters for the currently loaded VTS model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Retrieves a list of items, either in the scene or available as files, based on the provided options.
		/// 
		/// For more, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene</a>
		/// </summary>
		/// <param name="options">Configuration options about the request.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets a list of input parameters for the currently loaded Live2D model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model">https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets the value for the specified parameter.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-one-specific-parameter-default-or-custom">https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-one-specific-parameter-default-or-custom</a>
		/// </summary>
		/// <param name="parameterName">The name of the parameter to get the value of.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets the current port that this socket is set to connect to.
		/// </summary>
		/// <returns>Port number as an int</returns>
		int GetPort();
		/// <summary>
		/// Gets a dictionary indexed by port number containing information about all available VTube Studio ports.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#api-server-discovery-udp">https://github.com/DenchiSoft/VTubeStudio#api-server-discovery-udp</a>
		/// </summary>
		/// <returns>Dictionary indexed by port number.</returns>
		Dictionary<int, VTSStateBroadcastData> GetPorts();
		/// <summary>
		/// Gets color information about the scene lighting overlay, if it is enabled.
		/// 
		/// For more info, see
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-scene-lighting-overlay-color">https://github.com/DenchiSoft/VTubeStudio#getting-scene-lighting-overlay-color</a>
		/// </summary>
		/// <param name="onSuccess"></param>
		/// <param name="onError"></param>
		void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Gets current metrics about the VTS application.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics">https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics</a>
		/// </summary>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Selects the Websocket, JSON utility, and Token Storage implementations, then attempts to Authenticate the plugin.
		/// </summary>
		/// <param name="webSocket">The WebSocket implementation.</param>
		/// <param name="jsonUtility">The JSON serializer/deserializer implementation.</param>
		/// <param name="tokenStorage">The Token Storage implementation.</param>
		/// <param name="onConnect">Callback executed upon successful initialization.</param>
		/// <param name="onDisconnect">Callback executed upon disconnecting from VTS.</param>
		/// <param name="onError">Callback executed upon failed initialization.</param>
		void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, Action onConnect, Action onDisconnect, Action<VTSErrorData> onError);
		/// <summary>
		/// Sends a list of parameter names and corresponding values to assign to them.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
		/// </summary>
		/// <param name="values">A list of parameters and the values to assign to them.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Sends a list of parameter names and corresponding values to assign to them.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
		/// </summary>
		/// <param name="values">A list of parameters and the values to assign to them.</param>
		/// <param name="mode">The method by which the parameter values are applied.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Sends a list of parameter names and corresponding values to assign to them.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
		/// </summary>
		/// <param name="values">A list of parameters and the values to assign to them.</param>
		/// <param name="mode">The method by which the parameter values are applied.</param>
		/// <param name="faceFound">A flag which can be set to True to tell VTube Studio to consider the user face as found.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Loads an item into the scene, with properties based on the provided options.
		/// 
		/// For more, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene">https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene</a>
		/// </summary>
		/// <param name="fileName">The file name of the item to load, typically retrieved from an ItemListRequest.</param>
		/// <param name="options">Configuration options about the request.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Loads a VTS model by its Model ID. Will return an error if the model cannot be loaded.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id">https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id</a>
		/// </summary>
		/// <param name="modelID">The Model ID/Name.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Moves the items of the specified IDs based on their provided options.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
		/// </summary>
		/// <param name="items">The list of Item Insance IDs and their corresponding movement options</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Moves the currently loaded VTS model.
		/// 
		/// For more info, particularly about what each position value field does, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model</a>
		/// </summary>
		/// <param name="position">The desired position information. Fields will be null-valued by default.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Removes a custom parameter from the currently loaded VTS model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters</a>
		/// </summary>
		/// <param name="parameterName">The name f the parameter to remove.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Initiates a prompt in VTube Studio that prompts the user to click to select a number of art meshes.
		/// 
		/// For more info, see
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#asking-user-to-select-artmeshes">https://github.com/DenchiSoft/VTubeStudio#asking-user-to-select-artmeshes</a>
		/// </summary>
		/// <param name="textOverride">Optional text to display on the prompt.</param>
		/// <param name="helpOverride">Optional tooltip text to display on the prompt.</param>
		/// <param name="count">The number of art meshes to select. Values of 0 or lower will allow the user to choose any arbitrary number of art meshes (but at least one).</param>
		/// <param name="activeArtMeshes">A list of already-selected art meshes.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes, Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Overrides the physics properties of the current model. Once a plugin has overridden a model's physics, no other plugins may do so.
		/// 
		/// For more info, see
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#overriding-physics-settings-of-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#overriding-physics-settings-of-currently-loaded-vts-model</a>
		/// </summary>
		/// <param name="strengthOverrides">A list of strength override settings </param>
		/// <param name="windOverrides">A list of wind override settings.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Activates or deactivates the given expression.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-activation-or-deactivation-of-expressions">https://github.com/DenchiSoft/VTubeStudio#requesting-activation-or-deactivation-of-expressions</a>
		/// </summary>
		/// <parame name="expression">The expression file name to change the state of.</param>
		/// <param name="active">The state to set the expression to. True to activate, false to deactivate.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Sets the connection IP address to the given string. Returns true if the string is a valid IP Address format, returns false otherwise.
		/// If the IP Address is changed while an active connection exists, you will need to reconnect.
		/// </summary>
		/// <param name="ipString">The string form of the IP address, in dotted-quad notation for IPv4.</param>
		/// <returns>True if the string is a valid IP Address format, False otherwise.</returns>
		bool SetIPAddress(string ipString);
		/// <summary>
		/// Changes the NDI configuration.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#get-and-set-ndi-settings">https://github.com/DenchiSoft/VTubeStudio#get-and-set-ndi-settings</a>
		/// </summary>
		/// <parame name="config">The desired NDI configuration.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Sets the connection port to the given number. Returns true if the port is a valid VTube Studio port, returns false otherwise. 
		/// If the port number is changed while an active connection exists, you will need to reconnect.
		/// </summary>
		/// <param name="port">The port to connect to.</param>
		/// <returns>True if the port is a valid VTube Studio port, False otherwise.</returns>
		bool SetPort(int port);
		/// <summary>
		/// Subscribes to the Background Changed Event.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
		/// </summary>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Model Config Changed Event.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified</a>
		/// </summary>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Model Loaded Event. Can be configured with a model ID to only recieve events about the given model.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded</a>
		/// </summary>
		/// <param name="config">Configuration options about the subscription.</param>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Model Moved Event.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated</a>
		/// </summary>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Model Outline Event.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed</a>
		/// </summary>
		/// <param name="config">Configuration options about the subscription.</param>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Test Event for testing the event API. Can be configured with a message to echo back every second.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event</a>
		/// </summary>
		/// <param name="config">Configuration options about the subscription.</param>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Subscribes to the Tracking Status Changed Event.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
		/// </summary>
		/// <param name="onEvent">Callback to execute upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon successfully subscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Tints matched components of the current art mesh.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color">https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color</a>
		/// </summary>
		/// <param name="tint">The tint to be applied.</param>
		/// <param name="mixWithSceneLightingColor"> The amount to mix the color with scene lighting, from 0 to 1. Default is 1.0, which will have the color override scene lighting completely.
		/// <param name="matcher">The ArtMesh matcher search parameters.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Triggers a given hotkey.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys">https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys</a>
		/// </summary>
		/// <param name="hotkeyID">The model ID to get hotkeys for.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Triggers a given hotkey on a specified Live2D item.
		/// 
		/// For more info, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys">https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys</a>
		/// </summary>
		/// <param name="itemInstanceID">The instance ID of the Live2D item.</param>
		/// <param name="hotkeyID">The model ID to get hotkeys for.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Unload items from the scene, either broadly, by identifier, or by file name, based on the provided options.
		/// 
		/// For more, see 
		/// <a href="https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene">https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene</a>
		/// </summary>
		/// <param name="options">Configuration options about the request.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from all events.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing to the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Background Changed Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Model Config Changed Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Model Loaded Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>

		void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Model Moved Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Model Outline Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Test Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
		/// <summary>
		/// Unsubscribes from the Tracking Status Changed Event.
		/// </summary>
		/// <param name="onUnsubscribe">Callback executed upon successfully unsubscribing from the event.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError);
	}
}
