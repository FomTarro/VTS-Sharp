using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VTS.Core {

	public class VTSPlugin : IVTSPlugin, IDisposable {

		private string _pluginName;
		public string PluginName { get { return this._pluginName; } }
		private string _pluginAuthor;
		public string PluginAuthor { get { return this._pluginName; } }
		private string _pluginIcon;
		public string PluginIcon { get { return this._pluginIcon; } }

		private bool _isAuthenticated = false;
		public bool IsAuthenticated { get { return this._isAuthenticated; } }

		private string _token = null;

		private ITokenStorage _tokenStorage;
		public ITokenStorage TokenStorage { get { return this._tokenStorage; } }
		private IJsonUtility _jsonUtility;
		public IJsonUtility JsonUtility { get { return this._jsonUtility; } }
		private IVTSLogger _logger;
		public IVTSLogger Logger { get { return this._logger; } }

		private IVTSWebSocket _socket;
		public IVTSWebSocket Socket { get { return this._socket; } }

		private CancellationTokenSource _cancelToken;
		private Task _tickLoop = null;
		

		public VTSPlugin(IVTSLogger logger, string pluginName, string pluginAuthor, string pluginIcon) {
			this._socket = new VTSWebSocket();
			this._logger = logger;
			this._pluginName = pluginName;
			this._pluginAuthor = pluginAuthor;
			this._pluginIcon = pluginIcon;
			this._cancelToken = new CancellationTokenSource();
			this._tickLoop = TickLoop(this._cancelToken.Token);
		}

		~VTSPlugin(){
			Dispose();
		}

		public void Dispose() {
			this.Logger.Log(string.Format("Disposing of VTS Plugin: {0}...", this.PluginName));
			this._cancelToken.Cancel();
		}

		#region Initialization

		public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, Action onConnect, Action onDisconnect, Action<VTSErrorData> onError) {
			this._tokenStorage = tokenStorage;
			this._jsonUtility = jsonUtility;
			this.Socket.Initialize(webSocket, this._jsonUtility, this._logger);
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
						}
						else {
							this._isAuthenticated = true;
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
				this._isAuthenticated = false;
				onDisconnect();
			},
			(e) => {
				VTSErrorData error = new VTSErrorData();
				error.data.errorID = ErrorID.InternalServerError;
				error.data.message = e.Message;
				this._isAuthenticated = false;
				onError(error);
			});
		}

		public void Disconnect() {
			if (this.Socket != null) {
				this.Socket.Disconnect();
			}
		}

		private void Tick(float timeDelta){
			if (this.Socket != null) {
				this.Socket.Tick(timeDelta);
			}
		}

		private async Task TickLoop(CancellationToken token) {
			this.Logger.Log(string.Format("Starting VTS Plugin processor for plugin: {0}...", this.PluginName));
			while(!token.IsCancellationRequested){
				// TODO: make this interval configurable
				Tick(0.1f);
				await Task.Delay(100);
			}
			this.Logger.Log(string.Format("Ending VTS Plugin processor for plugin: {0}...", this.PluginName));
		}

		#endregion

		#region Authentication

		private void Authenticate(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError) {
			this._isAuthenticated = false;
			if (this._tokenStorage != null) {
				this._token = this._tokenStorage.LoadToken();
				if (String.IsNullOrEmpty(this._token)) {
					GetToken(onSuccess, onError);
				}
				else {
					UseToken(onSuccess, onError);
				}
			}
			else {
				GetToken(onSuccess, onError);
			}
		}

		private void Reauthenticate(Action onConnect, Action<VTSErrorData> onError) {
			// Debug.LogWarning("Token expired, acquiring new token...");
			this._isAuthenticated = false;
			this._tokenStorage.DeleteToken();
			Authenticate(
				(t) => {
					this._isAuthenticated = true;
					onConnect();
				},
				(t) => {
					this._isAuthenticated = false;
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
				if (this._tokenStorage != null) {
					this._tokenStorage.SaveToken(this._token);
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
		public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSStateData request = new VTSStateData();
			this.Socket.Send<VTSStateData, VTSStateData>(request, onSuccess, onError);
		}

		public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError) {
			VTSStatisticsData request = new VTSStatisticsData();
			this.Socket.Send<VTSStatisticsData, VTSStatisticsData>(request, onSuccess, onError);
		}

		public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError) {
			VTSFolderInfoData request = new VTSFolderInfoData();
			this.Socket.Send<VTSFolderInfoData, VTSFolderInfoData>(request, onSuccess, onError);
		}

		public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelData request = new VTSCurrentModelData();
			this.Socket.Send<VTSCurrentModelData, VTSCurrentModelData>(request, onSuccess, onError);
		}

		public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError) {
			VTSAvailableModelsData request = new VTSAvailableModelsData();
			this.Socket.Send<VTSAvailableModelsData, VTSAvailableModelsData>(request, onSuccess, onError);
		}

		public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError) {
			VTSModelLoadData request = new VTSModelLoadData();
			request.data.modelID = modelID;
			this.Socket.Send<VTSModelLoadData, VTSModelLoadData>(request, onSuccess, onError);
		}

		public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSMoveModelData request = new VTSMoveModelData();
			request.data = position;
			this.Socket.Send<VTSMoveModelData, VTSMoveModelData>(request, onSuccess, onError);
		}

		public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
			request.data.modelID = modelID;
			this.Socket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}

		public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
			request.data.live2DItemFileName = live2DItemFileName;
			this.Socket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}

		public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
			request.data.hotkeyID = hotkeyID;
			this.Socket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}

		public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
			request.data.hotkeyID = hotkeyID;
			request.data.itemInstanceID = itemInstanceID;
			this.Socket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}

		public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshListData request = new VTSArtMeshListData();
			this.Socket.Send<VTSArtMeshListData, VTSArtMeshListData>(request, onSuccess, onError);
		}

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

		public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError) {
			VTSSceneColorOverlayData request = new VTSSceneColorOverlayData();
			this.Socket.Send<VTSSceneColorOverlayData, VTSSceneColorOverlayData>(request, onSuccess, onError);
		}

		public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError) {
			VTSFaceFoundData request = new VTSFaceFoundData();
			this.Socket.Send<VTSFaceFoundData, VTSFaceFoundData>(request, onSuccess, onError);
		}

		public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSInputParameterListData request = new VTSInputParameterListData();
			this.Socket.Send<VTSInputParameterListData, VTSInputParameterListData>(request, onSuccess, onError);
		}

		public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterValueData request = new VTSParameterValueData();
			request.data.name = parameterName;
			this.Socket.Send<VTSParameterValueData, VTSParameterValueData>(request, onSuccess, onError);
		}

		public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSLive2DParameterListData request = new VTSLive2DParameterListData();
			this.Socket.Send<VTSLive2DParameterListData, VTSLive2DParameterListData>(request, onSuccess, onError);
		}

		public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterCreationData request = new VTSParameterCreationData();
			request.data.parameterName = SanitizeParameterName(parameter.parameterName);
			request.data.explanation = parameter.explanation;
			request.data.min = parameter.min;
			request.data.max = parameter.max;
			request.data.defaultValue = parameter.defaultValue;
			this.Socket.Send<VTSParameterCreationData, VTSParameterCreationData>(request, onSuccess, onError);
		}

		public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterDeletionData request = new VTSParameterDeletionData();
			request.data.parameterName = SanitizeParameterName(parameterName);
			this.Socket.Send<VTSParameterDeletionData, VTSParameterDeletionData>(request, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, VTSInjectParameterMode.SET, false, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, mode, false, onSuccess, onError);
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

		public void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionStateData request = new VTSExpressionStateData();
			request.data.details = true;
			this.Socket.Send<VTSExpressionStateData, VTSExpressionStateData>(request, onSuccess, onError);
		}

		public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionActivationData request = new VTSExpressionActivationData();
			request.data.expressionFile = expression;
			request.data.active = active;
			this.Socket.Send<VTSExpressionActivationData, VTSExpressionActivationData>(request, onSuccess, onError);
		}

		public void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelPhysicsData request = new VTSCurrentModelPhysicsData();
			this.Socket.Send<VTSCurrentModelPhysicsData, VTSCurrentModelPhysicsData>(request, onSuccess, onError);
		}

		public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSOverrideModelPhysicsData request = new VTSOverrideModelPhysicsData();
			request.data.strengthOverrides = strengthOverrides;
			request.data.windOverrides = windOverrides;
			this.Socket.Send<VTSOverrideModelPhysicsData, VTSOverrideModelPhysicsData>(request, onSuccess, onError);
		}

		public void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError) {
			this.Socket.Send<VTSNDIConfigData, VTSNDIConfigData>(config, onSuccess, onError);
		}

		public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemListRequestData request = new VTSItemListRequestData();
			request.data.includeAvailableSpots = options.includeAvailableSpots;
			request.data.includeItemInstancesInScene = options.includeItemInstancesInScene;
			request.data.includeAvailableItemFiles = options.includeAvailableItemFiles;
			request.data.onlyItemsWithFileName = options.onlyItemsWithFileName;
			request.data.onlyItemsWithInstanceID = options.onlyItemsWithInstanceID;
			this.Socket.Send<VTSItemListRequestData, VTSItemListResponseData>(request, onSuccess, onError);
		}

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

		public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemUnloadRequestData request = new VTSItemUnloadRequestData();
			request.data.instanceIDs = options.itemInstanceIDs;
			request.data.fileNames = options.fileNames;
			request.data.unloadAllInScene = options.unloadAllInScene;
			request.data.unloadAllLoadedByThisPlugin = options.unloadAllLoadedByThisPlugin;
			request.data.allowUnloadingItemsLoadedByUserOrOtherPlugins = options.allowUnloadingItemsLoadedByUserOrOtherPlugins;
			this.Socket.Send<VTSItemUnloadRequestData, VTSItemUnloadResponseData>(request, onSuccess, onError);
		}

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

		#endregion

		#region VTS Event Subscription API Wrapper

		private void SubscribeToEvent<T, K>(string eventName, bool subscribed, VTSEventConfigData config, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) where T : VTSEventSubscriptionRequestData, new() where K : VTSEventData {
			T request = new T();
			request.SetEventName(eventName);
			request.SetSubscribed(subscribed);
			if (config != null) {
				request.SetConfig(config);
			}
			this.Socket.SendEventSubscription<T, K>(request, onEvent, onSubscribe, onError, () => {
				SubscribeToEvent<T, K>(eventName, subscribed, config, onEvent, onSubscribe, onError);
			});
		}

		public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>(null, false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>("TestEvent", true, config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData>("TestEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSModelLoadedEventData>("ModelLoadedEvent", true, config, onEvent, onSubscribe, onError);
		}
		public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSTestEventData>("ModelLoadedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData>("TrackingStatusChangedEvent", true, new VTSTrackingEventConfigOptions(), onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData>("TrackingStatusChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData>("BackgroundChangedEvent", true, new VTSBackgroundChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData>("BackgroundChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData>("ModelConfigChangedEvent", true, new VTSModelConfigChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData>("ModelConfigChangedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData>("ModelMovedEvent", true, new VTSModelMovedEventConfigOptions(), onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData>("ModelMovedEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData>("ModelOutlineEvent", true, config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData>("ModelOutlineEvent", false, null, DoNothingCallback, onUnsubscribe, onError);
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
			}
			else if (mode == VTSInjectParameterMode.SET) {
				return "set";
			}
			return "set";
		}

		private static string MotionCurveToString(VTSItemMotionCurve curve) {
			if (curve == VTSItemMotionCurve.LINEAR) {
				return "linear";
			}
			else if (curve == VTSItemMotionCurve.EASE_IN) {
				return "easeIn";
			}
			else if (curve == VTSItemMotionCurve.EASE_OUT) {
				return "easeOut";
			}
			else if (curve == VTSItemMotionCurve.EASE_BOTH) {
				return "easeBoth";
			}
			else if (curve == VTSItemMotionCurve.OVERSHOOT) {
				return "overshoot";
			}
			else if (curve == VTSItemMotionCurve.ZIP) {
				return "zip";
			}
			return "linear";
		}

		private static Regex ALPHANUMERIC = new Regex(@"\W|");
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
