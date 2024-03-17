using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VTS.Core {

	/// <summary>
	/// The base class for VTS plugin creation in any C# runtime.
	/// </summary>
	public class CoreVTSPlugin : IVTSPlugin, IDisposable {

		public string PluginName { get; private set; }
		public string PluginAuthor { get; private set; }
		public string PluginIcon { get; private set; }

		public bool IsAuthenticated { get; private set; }
		private string _token = null;

		public ITokenStorage TokenStorage { get; private set; }
		public IJsonUtility JsonUtility { get; private set; }
		public IVTSLogger Logger { get; private set; }
		public IVTSWebSocket Socket { get; private set; }

		private readonly CancellationTokenSource _cancelToken;
		private readonly Task _tickLoop = null;
		private readonly int _tickInterval = 100;


		/// <summary>
		/// Creates a new VTSPlugin.
		/// </summary>
		/// <param name="logger">The logger implementation</param>
		/// <param name="updateIntervalMs">The number of milliseconds between each update cycle of the plugin.</param>
		/// <param name="pluginName">The plugin name. Must be between 3 and 32 characters.</param>
		/// <param name="pluginAuthor">The plugin author. Must be between 3 and 32 characters.</param>
		/// <param name="pluginIcon">The plugin icon, encoded as a base64 string. Must be 128*128 pixels exactly.</param>
		public CoreVTSPlugin(IVTSLogger logger, int updateIntervalMs, string pluginName, string pluginAuthor, string pluginIcon) {
			this.Socket = new VTSWebSocket();
			this.Logger = logger;
			this.PluginName = pluginName;
			this.PluginAuthor = pluginAuthor;
			this.PluginIcon = pluginIcon;
			this._cancelToken = new CancellationTokenSource();
			this._tickInterval = updateIntervalMs;
			this._tickLoop = TickLoop(this._cancelToken.Token);

			if (pluginName.Length < 3 || pluginName.Length > 32 || pluginAuthor.Length < 3 || pluginAuthor.Length > 32)
				throw new Exception("Plugin name and plugin author must both be between 3 and 32 characters.");
		}

		~CoreVTSPlugin() {
			Dispose();
		}

		public void Dispose() {
			this.Logger.Log(string.Format("Disposing of VTS Plugin: {0}...", this.PluginName));
			this._cancelToken.Cancel();
		}

		#region Initialization

		public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, Action onConnect, Action onDisconnect, Action<VTSErrorData> onError) {
			this.TokenStorage = tokenStorage;
			this.JsonUtility = jsonUtility;
			this.Socket.Initialize(webSocket, this.JsonUtility, this.Logger);
			Action onCombinedConnect = () => {
				this.Socket.ResubscribeToEvents();
				onConnect();
			};
			this.Socket.Connect(() => {
				// If API enabled, authenticate
				Authenticate(
					(r) => {
						if (!r.data.authenticated) {
							Reauthenticate(onCombinedConnect, onError);
						} else {
							this.IsAuthenticated = true;
							onCombinedConnect();
						}
					},
					(r) => {
						// If initial authentication fails, try again
						// (Likely just needs fresh token)
						Reauthenticate(onCombinedConnect, onError);
					}
				);
			},
			() => {
				this.IsAuthenticated = false;
				onDisconnect();
			},
			(e) => {
				VTSErrorData error = new VTSErrorData();
				error.data.errorID = ErrorID.InternalServerError;
				error.data.message = e.Message;
				this.IsAuthenticated = false;
				onError(error);
			});
		}

		public Task InitializeAsync(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, Action onDisconnect) {
			var tcs = new TaskCompletionSource<object>();

			Initialize(
				webSocket,
				jsonUtility,
				tokenStorage,
				() => tcs.SetResult(null),
				onDisconnect,
				error => tcs.SetException(error.ToException())
			);

			return tcs.Task;
		}

		public void Disconnect() {
			if (this.Socket != null) {
				this.Socket.Disconnect();
			}
		}

		private void Tick(float timeDelta) {
			if (this.Socket != null) {
				this.Socket.Tick(timeDelta);
			}
		}

		private async Task TickLoop(CancellationToken token) {
			float intervalInSeconds = ((float)this._tickInterval) / 1000f;
			this.Logger.Log(string.Format("Starting VTS Plugin processor for plugin: {0}...", this.PluginName));
			while (!token.IsCancellationRequested) {
				Tick(intervalInSeconds);
				await Task.Delay(this._tickInterval);
			}
			this.Logger.Log(string.Format("Ending VTS Plugin processor for plugin: {0}...", this.PluginName));
		}

		#endregion

		#region Authentication

		private void Authenticate(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError) {
			this.IsAuthenticated = false;
			if (this.TokenStorage != null) {
				this._token = this.TokenStorage.LoadToken();
				if (string.IsNullOrEmpty(this._token)) {
					GetToken(onSuccess, onError);
				} else {
					UseToken(onSuccess, onError);
				}
			} else {
				GetToken(onSuccess, onError);
			}
		}

		private void Reauthenticate(Action onConnect, Action<VTSErrorData> onError) {
			// Debug.LogWarning("Token expired, acquiring new token...");
			this.IsAuthenticated = false;
			this.TokenStorage.DeleteToken();
			Authenticate(
				(t) => {
					this.IsAuthenticated = true;
					onConnect();
				},
				(t) => {
					this.IsAuthenticated = false;
					onError(t);
				}
			);
		}

		private void GetToken(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError) {
			VTSAuthData tokenRequest = new VTSAuthData();
			tokenRequest.data.pluginName = this.PluginName;
			tokenRequest.data.pluginDeveloper = this.PluginAuthor;
			tokenRequest.data.pluginIcon = this.PluginIcon;
			this.Socket.Send<VTSAuthData, VTSAuthData>(tokenRequest,
			(a) => {
				this._token = a.data.authenticationToken;
				if (this.TokenStorage != null) {
					this.TokenStorage.SaveToken(this._token);
				}
				UseToken(onSuccess, onError);
			},
			onError);
		}

		private void UseToken(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError) {
			VTSAuthData authRequest = new VTSAuthData();
			authRequest.messageType = "AuthenticationRequest";
			authRequest.data.pluginName = this.PluginName;
			authRequest.data.pluginDeveloper = this.PluginAuthor;
			authRequest.data.authenticationToken = this._token;
			this.Socket.Send<VTSAuthData, VTSAuthData>(authRequest, onSuccess, onError);
		}

		#endregion

		#region Port Discovery

		public Dictionary<int, VTSStateBroadcastData> GetPorts() {
			return this.Socket.GetPorts();
		}

		public int GetPort() {
			return this.Socket.Port;
		}

		public bool SetPort(int port) {
			return this.Socket.SetPort(port);
		}

		public bool SetIPAddress(string ipString) {
			return this.Socket.SetIPAddress(ipString);
		}

		#endregion

		#region VTS General API Wrapper

		// Get API State

		public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSStateData request = new VTSStateData();
			this.Socket.Send<VTSStateData, VTSStateData>(request, onSuccess, onError);
		}
		public async Task<VTSStateData> GetAPIState() {
			return await VTSExtensions.Async<VTSStateData, VTSErrorData>(GetAPIState);
		}

		// Get Statistics

		public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError) {
			VTSStatisticsData request = new VTSStatisticsData();
			this.Socket.Send<VTSStatisticsData, VTSStatisticsData>(request, onSuccess, onError);
		}
		public async Task<VTSStatisticsData> GetStatistics() {
			return await VTSExtensions.Async<VTSStatisticsData, VTSErrorData>(GetStatistics);
		}

		// Get Folder Info

		public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError) {
			VTSFolderInfoData request = new VTSFolderInfoData();
			this.Socket.Send<VTSFolderInfoData, VTSFolderInfoData>(request, onSuccess, onError);
		}
		public async Task<VTSFolderInfoData> GetFolderInfo() {
			return await VTSExtensions.Async<VTSFolderInfoData, VTSErrorData>(GetFolderInfo);
		}

		// Get Current Model

		public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelData request = new VTSCurrentModelData();
			this.Socket.Send<VTSCurrentModelData, VTSCurrentModelData>(request, onSuccess, onError);
		}
		public Task<VTSCurrentModelData> GetCurrentModel() {
			return VTSExtensions.Async<VTSCurrentModelData, VTSErrorData>(GetCurrentModel);
		}

		// Get Available Models

		public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError) {
			VTSAvailableModelsData request = new VTSAvailableModelsData();
			this.Socket.Send<VTSAvailableModelsData, VTSAvailableModelsData>(request, onSuccess, onError);
		}
		public Task<VTSAvailableModelsData> GetAvailableModels() {
			return VTSExtensions.Async<VTSAvailableModelsData, VTSErrorData>(GetAvailableModels);
		}

		// Load Model

		public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError) {
			VTSModelLoadData request = new VTSModelLoadData();
			request.data.modelID = modelID;
			this.Socket.Send<VTSModelLoadData, VTSModelLoadData>(request, onSuccess, onError);
		}
		public Task<VTSModelLoadData> LoadModel(string modelId) {
			return VTSExtensions.Async<string, VTSModelLoadData, VTSErrorData>(LoadModel, modelId);
		}

		// Move Model

		public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSMoveModelData request = new VTSMoveModelData();
			request.data = position;
			this.Socket.Send<VTSMoveModelData, VTSMoveModelData>(request, onSuccess, onError);
		}
		public async Task<VTSMoveModelData> MoveModel(VTSMoveModelData.Data position) {
			return await VTSExtensions.Async<VTSMoveModelData.Data, VTSMoveModelData, VTSErrorData>(MoveModel, position);
		}

		// Get Hotkeys in Model

		public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
			request.data.modelID = modelID;
			this.Socket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}
		public async Task<VTSHotkeysInCurrentModelData> GetHotkeysInCurrentModel(string modelId) {
			return await VTSExtensions.Async<string, VTSHotkeysInCurrentModelData, VTSErrorData>(GetHotkeysInCurrentModel, modelId);
		}

		// Get Hotkeys in Item

		public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
			request.data.live2DItemFileName = live2DItemFileName;
			this.Socket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}
		public async Task<VTSHotkeysInCurrentModelData> GetHotkeysInLive2DItem(string live2DItemFileName) {
			return await VTSExtensions.Async<string, VTSHotkeysInCurrentModelData, VTSErrorData>(GetHotkeysInLive2DItem, live2DItemFileName);
		}

		// Trigger Hotkey		

		public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
			request.data.hotkeyID = hotkeyID;
			this.Socket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}
		public async Task<VTSHotkeyTriggerData> TriggerHotkey(string hotkeyId) {
			return await VTSExtensions.Async<string, VTSHotkeyTriggerData, VTSErrorData>(TriggerHotkey, hotkeyId);
		}

		// Trigger Hotkey in Item

		public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
			request.data.hotkeyID = hotkeyID;
			request.data.itemInstanceID = itemInstanceID;
			this.Socket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}
		public async Task<VTSHotkeyTriggerData> TriggerHotkeyForLive2DItem(string itemInstanceId, string hotkeyId) {
			return await VTSExtensions.Async<string, string, VTSHotkeyTriggerData, VTSErrorData>(TriggerHotkeyForLive2DItem, itemInstanceId, hotkeyId);
		}

		// Get Art Mesh List

		public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshListData request = new VTSArtMeshListData();
			this.Socket.Send<VTSArtMeshListData, VTSArtMeshListData>(request, onSuccess, onError);
		}
		public async Task<VTSArtMeshListData> GetArtMeshList() {
			return await VTSExtensions.Async<VTSArtMeshListData, VTSErrorData>(GetArtMeshList);
		}

		// Tint Art Mesh

		public void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError) {
			VTSColorTintData request = new VTSColorTintData();
			ArtMeshColorTint colorTint = new ArtMeshColorTint() {
				colorR = tint.colorR,
				colorG = tint.colorG,
				colorB = tint.colorB,
				colorA = tint.colorA,
				mixWithSceneLightingColor = System.Math.Min(1, System.Math.Max(mixWithSceneLightingColor, 0))
			};
			request.data.colorTint = colorTint;
			request.data.artMeshMatcher = matcher;
			this.Socket.Send<VTSColorTintData, VTSColorTintData>(request, onSuccess, onError);
		}
		public async Task<VTSColorTintData> TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher) {
			return await VTSExtensions.Async<ColorTint, float, ArtMeshMatcher, VTSColorTintData, VTSErrorData>(TintArtMesh, tint, mixWithSceneLightingColor, matcher);
		}

		// Get Scene Color Overlay Info

		public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError) {
			VTSSceneColorOverlayData request = new VTSSceneColorOverlayData();
			this.Socket.Send<VTSSceneColorOverlayData, VTSSceneColorOverlayData>(request, onSuccess, onError);
		}
		public async Task<VTSSceneColorOverlayData> GetSceneColorOverlayInfo() {
			return await VTSExtensions.Async<VTSSceneColorOverlayData, VTSErrorData>(GetSceneColorOverlayInfo);
		}

		// Get Found Face

		public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError) {
			VTSFaceFoundData request = new VTSFaceFoundData();
			this.Socket.Send<VTSFaceFoundData, VTSFaceFoundData>(request, onSuccess, onError);
		}
		public async Task<VTSFaceFoundData> GetFaceFound() {
			return await VTSExtensions.Async<VTSFaceFoundData, VTSErrorData>(GetFaceFound);
		}

		// Get Input Parameter List

		public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSInputParameterListData request = new VTSInputParameterListData();
			this.Socket.Send<VTSInputParameterListData, VTSInputParameterListData>(request, onSuccess, onError);
		}
		public async Task<VTSInputParameterListData> GetInputParameterList() {
			return await VTSExtensions.Async<VTSInputParameterListData, VTSErrorData>(GetInputParameterList);
		}

		// Get Parameter Value

		public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterValueData request = new VTSParameterValueData();
			request.data.name = parameterName;
			this.Socket.Send<VTSParameterValueData, VTSParameterValueData>(request, onSuccess, onError);
		}
		public async Task<VTSParameterValueData> GetParameterValue(string parameterName) {
			return await VTSExtensions.Async<string, VTSParameterValueData, VTSErrorData>(GetParameterValue, parameterName);
		}

		// Get Live2D Parameter List

		public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSLive2DParameterListData request = new VTSLive2DParameterListData();
			this.Socket.Send<VTSLive2DParameterListData, VTSLive2DParameterListData>(request, onSuccess, onError);
		}
		public async Task<VTSLive2DParameterListData> GetLive2DParameterList() {
			return await VTSExtensions.Async<VTSLive2DParameterListData, VTSErrorData>(GetLive2DParameterList);
		}

		// Add Custom Parameter

		public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterCreationData request = new VTSParameterCreationData();
			request.data.parameterName = SanitizeParameterName(parameter.parameterName);
			request.data.explanation = parameter.explanation;
			request.data.min = parameter.min;
			request.data.max = parameter.max;
			request.data.defaultValue = parameter.defaultValue;
			this.Socket.Send<VTSParameterCreationData, VTSParameterCreationData>(request, onSuccess, onError);
		}
		public async Task<VTSParameterCreationData> AddCustomParameter(VTSCustomParameter parameter) {
			return await VTSExtensions.Async<VTSCustomParameter, VTSParameterCreationData, VTSErrorData>(AddCustomParameter, parameter);
		}

		// Remove Custom Parameter

		public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterDeletionData request = new VTSParameterDeletionData();
			request.data.parameterName = SanitizeParameterName(parameterName);
			this.Socket.Send<VTSParameterDeletionData, VTSParameterDeletionData>(request, onSuccess, onError);
		}
		public async Task<VTSParameterDeletionData> RemoveCustomParameter(string parameterName) {
			return await VTSExtensions.Async<string, VTSParameterDeletionData, VTSErrorData>(RemoveCustomParameter, parameterName);
		}

		// Inject Parameter Values

		public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, VTSInjectParameterMode.SET, false, onSuccess, onError);
		}
		public async Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values) {
			return await VTSExtensions.Async<VTSParameterInjectionValue[], VTSInjectParameterData, VTSErrorData>(InjectParameterValues, values);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, mode, false, onSuccess, onError);
		}
		public async Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode) {
			return await VTSExtensions.Async<VTSParameterInjectionValue[], VTSInjectParameterMode, VTSInjectParameterData, VTSErrorData>(InjectParameterValues, values, mode);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			VTSInjectParameterData request = new VTSInjectParameterData();
			foreach (VTSParameterInjectionValue value in values) {
				value.id = SanitizeParameterName(value.id);
			}
			request.data.faceFound = faceFound;
			request.data.parameterValues = values;
			request.data.mode = InjectParameterModeToString(mode);
			this.Socket.Send<VTSInjectParameterData, VTSInjectParameterData>(request, onSuccess, onError);
		}
		public async Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound) {
			return await VTSExtensions.Async<VTSParameterInjectionValue[], VTSInjectParameterMode, bool, VTSInjectParameterData, VTSErrorData>(InjectParameterValues, values, mode, faceFound);
		}

		// Get Expression State List

		public void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionStateData request = new VTSExpressionStateData();
			request.data.details = true;
			this.Socket.Send<VTSExpressionStateData, VTSExpressionStateData>(request, onSuccess, onError);
		}
		public async Task<VTSExpressionStateData> GetExpressionStateList() {
			return await VTSExtensions.Async<VTSExpressionStateData, VTSErrorData>(GetExpressionStateList);
		}

		// Set Expression State

		public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionActivationData request = new VTSExpressionActivationData();
			request.data.expressionFile = expression;
			request.data.active = active;
			this.Socket.Send<VTSExpressionActivationData, VTSExpressionActivationData>(request, onSuccess, onError);
		}
		public async Task<VTSExpressionActivationData> SetExpressionState(string expression, bool active) {
			return await VTSExtensions.Async<string, bool, VTSExpressionActivationData, VTSErrorData>(SetExpressionState, expression, active);
		}

		// Get Current Model Physics

		public void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelPhysicsData request = new VTSCurrentModelPhysicsData();
			this.Socket.Send<VTSCurrentModelPhysicsData, VTSCurrentModelPhysicsData>(request, onSuccess, onError);
		}
		public async Task<VTSCurrentModelPhysicsData> GetCurrentModelPhysics() {
			return await VTSExtensions.Async<VTSCurrentModelPhysicsData, VTSErrorData>(GetCurrentModelPhysics);
		}

		// Set Current Model Physics

		public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSOverrideModelPhysicsData request = new VTSOverrideModelPhysicsData();
			request.data.strengthOverrides = strengthOverrides;
			request.data.windOverrides = windOverrides;
			this.Socket.Send<VTSOverrideModelPhysicsData, VTSOverrideModelPhysicsData>(request, onSuccess, onError);
		}
		public async Task<VTSOverrideModelPhysicsData> SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides) {
			return await VTSExtensions.Async<VTSPhysicsOverride[], VTSPhysicsOverride[], VTSOverrideModelPhysicsData, VTSErrorData>(SetCurrentModelPhysics, strengthOverrides, windOverrides);
		}

		// Set NDI Config

		public void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError) {
			this.Socket.Send<VTSNDIConfigData, VTSNDIConfigData>(config, onSuccess, onError);
		}
		public async Task<VTSNDIConfigData> SetNDIConfig(VTSNDIConfigData config) {
			return await VTSExtensions.Async<VTSNDIConfigData, VTSNDIConfigData, VTSErrorData>(SetNDIConfig, config);
		}

		// Get Item List

		public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemListRequestData request = new VTSItemListRequestData();
			request.data.includeAvailableSpots = options.includeAvailableSpots;
			request.data.includeItemInstancesInScene = options.includeItemInstancesInScene;
			request.data.includeAvailableItemFiles = options.includeAvailableItemFiles;
			request.data.onlyItemsWithFileName = options.onlyItemsWithFileName;
			request.data.onlyItemsWithInstanceID = options.onlyItemsWithInstanceID;
			this.Socket.Send<VTSItemListRequestData, VTSItemListResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemListResponseData> GetItemList(VTSItemListOptions options) {
			return await VTSExtensions.Async<VTSItemListOptions, VTSItemListResponseData, VTSErrorData>(GetItemList, options);
		}

		// Load Item

		public void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemLoadRequestData request = new VTSItemLoadRequestData();
			request.data.fileName = fileName;
			request.data.positionX = options.positionX;
			request.data.positionY = options.positionY;
			request.data.size = options.size;
			request.data.rotation = options.rotation;
			request.data.fadeTime = options.fadeTime;
			request.data.order = options.order;
			request.data.failIfOrderTaken = options.failIfOrderTaken;
			request.data.smoothing = options.smoothing;
			request.data.censored = options.censored;
			request.data.flipped = options.flipped;
			request.data.locked = options.locked;
			request.data.unloadWhenPluginDisconnects = options.unloadWhenPluginDisconnects;
			this.Socket.Send<VTSItemLoadRequestData, VTSItemLoadResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemLoadResponseData> LoadItem(string fileName, VTSItemLoadOptions options) {
			return await VTSExtensions.Async<string, VTSItemLoadOptions, VTSItemLoadResponseData, VTSErrorData>(LoadItem, fileName, options);
		}

		public void LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemLoadRequestData request = new VTSItemLoadRequestData();
			request.data.fileName = fileName;
			request.data.positionX = options.positionX;
			request.data.positionY = options.positionY;
			request.data.size = options.size;
			request.data.rotation = options.rotation;
			request.data.fadeTime = options.fadeTime;
			request.data.order = options.order;
			request.data.failIfOrderTaken = options.failIfOrderTaken;
			request.data.smoothing = options.smoothing;
			request.data.censored = options.censored;
			request.data.flipped = options.flipped;
			request.data.locked = options.locked;
			request.data.unloadWhenPluginDisconnects = options.unloadWhenPluginDisconnects;
			// Custom Data item settings
			request.data.customDataBase64 = base64;
			request.data.customDataAskUserFirst = options.askUserFirst;
			request.data.customDataSkipAskingUserIfWhitelisted = options.skipAskingUserIfWhitelisted;
			request.data.customDataAskTimer = options.askTimer;
			this.Socket.Send<VTSItemLoadRequestData, VTSItemLoadResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemLoadResponseData> LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options) {
			return await VTSExtensions.Async<string, string, VTSCustomDataItemLoadOptions, VTSItemLoadResponseData, VTSErrorData>(LoadCustomDataItem, fileName, base64, options);
		}

		// Unload Item

		public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemUnloadRequestData request = new VTSItemUnloadRequestData();
			request.data.instanceIDs = options.itemInstanceIDs;
			request.data.fileNames = options.fileNames;
			request.data.unloadAllInScene = options.unloadAllInScene;
			request.data.unloadAllLoadedByThisPlugin = options.unloadAllLoadedByThisPlugin;
			request.data.allowUnloadingItemsLoadedByUserOrOtherPlugins = options.allowUnloadingItemsLoadedByUserOrOtherPlugins;
			this.Socket.Send<VTSItemUnloadRequestData, VTSItemUnloadResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemUnloadResponseData> UnloadItem(VTSItemUnloadOptions options) {
			return await VTSExtensions.Async<VTSItemUnloadOptions, VTSItemUnloadResponseData, VTSErrorData>(UnloadItem, options);
		}

		// Animate Item

		public void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemAnimationControlRequestData request = new VTSItemAnimationControlRequestData();
			request.data.itemInstanceID = itemInstanceID;
			request.data.framerate = options.framerate;
			request.data.frame = options.frame;
			request.data.brightness = options.brightness;
			request.data.opacity = options.opacity;
			request.data.setAutoStopFrames = options.setAutoStopFrames;
			request.data.autoStopFrames = options.autoStopFrames;
			request.data.setAnimationPlayState = options.setAnimationPlayState;
			request.data.animationPlayState = options.animationPlayState;
			this.Socket.Send<VTSItemAnimationControlRequestData, VTSItemAnimationControlResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemAnimationControlResponseData> AnimateItem(string itemInstanceId, VTSItemAnimationControlOptions options) {
			return await VTSExtensions.Async<string, VTSItemAnimationControlOptions, VTSItemAnimationControlResponseData, VTSErrorData>(AnimateItem, itemInstanceId, options);
		}

		// Move Item

		public void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemMoveRequestData request = new VTSItemMoveRequestData();
			request.data.itemsToMove = new VTSItemToMove[items.Length];
			for (int i = 0; i < items.Length; i++) {
				VTSItemMoveEntry entry = items[i];
				request.data.itemsToMove[i] = new VTSItemToMove(
					entry.itemInsanceID,
					entry.options.timeInSeconds,
					MotionCurveToString(entry.options.fadeMode),
					entry.options.positionX,
					entry.options.positionY,
					entry.options.size,
					entry.options.rotation,
					entry.options.order,
					entry.options.setFlip,
					entry.options.flip,
					entry.options.userCanStop
				);
			}
			this.Socket.Send<VTSItemMoveRequestData, VTSItemMoveResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSItemMoveResponseData> MoveItem(VTSItemMoveEntry[] items) {
			return await VTSExtensions.Async<VTSItemMoveEntry[], VTSItemMoveResponseData, VTSErrorData>(MoveItem, items);
		}

		// Pin/Unpin Items

		private void PinItem(string itemInstanceID, VTSItemAngleRelativityMode angleRelativeTo, VTSItemSizeRelativityMode sizeRelativeTo, VTSVertexPinMode vertexPinType, ArtMeshCoordinate pinInfo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemPinRequestData request = new VTSItemPinRequestData();
			request.data.pin = true;
			request.data.itemInstanceID = itemInstanceID;
			request.data.vertexPinType = vertexPinType;
			request.data.angleRelativeTo = angleRelativeTo;
			request.data.sizeRelativeTo = sizeRelativeTo;
			request.data.pinInfo = pinInfo;
			this.Socket.Send<VTSItemPinRequestData, VTSItemPinResponseData>(request, onSuccess, onError);
		}

		public void PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new ArtMeshCoordinate {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size
			};
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Center, coordinate, onSuccess, onError);
		}

		public async Task<VTSItemPinResponseData> PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo) {
			return await VTSExtensions.Async<string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, VTSItemPinResponseData, VTSErrorData>(
				PinItemToCenter, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}

		public void PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new ArtMeshCoordinate {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size
			};
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Random, coordinate, onSuccess, onError);
		}

		public async Task<VTSItemPinResponseData> PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo) {
			return await VTSExtensions.Async<string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, VTSItemPinResponseData, VTSErrorData>(
				PinItemToRandom, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}


		public void PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new ArtMeshCoordinate {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size,
			};
			coordinate.SetBarycentricCoodrinate(point);
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Provided, coordinate, onSuccess, onError);
		}

		public async Task<VTSItemPinResponseData> PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point) {
			return await VTSExtensions.Async<string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, BarycentricCoordinate, VTSItemPinResponseData, VTSErrorData>(
				PinItemToPoint, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point);
		}


		public void UnpinItem(string itemInsanceID, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemPinRequestData request = new VTSItemPinRequestData();
			request.data.pin = false;
			request.data.itemInstanceID = itemInsanceID;
			this.Socket.Send<VTSItemPinRequestData, VTSItemPinResponseData>(request, onSuccess, onError);
		}

		public async Task<VTSItemPinResponseData> UnpinItem(string itemInstanceID) {
			return await VTSExtensions.Async<string, VTSItemPinResponseData, VTSErrorData>(
				UnpinItem, itemInstanceID);
		}


		// Request Art Mesh Selection

		public void RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes, Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshSelectionRequestData request = new VTSArtMeshSelectionRequestData();
			request.data.textOverride = textOverride;
			request.data.helpOverride = helpOverride;
			request.data.requestedArtMeshCount = count;
			string[] array = new string[activeArtMeshes.Count];
			activeArtMeshes.CopyTo(array, 0);
			request.data.activeArtMeshes = array;
			this.Socket.Send<VTSArtMeshSelectionRequestData, VTSArtMeshSelectionResponseData>(request, onSuccess, onError);
		}
		public async Task<VTSArtMeshSelectionResponseData> RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes) {
			return await VTSExtensions.Async<string, string, int, ICollection<string>, VTSArtMeshSelectionResponseData, VTSErrorData>(
				RequestArtMeshSelection, textOverride, helpOverride, count, activeArtMeshes);
		}

		// Request Permissions

		public void RequestPermission(VTSPermission permission, Action<VTSPermissionResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPermissionRequestData request = new VTSPermissionRequestData();
			request.data.requestedPermission = permission;
			this.Socket.Send<VTSPermissionRequestData, VTSPermissionResponseData>(request, onSuccess, onError);
		}

		public async Task<VTSPermissionResponseData> RequestPermission(VTSPermission permission) {
			return await VTSExtensions.Async<VTSPermission, VTSPermissionResponseData, VTSErrorData>(RequestPermission, permission);
		}

		public void GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter, Action<VTSPostProcessingStateResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPostProcessingStateRequestData request = new VTSPostProcessingStateRequestData();
			request.data.fillPostProcessingPresetsArray = fillPostProcessingPresetsArray;
			request.data.fillPostProcessingEffectsArray = fillPostProcessingEffectsArray;
			request.data.effectIDFilter = effectIDFilter;
			this.Socket.Send<VTSPostProcessingStateRequestData, VTSPostProcessingStateResponseData>(request, onSuccess, onError);
		}

		public async Task<VTSPostProcessingStateResponseData> GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter) {
			return await VTSExtensions.Async<bool, bool, Effects[], VTSPostProcessingStateResponseData, VTSErrorData>(GetPostProcessingEffectStateList, fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter);
		}

		public void SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values, Action<VTSPostProcessingUpdateResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPostProcssingUpdateRequestData request = new VTSPostProcssingUpdateRequestData();
			request.data.postProcessingOn = options.postProcessingOn;
			request.data.setPostProcessingPreset = options.setPostProcessingPreset;
			request.data.setPostProcessingValues = options.setPostProcessingValues;
			request.data.presetToSet = options.presetToSet;
			request.data.postProcessingFadeTime = options.postProcessingFadeTime;
			request.data.setAllOtherValuesToDefault = options.setAllOtherValuesToDefault;
			request.data.usingRestrictedEffects = options.usingRestrictedEffects;
			request.data.randomizeAll = options.randomizeAll;
			request.data.randomizeAllChaosLevel = options.randomizeAllChaosLevel;
			request.data.postProcessingValues = values;
			this.Socket.Send<VTSPostProcssingUpdateRequestData, VTSPostProcessingUpdateResponseData>(request, onSuccess, onError);
		}

		public async Task<VTSPostProcessingUpdateResponseData> SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values) {
			return await VTSExtensions.Async<VTSPostProcessingUpdateOptions, PostProcessingValue[], VTSPostProcessingUpdateResponseData, VTSErrorData>(SetPostProcessingEffectValues, options, values);
		}

		#endregion

		#region VTS Event Subscription API Wrapper

		private void SubscribeToEvent<T, K, V>(bool subscribed, V config, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) where T : VTSEventSubscriptionRequestData<V>, new() where K : VTSEventData where V : VTSEventConfigData {
			T request = new T();
			request.SetSubscribed(subscribed);
			if (config != null) {
				request.SetConfig(config);
			}
			this.Socket.SendEventSubscription<T, K, V>(request, onEvent, onSubscribe, onError, () => {
				SubscribeToEvent<T, K, V>(subscribed, config, onEvent, onSubscribe, onError);
			});
		}
		private async Task<VTSEventSubscriptionResponseData> SubscribeToEventAsync<T, K, V>(bool subscribed, V config, Action<K> onEvent) where T : VTSEventSubscriptionRequestData<V>, new() where K : VTSEventData where V : VTSEventConfigData {
			return await VTSExtensions.Async<bool, V, Action<K>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToEvent<T, K, V>, subscribed, config, onEvent);
		}

		public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSUnsubscribeFromAllRequestData, VTSTestEventData, VTSTestEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromAllEvents() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromAllEvents);
		}

		// Test Event

		public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData, VTSTestEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent) {
			return await VTSExtensions.Async<VTSTestEventConfigOptions, Action<VTSTestEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToTestEvent, config, onEvent);
		}

		public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData, VTSTestEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromTestEventAsync() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromTestEvent);
		}

		// Model Loaded Event

		public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSModelLoadedEventData, VTSModelLoadedEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent) {
			return await VTSExtensions.Async<VTSModelLoadedEventConfigOptions, Action<VTSModelLoadedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelLoadedEvent, config, onEvent);
		}

		public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSTestEventData, VTSModelLoadedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelLoadedEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelLoadedEvent);
		}

		// Tracking Changed Event

		public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData, VTSTrackingEventConfigOptions>(true, new VTSTrackingEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent) {
			return await VTSExtensions.Async<Action<VTSTrackingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToTrackingEvent, onEvent);
		}

		public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData, VTSTrackingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromTrackingEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromTrackingEvent);
		}

		// Background Changed Event

		public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData, VTSBackgroundChangedEventConfigOptions>(true, new VTSBackgroundChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent) {
			return await VTSExtensions.Async<Action<VTSBackgroundChangedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToBackgroundChangedEvent, onEvent);
		}

		public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData, VTSBackgroundChangedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromBackgroundChangedEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromBackgroundChangedEvent);
		}

		// Model Config Changed Event

		public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData, VTSModelConfigChangedEventConfigOptions>(true, new VTSModelConfigChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent) {
			return await VTSExtensions.Async<Action<VTSModelConfigChangedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelConfigChangedEvent, onEvent);
		}

		public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData, VTSModelConfigChangedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelConfigChangedEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelConfigChangedEvent);
		}

		// Model Moved Event

		public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData, VTSModelMovedEventConfigOptions>(true, new VTSModelMovedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent) {
			return await VTSExtensions.Async<Action<VTSModelMovedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelMovedEvent, onEvent);
		}

		public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData, VTSModelMovedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelMovedEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelMovedEvent);
		}

		// Model Outline Event

		public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData, VTSModelOutlineEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent) {
			return await VTSExtensions.Async<VTSModelOutlineEventConfigOptions, Action<VTSModelOutlineEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelOutlineEvent, config, onEvent);
		}

		public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData, VTSModelOutlineEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelOutlineEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelOutlineEvent);
		}

		// Hotkey Triggered Event

		public void SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSHotkeyTriggeredEventSubscriptionRequestData, VTSHotkeyTriggeredEventData, VTSHotkeyTriggeredEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent) {
			return await VTSExtensions.Async<VTSHotkeyTriggeredEventConfigOptions, Action<VTSHotkeyTriggeredEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToHotkeyTriggeredEvent, config, onEvent);
		}

		public void UnsubscribeFromHotkeyTriggeredEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSHotkeyTriggeredEventSubscriptionRequestData, VTSHotkeyTriggeredEventData, VTSHotkeyTriggeredEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromHotkeyTriggeredEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromHotkeyTriggeredEvent);
		}

		// Animation Event Triggered Event

		public void SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelAnimationEventSubscriptionRequestData, VTSModelAnimationEventData, VTSModelAnimationEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent) {
			return await VTSExtensions.Async<VTSModelAnimationEventConfigOptions, Action<VTSModelAnimationEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelAnimationEvent, config, onEvent);
		}

		public void UnsubscribeFromModelAnimationEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelAnimationEventSubscriptionRequestData, VTSModelAnimationEventData, VTSModelAnimationEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelAnimationEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelAnimationEvent);
		}

		// Item Event

		public void SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSItemEventSubscriptionRequestData, VTSItemEventData, VTSItemEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent) {
			return await VTSExtensions.Async<VTSItemEventConfigOptions, Action<VTSItemEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToItemEvent, config, onEvent);
		}

		public void UnsubscribeFromItemEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSItemEventSubscriptionRequestData, VTSItemEventData, VTSItemEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromItemEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromItemEvent);
		}

		// Model Clicked Event

		public void SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelClickedEventSubscriptionRequestData, VTSModelClickedEventData, VTSModelClickedEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public async Task<VTSEventSubscriptionResponseData> SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent) {
			return await VTSExtensions.Async<VTSModelClickedEventConfigOptions, Action<VTSModelClickedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToModelClickedEvent, config, onEvent);
		}

		public void UnsubscribeFromModelClickedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelClickedEventSubscriptionRequestData, VTSModelClickedEventData, VTSModelClickedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelClickedEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromModelClickedEvent);
		}

		// Post Processing Event

		public void SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSPostProcessingEventSubscriptionRequestData, VTSPostProcessingEventData, VTSPostProcessingEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public async Task<VTSEventSubscriptionResponseData> SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent) {
			return await VTSExtensions.Async<VTSPostProcessingEventConfigOptions, Action<VTSPostProcessingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
				SubscribeToPostProcessingEvent, config, onEvent);
		}

		public void UnsubscribeFromPostProcessingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSPostProcessingEventSubscriptionRequestData, VTSPostProcessingEventData, VTSPostProcessingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public async Task<VTSEventSubscriptionResponseData> UnsubscribeFromPostProcessingEvent() {
			return await VTSExtensions.Async<VTSEventSubscriptionResponseData, VTSErrorData>(UnsubscribeFromPostProcessingEvent);
		}

		#endregion

		#region Helper Methods 

		/// <summary>
		/// Static VTS API callback method which does nothing. Saves you from needing to make a new inline function each time.
		/// </summary>
		/// <param name="response"></param>
		public static void DoNothingCallback(VTSMessageData response) {
			// Do nothing!
		}

		private static string InjectParameterModeToString(VTSInjectParameterMode mode) {
			if (mode == VTSInjectParameterMode.ADD) {
				return "add";
			} else if (mode == VTSInjectParameterMode.SET) {
				return "set";
			}
			return "set";
		}

		private static string MotionCurveToString(VTSItemMotionCurve curve) {
			if (curve == VTSItemMotionCurve.LINEAR) {
				return "linear";
			} else if (curve == VTSItemMotionCurve.EASE_IN) {
				return "easeIn";
			} else if (curve == VTSItemMotionCurve.EASE_OUT) {
				return "easeOut";
			} else if (curve == VTSItemMotionCurve.EASE_BOTH) {
				return "easeBoth";
			} else if (curve == VTSItemMotionCurve.OVERSHOOT) {
				return "overshoot";
			} else if (curve == VTSItemMotionCurve.ZIP) {
				return "zip";
			}
			return "linear";
		}

		private static readonly Regex ALPHANUMERIC = new Regex(@"\W|");
		private static string SanitizeParameterName(string name) {
			// between 4 and 32 chars, alphanumeric, underscores allowed
			string output = name;
			output = ALPHANUMERIC.Replace(output, "");
			output.PadLeft(4, 'X');
			output = output.Substring(0, Math.Min(output.Length, 31));
			return output;
		}

		#endregion
	}
}
