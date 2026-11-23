using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
		public IWebSocket Socket { get; private set; }

		private readonly VTSWebSocket _vtsSocket;

		private readonly CancellationTokenSource _cancelToken;
		private readonly Task _tickLoop = null;
		private readonly int _tickInterval = 100;

		/// <summary>
		/// Creates a new VTSPlugin.
		/// </summary>
		/// <param name="webSocket">The websocket transport layer.</param>
		/// <param name="jsonUtility">The mechanism for serializing and deserializing JSON.</param>
		/// <param name="tokenStorage">The mechanism for storing a VTube Studio authentication token.</param>
		/// <param name="logger">The logger implementation</param>
		/// <param name="updateIntervalMs">The number of milliseconds between each update cycle of the plugin.</param>
		/// <param name="pluginName">The plugin name. Must be between 3 and 32 characters.</param>
		/// <param name="pluginAuthor">The plugin author. Must be between 3 and 32 characters.</param>
		/// <param name="pluginIcon">The plugin icon, encoded as a base64 string. Must be 128*128 pixels exactly.</param>
		public CoreVTSPlugin(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, IVTSLogger logger, int updateIntervalMs, string pluginName, string pluginAuthor, string pluginIcon) {
			this.Socket = webSocket;
			this.TokenStorage = tokenStorage;
			this.JsonUtility = jsonUtility;
			this.Logger = logger;
			this._vtsSocket = new VTSWebSocket(webSocket, jsonUtility, logger);
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
			this._vtsSocket.Dispose();
		}

		#region Initialization

		public void Initialize(Action onConnect, Action onDisconnect, Action<VTSErrorData> onError) {
			this._vtsSocket.Initialize();
			void OnCombinedConnect() {
				this._vtsSocket.ResubscribeToEvents();
				onConnect();
			}
			this._vtsSocket.Connect(() => {
				// If API enabled, authenticate
				Authenticate(
					(r) => {
						if (!r.data.authenticated) {
							Reauthenticate(OnCombinedConnect, onError);
						} else {
							this.IsAuthenticated = true;
							OnCombinedConnect();
						}
					},
					(r) => {
						// If initial authentication fails, try again
						// (Likely just needs fresh token)
						Reauthenticate(OnCombinedConnect, onError);
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

		public Task InitializeAsync(Action onDisconnect) {
			var tcs = new TaskCompletionSource<object>();

			Initialize(
				() => tcs.SetResult(null),
				onDisconnect,
				error => tcs.SetException(error.ToException())
			);

			return tcs.Task;
		}

		public void Disconnect() {
			this._vtsSocket?.Disconnect();
		}

		private void Tick(float timeDelta) {
			this._vtsSocket?.Tick(timeDelta);
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
			this._vtsSocket.Send<VTSAuthData, VTSAuthData>(tokenRequest,
			(a) => {
				this._token = a.data.authenticationToken;
				this.TokenStorage?.SaveToken(this._token);
				UseToken(onSuccess, onError);
			},
			onError);
		}

		private void UseToken(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError) {
			VTSAuthData authRequest = new() {
				messageType = "AuthenticationRequest"
			};
			authRequest.data.pluginName = this.PluginName;
			authRequest.data.pluginDeveloper = this.PluginAuthor;
			authRequest.data.authenticationToken = this._token;
			this._vtsSocket.Send<VTSAuthData, VTSAuthData>(authRequest, onSuccess, onError);
		}

		#endregion

		#region Port Discovery

		public Dictionary<int, VTSStateBroadcastData> GetPorts() {
			return this._vtsSocket.GetPorts();
		}

		public int GetPort() {
			return this._vtsSocket.Port;
		}

		public bool SetPort(int port) {
			return this._vtsSocket.SetPort(port);
		}

		public bool SetIPAddress(string ipString) {
			return this._vtsSocket.SetIPAddress(ipString);
		}

		#endregion

		#region VTS General API Wrapper

		// Get API State

		public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSStateData request = new();
			this._vtsSocket.Send<VTSStateData, VTSStateData>(request, onSuccess, onError);
		}
		public Task<VTSStateData> GetAPIState() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSStateData, VTSErrorData>(
				static (plugin, onSuccess, onError) =>
				plugin.GetAPIState(onSuccess, onError),
				this);
		}

		// Get Statistics

		public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError) {
			VTSStatisticsData request = new();
			this._vtsSocket.Send<VTSStatisticsData, VTSStatisticsData>(request, onSuccess, onError);
		}
		public Task<VTSStatisticsData> GetStatistics() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSStatisticsData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
				plugin.GetStatistics(onSuccess, onError),
				this);
        }

		// Get Folder Info

		public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError) {
			VTSFolderInfoData request = new();
			this._vtsSocket.Send<VTSFolderInfoData, VTSFolderInfoData>(request, onSuccess, onError);
		}
		public Task<VTSFolderInfoData> GetFolderInfo() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSFolderInfoData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
				plugin.GetFolderInfo(onSuccess, onError),
				this);
        }

		// Get Current Model

		public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelData request = new();
			this._vtsSocket.Send<VTSCurrentModelData, VTSCurrentModelData>(request, onSuccess, onError);
		}
		public Task<VTSCurrentModelData> GetCurrentModel() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSCurrentModelData, VTSErrorData>(
				static (plugin, onSuccess, onError) =>
				plugin.GetCurrentModel(onSuccess, onError),
				this);
		}

		// Get Available Models

		public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError) {
			VTSAvailableModelsData request = new();
			this._vtsSocket.Send<VTSAvailableModelsData, VTSAvailableModelsData>(request, onSuccess, onError);
		}
		public Task<VTSAvailableModelsData> GetAvailableModels() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSAvailableModelsData, VTSErrorData>(
				static (plugin, onSuccess, onError) =>
				plugin.GetAvailableModels(onSuccess, onError),
				this);
		}

		// Load Model

		public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError) {
			VTSModelLoadData request = new();
			request.data.modelID = modelID;
			this._vtsSocket.Send<VTSModelLoadData, VTSModelLoadData>(request, onSuccess, onError);
		}
		public Task<VTSModelLoadData> LoadModel(string modelId) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSModelLoadData, VTSErrorData>(
				static (plugin, modelID, onSuccess, onError) =>
				plugin.LoadModel(modelID, onSuccess, onError),
				this, modelId);
		}

		// Move Model

		public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSMoveModelData request = new() {
				data = position
			};
			this._vtsSocket.Send<VTSMoveModelData, VTSMoveModelData>(request, onSuccess, onError);
		}
		public Task<VTSMoveModelData> MoveModel(VTSMoveModelData.Data position) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSMoveModelData.Data, VTSMoveModelData, VTSErrorData>(
                static (plugin, position, onSuccess, onError) =>
				plugin.MoveModel(position, onSuccess, onError),
				this, position);
		}

		// Get Hotkeys in Model

		public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new();
			request.data.modelID = modelID;
			this._vtsSocket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}
		public Task<VTSHotkeysInCurrentModelData> GetHotkeysInCurrentModel(string modelId) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSHotkeysInCurrentModelData, VTSErrorData>(
                static (plugin, modelID, onSuccess, onError) =>
				plugin.GetHotkeysInCurrentModel(modelID, onSuccess, onError),
				this, modelId);
        }

		// Get Hotkeys in Item

		public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeysInCurrentModelData request = new();
			request.data.live2DItemFileName = live2DItemFileName;
			this._vtsSocket.Send<VTSHotkeysInCurrentModelData, VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
		}
		public Task<VTSHotkeysInCurrentModelData> GetHotkeysInLive2DItem(string live2DItemFileName) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSHotkeysInCurrentModelData, VTSErrorData>(
                static (plugin, live2DItemFileName, onSuccess, onError) =>
				plugin.GetHotkeysInLive2DItem(live2DItemFileName, onSuccess, onError),
				this, live2DItemFileName);
        }

		// Trigger Hotkey		

		public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new();
			request.data.hotkeyID = hotkeyID;
			this._vtsSocket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}
		public Task<VTSHotkeyTriggerData> TriggerHotkey(string hotkeyId) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSHotkeyTriggerData, VTSErrorData>(
                static (plugin, hotkeyId, onSuccess, onError) =>
                plugin.TriggerHotkey(hotkeyId, onSuccess, onError),
                this, hotkeyId);
        }

		// Trigger Hotkey in Item

		public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new();
			request.data.hotkeyID = hotkeyID;
			request.data.itemInstanceID = itemInstanceID;
			this._vtsSocket.Send<VTSHotkeyTriggerData, VTSHotkeyTriggerData>(request, onSuccess, onError);
		}
		public Task<VTSHotkeyTriggerData> TriggerHotkeyForLive2DItem(string itemInstanceId, string hotkeyId) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, VTSHotkeyTriggerData, VTSErrorData>(
                static (plugin, itemInstanceId, hotkeyId, onSuccess, onError) =>
                plugin.TriggerHotkeyForLive2DItem(itemInstanceId, hotkeyId, onSuccess, onError),
                this, itemInstanceId, hotkeyId);
        }

		// Get Art Mesh List

		public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshListData request = new();
			this._vtsSocket.Send<VTSArtMeshListData, VTSArtMeshListData>(request, onSuccess, onError);
		}
		public Task<VTSArtMeshListData> GetArtMeshList() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSArtMeshListData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetArtMeshList(onSuccess, onError),
                this);
        }

		// Tint Art Mesh

		public void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError) {
			VTSColorTintData request = new();
			ArtMeshColorTint colorTint = new() {
				colorR = tint.colorR,
				colorG = tint.colorG,
				colorB = tint.colorB,
				colorA = tint.colorA,
				mixWithSceneLightingColor = System.Math.Min(1, System.Math.Max(mixWithSceneLightingColor, 0))
			};
			request.data.colorTint = colorTint;
			request.data.artMeshMatcher = matcher;
			this._vtsSocket.Send<VTSColorTintData, VTSColorTintData>(request, onSuccess, onError);
		}
		public Task<VTSColorTintData> TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher) {
			return VTSExtensions.Async<CoreVTSPlugin, ColorTint, float, ArtMeshMatcher, VTSColorTintData, VTSErrorData>(
                static (plugin, tint, mixWithSceneLightingColor, matcher, onSuccess, onError) =>
                plugin.TintArtMesh(tint, mixWithSceneLightingColor, matcher, onSuccess, onError),
                this, tint, mixWithSceneLightingColor, matcher);
		}

		// Get Scene Color Overlay Info

		public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError) {
			VTSSceneColorOverlayData request = new();
			this._vtsSocket.Send<VTSSceneColorOverlayData, VTSSceneColorOverlayData>(request, onSuccess, onError);
		}
		public Task<VTSSceneColorOverlayData> GetSceneColorOverlayInfo() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSSceneColorOverlayData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetSceneColorOverlayInfo(onSuccess, onError),
                this);
        }

		// Get Found Face

		public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError) {
			VTSFaceFoundData request = new();
			this._vtsSocket.Send<VTSFaceFoundData, VTSFaceFoundData>(request, onSuccess, onError);
		}
		public Task<VTSFaceFoundData> GetFaceFound() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSFaceFoundData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetFaceFound(onSuccess, onError),
                this);
        }

		// Get Input Parameter List

		public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSInputParameterListData request = new();
			this._vtsSocket.Send<VTSInputParameterListData, VTSInputParameterListData>(request, onSuccess, onError);
		}
		public Task<VTSInputParameterListData> GetInputParameterList() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSInputParameterListData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetInputParameterList(onSuccess, onError),
                this);
        }

		// Get Parameter Value

		public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterValueData request = new();
			request.data.name = parameterName;
			this._vtsSocket.Send<VTSParameterValueData, VTSParameterValueData>(request, onSuccess, onError);
		}
		public Task<VTSParameterValueData> GetParameterValue(string parameterName) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSParameterValueData, VTSErrorData>(
                static (plugin, parameterName, onSuccess, onError) =>
                plugin.GetParameterValue(parameterName, onSuccess, onError),
                this, parameterName);
        }

		// Get Live2D Parameter List

		public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError) {
			VTSLive2DParameterListData request = new();
			this._vtsSocket.Send<VTSLive2DParameterListData, VTSLive2DParameterListData>(request, onSuccess, onError);
		}
		public Task<VTSLive2DParameterListData> GetLive2DParameterList() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSLive2DParameterListData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetLive2DParameterList(onSuccess, onError),
                this);
        }

		// Add Custom Parameter

		public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterCreationData request = new();
			request.data.parameterName = SanitizeParameterName(parameter.parameterName);
			request.data.explanation = parameter.explanation;
			request.data.min = parameter.min;
			request.data.max = parameter.max;
			request.data.defaultValue = parameter.defaultValue;
			this._vtsSocket.Send<VTSParameterCreationData, VTSParameterCreationData>(request, onSuccess, onError);
		}
		public Task<VTSParameterCreationData> AddCustomParameter(VTSCustomParameter parameter) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSCustomParameter, VTSParameterCreationData, VTSErrorData>(
                static (plugin, parameter, onSuccess, onError) =>
                plugin.AddCustomParameter(parameter, onSuccess, onError),
                this, parameter);
        }

		// Remove Custom Parameter

		public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError) {
			VTSParameterDeletionData request = new();
			request.data.parameterName = SanitizeParameterName(parameterName);
			this._vtsSocket.Send<VTSParameterDeletionData, VTSParameterDeletionData>(request, onSuccess, onError);
		}
		public Task<VTSParameterDeletionData> RemoveCustomParameter(string parameterName) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSParameterDeletionData, VTSErrorData>(
                static (plugin, parameterName, onSuccess, onError) =>
                plugin.RemoveCustomParameter(parameterName, onSuccess, onError),
                this, parameterName);
        }

		// Inject Parameter Values

		public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, VTSInjectParameterMode.SET, false, onSuccess, onError);
		}
		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSParameterInjectionValue[], VTSInjectParameterData, VTSErrorData>(
				static (plugin, values, onSuccess, onError) =>
				plugin.InjectParameterValues(values, onSuccess, onError),
				this, values);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			InjectParameterValues(values, mode, false, onSuccess, onError);
		}
		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSParameterInjectionValue[], VTSInjectParameterMode, VTSInjectParameterData, VTSErrorData>(
                static (plugin, values, mode, onSuccess, onError) =>
                plugin.InjectParameterValues(values, mode, onSuccess, onError),
                this, values, mode);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			VTSInjectParameterData request = new();
			foreach (VTSParameterInjectionValue value in values) {
				value.id = SanitizeParameterName(value.id);
			}
			request.data.faceFound = faceFound;
			request.data.parameterValues = values;
			request.data.mode = InjectParameterModeToString(mode);
			this._vtsSocket.Send<VTSInjectParameterData, VTSInjectParameterData>(request, onSuccess, onError);
		}
		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSParameterInjectionValue[], VTSInjectParameterMode, bool, VTSInjectParameterData, VTSErrorData>(
                static (plugin, values, mode, faceFound, onSuccess, onError) =>
                plugin.InjectParameterValues(values, mode, faceFound, onSuccess, onError),
                this, values, mode, faceFound);
		}

		// Get Expression State List

		public void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionStateData request = new();
			request.data.details = true;
			this._vtsSocket.Send<VTSExpressionStateData, VTSExpressionStateData>(request, onSuccess, onError);
		}
		public Task<VTSExpressionStateData> GetExpressionStateList() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSExpressionStateData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetExpressionStateList(onSuccess, onError),
                this);
        }

		// Set Expression State

		public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError) {
			VTSExpressionActivationData request = new();
			request.data.expressionFile = expression;
			request.data.active = active;
			this._vtsSocket.Send<VTSExpressionActivationData, VTSExpressionActivationData>(request, onSuccess, onError);
		}
		public Task<VTSExpressionActivationData> SetExpressionState(string expression, bool active) {
			return VTSExtensions.Async<CoreVTSPlugin, string, bool, VTSExpressionActivationData, VTSErrorData>(
                static (plugin, expression, active, onSuccess, onError) =>
                plugin.SetExpressionState(expression, active, onSuccess, onError),
                this, expression, active);
		}

		// Get Current Model Physics

		public void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSCurrentModelPhysicsData request = new();
			this._vtsSocket.Send<VTSCurrentModelPhysicsData, VTSCurrentModelPhysicsData>(request, onSuccess, onError);
		}
		public Task<VTSCurrentModelPhysicsData> GetCurrentModelPhysics() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSCurrentModelPhysicsData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.GetCurrentModelPhysics(onSuccess, onError),
                this);
		}

		// Set Current Model Physics

		public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			VTSOverrideModelPhysicsData request = new();
			request.data.strengthOverrides = strengthOverrides;
			request.data.windOverrides = windOverrides;
			this._vtsSocket.Send<VTSOverrideModelPhysicsData, VTSOverrideModelPhysicsData>(request, onSuccess, onError);
		}
		public Task<VTSOverrideModelPhysicsData> SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSPhysicsOverride[], VTSPhysicsOverride[], VTSOverrideModelPhysicsData, VTSErrorData>(
                static (plugin, strengthOverrides, windOverrides, onSuccess, onError) =>
                plugin.SetCurrentModelPhysics(strengthOverrides, windOverrides, onSuccess, onError),
                this, strengthOverrides, windOverrides);
		}

		// Set NDI Config

		public void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError) {
			this._vtsSocket.Send<VTSNDIConfigData, VTSNDIConfigData>(config, onSuccess, onError);
		}
		public Task<VTSNDIConfigData> SetNDIConfig(VTSNDIConfigData config) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSNDIConfigData, VTSNDIConfigData, VTSErrorData>(
                static (plugin, config, onSuccess, onError) =>
                plugin.SetNDIConfig(config, onSuccess, onError),
                this, config);
        }

		// Get Item List

		public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemListRequestData request = new();
			request.data.includeAvailableSpots = options.includeAvailableSpots;
			request.data.includeItemInstancesInScene = options.includeItemInstancesInScene;
			request.data.includeAvailableItemFiles = options.includeAvailableItemFiles;
			request.data.onlyItemsWithFileName = options.onlyItemsWithFileName;
			request.data.onlyItemsWithInstanceID = options.onlyItemsWithInstanceID;
			this._vtsSocket.Send<VTSItemListRequestData, VTSItemListResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemListResponseData> GetItemList(VTSItemListOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSItemListOptions, VTSItemListResponseData, VTSErrorData>(
                static (plugin, options, onSuccess, onError) =>
                plugin.GetItemList(options, onSuccess, onError),
                this, options);
        }

		// Load Item

		public void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemLoadRequestData request = new();
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
			this._vtsSocket.Send<VTSItemLoadRequestData, VTSItemLoadResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemLoadResponseData> LoadItem(string fileName, VTSItemLoadOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSItemLoadOptions, VTSItemLoadResponseData, VTSErrorData>(
                static (plugin, fileName, options, onSuccess, onError) =>
                plugin.LoadItem(fileName, options, onSuccess, onError),
                this, fileName, options);
		}

		public void LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemLoadRequestData request = new();
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
			this._vtsSocket.Send<VTSItemLoadRequestData, VTSItemLoadResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemLoadResponseData> LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, VTSCustomDataItemLoadOptions, VTSItemLoadResponseData, VTSErrorData>(
                static (plugin, fileName, base64, options, onSuccess, onError) =>
                plugin.LoadCustomDataItem(fileName, base64, options, onSuccess, onError),
                this, fileName, base64, options);
		}

		// Unload Item

		public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemUnloadRequestData request = new();
			request.data.instanceIDs = options.itemInstanceIDs;
			request.data.fileNames = options.fileNames;
			request.data.unloadAllInScene = options.unloadAllInScene;
			request.data.unloadAllLoadedByThisPlugin = options.unloadAllLoadedByThisPlugin;
			request.data.allowUnloadingItemsLoadedByUserOrOtherPlugins = options.allowUnloadingItemsLoadedByUserOrOtherPlugins;
			this._vtsSocket.Send<VTSItemUnloadRequestData, VTSItemUnloadResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemUnloadResponseData> UnloadItem(VTSItemUnloadOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSItemUnloadOptions, VTSItemUnloadResponseData, VTSErrorData>(
                static (plugin, options, onSuccess, onError) =>
                plugin.UnloadItem(options, onSuccess, onError),
                this, options);
        }

		// Animate Item

		public void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemAnimationControlRequestData request = new();
			request.data.itemInstanceID = itemInstanceID;
			request.data.framerate = options.framerate;
			request.data.frame = options.frame;
			request.data.brightness = options.brightness;
			request.data.opacity = options.opacity;
			request.data.setAutoStopFrames = options.setAutoStopFrames;
			request.data.autoStopFrames = options.autoStopFrames;
			request.data.setAnimationPlayState = options.setAnimationPlayState;
			request.data.animationPlayState = options.animationPlayState;
			this._vtsSocket.Send<VTSItemAnimationControlRequestData, VTSItemAnimationControlResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemAnimationControlResponseData> AnimateItem(string itemInstanceId, VTSItemAnimationControlOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSItemAnimationControlOptions, VTSItemAnimationControlResponseData, VTSErrorData>(
                static (plugin, itemInstanceId, options, onSuccess, onError) =>
                plugin.AnimateItem(itemInstanceId, options, onSuccess, onError),
                this, itemInstanceId, options);
		}

		// Move Item

		public void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemMoveRequestData request = new();
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
			this._vtsSocket.Send<VTSItemMoveRequestData, VTSItemMoveResponseData>(request, onSuccess, onError);
		}
		public Task<VTSItemMoveResponseData> MoveItem(VTSItemMoveEntry[] items) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSItemMoveEntry[], VTSItemMoveResponseData, VTSErrorData>(
                static (plugin, items, onSuccess, onError) =>
                plugin.MoveItem(items, onSuccess, onError),
                this, items);
		}

		// Pin/Unpin Items

		private void PinItem(string itemInstanceID, VTSItemAngleRelativityMode angleRelativeTo, VTSItemSizeRelativityMode sizeRelativeTo, VTSVertexPinMode vertexPinType, ArtMeshCoordinate pinInfo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemPinRequestData request = new();
			request.data.pin = true;
			request.data.itemInstanceID = itemInstanceID;
			request.data.vertexPinType = vertexPinType;
			request.data.angleRelativeTo = angleRelativeTo;
			request.data.sizeRelativeTo = sizeRelativeTo;
			request.data.pinInfo = pinInfo;
			this._vtsSocket.Send<VTSItemPinRequestData, VTSItemPinResponseData>(request, onSuccess, onError);
		}

		public void PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new() {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size
			};
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Center, coordinate, onSuccess, onError);
		}

		public Task<VTSItemPinResponseData> PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, VTSItemPinResponseData, VTSErrorData>(
                static (plugin, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError) =>
                plugin.PinItemToCenter(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError),
                this, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}

		public void PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new() {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size
			};
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Random, coordinate, onSuccess, onError);
		}

		public Task<VTSItemPinResponseData> PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, VTSItemPinResponseData, VTSErrorData>(
                static (plugin, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError) =>
                plugin.PinItemToRandom(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError),
                this, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}


		public void PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			ArtMeshCoordinate coordinate = new() {
				modelID = modelID,
				artMeshID = artMeshID,
				angle = angle,
				size = size,
			};
			coordinate.SetBarycentricCoodrinate(point);
			PinItem(itemInstanceID, angleRelativeTo, sizeRelativeTo, VTSVertexPinMode.Provided, coordinate, onSuccess, onError);
		}

		public Task<VTSItemPinResponseData> PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, string, float, VTSItemAngleRelativityMode, float, VTSItemSizeRelativityMode, BarycentricCoordinate, VTSItemPinResponseData, VTSErrorData>(
                static (plugin, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point, onSuccess, onError) =>
                plugin.PinItemToPoint(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point, onSuccess, onError),
                this, itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point);
		}


		public void UnpinItem(string itemInsanceID, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemPinRequestData request = new();
			request.data.pin = false;
			request.data.itemInstanceID = itemInsanceID;
			this._vtsSocket.Send<VTSItemPinRequestData, VTSItemPinResponseData>(request, onSuccess, onError);
		}

		public Task<VTSItemPinResponseData> UnpinItem(string itemInstanceID) {
			return VTSExtensions.Async<CoreVTSPlugin, string, VTSItemPinResponseData, VTSErrorData>(
                static (plugin, itemInstanceID, onSuccess, onError) =>
                plugin.UnpinItem(itemInstanceID, onSuccess, onError),
                this, itemInstanceID);
		}


		// Request Art Mesh Selection

		public void RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes, Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshSelectionRequestData request = new();
			request.data.textOverride = textOverride;
			request.data.helpOverride = helpOverride;
			request.data.requestedArtMeshCount = count;
			string[] array = new string[activeArtMeshes.Count];
			activeArtMeshes.CopyTo(array, 0);
			request.data.activeArtMeshes = array;
			this._vtsSocket.Send<VTSArtMeshSelectionRequestData, VTSArtMeshSelectionResponseData>(request, onSuccess, onError);
		}
		public Task<VTSArtMeshSelectionResponseData> RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes) {
			return VTSExtensions.Async<CoreVTSPlugin, string, string, int, ICollection<string>, VTSArtMeshSelectionResponseData, VTSErrorData>(
                static (plugin, textOverride, helpOverride, count, activeArtMeshes, onSuccess, onError) =>
                plugin.RequestArtMeshSelection(textOverride, helpOverride, count, activeArtMeshes, onSuccess, onError),
                this, textOverride, helpOverride, count, activeArtMeshes);
		}

		// Request Permissions

		public void RequestPermission(VTSPermission permission, Action<VTSPermissionResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPermissionRequestData request = new();
			request.data.requestedPermission = permission;
			this._vtsSocket.Send<VTSPermissionRequestData, VTSPermissionResponseData>(request, onSuccess, onError);
		}

		public Task<VTSPermissionResponseData> RequestPermission(VTSPermission permission) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSPermission, VTSPermissionResponseData, VTSErrorData>(
                static (plugin, permission, onSuccess, onError) =>
                plugin.RequestPermission(permission, onSuccess, onError),
                this, permission);
		}

		// Get Post Processing State

		public void GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter, Action<VTSPostProcessingStateResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPostProcessingStateRequestData request = new();
			request.data.fillPostProcessingPresetsArray = fillPostProcessingPresetsArray;
			request.data.fillPostProcessingEffectsArray = fillPostProcessingEffectsArray;
			request.data.effectIDFilter = effectIDFilter;
			this._vtsSocket.Send<VTSPostProcessingStateRequestData, VTSPostProcessingStateResponseData>(request, onSuccess, onError);
		}

		public Task<VTSPostProcessingStateResponseData> GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter) {
			return VTSExtensions.Async<CoreVTSPlugin, bool, bool, Effects[], VTSPostProcessingStateResponseData, VTSErrorData>(
                static (plugin, fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter, onSuccess, onError) =>
                plugin.GetPostProcessingEffectStateList(fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter, onSuccess, onError),
                this, fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter);
		}

		// Set Post Processing Values

		public void SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values, Action<VTSPostProcessingUpdateResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSPostProcssingUpdateRequestData request = new();
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
			this._vtsSocket.Send<VTSPostProcssingUpdateRequestData, VTSPostProcessingUpdateResponseData>(request, onSuccess, onError);
		}

		public Task<VTSPostProcessingUpdateResponseData> SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSPostProcessingUpdateOptions, PostProcessingValue[], VTSPostProcessingUpdateResponseData, VTSErrorData>(
                static (plugin, options, values, onSuccess, onError) =>
                plugin.SetPostProcessingEffectValues(options, values, onSuccess, onError),
                this, options, values);
		}

		// Sort Item Within Model

		public void SortItemWithinModel(VTSItemSortOptions options, Action<VTSItemSortResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSItemSortRequestData request = new();
			request.data.itemInstanceID = options.itemInstanceID;

			request.data.frontOn = options.frontOn;
			request.data.backOn = options.backOn;

			request.data.setSplitPoint = ItemSortSplitPointToString(options.setSplitPoint);
			if (options.setSplitPoint == VTSItemSortSplitPoint.UseArtMeshID) {
				request.data.splitAt = options.splitAtArtmeshID;
			}

			request.data.setFrontOrder = ItemSortOrderToString(options.setFrontOrder);
			request.data.withinModelOrderFront = options.setFrontOrder == VTSItemSortOrder.UseArtMeshID
				? options.withinModelOrderFrontArtmeshID
				: ItemSortWithinModelOrderToString(options.withinModelOrderFront);

			request.data.setBackOrder = ItemSortOrderToString(options.setBackOrder);
			request.data.withinModelOrderBack = options.setBackOrder == VTSItemSortOrder.UseArtMeshID
				? options.withinModelOrderBackArtmeshID
				: ItemSortWithinModelOrderToString(options.withinModelOrderBack);

			this._vtsSocket.Send<VTSItemSortRequestData, VTSItemSortResponseData>(request, onSuccess, onError);
		}

		public Task<VTSItemSortResponseData> SortItemWithinModel(VTSItemSortOptions options) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSItemSortOptions, VTSItemSortResponseData, VTSErrorData>(
                static (plugin, options, onSuccess, onError) =>
                plugin.SortItemWithinModel(options, onSuccess, onError),
                this, options);
		}

		// Get Art Meshes At Position

		public void GetArtMeshesAtPosition(Pair position, float visualize, Action<VTSArtMeshAtPositionResponseData> onSuccess, Action<VTSErrorData> onError) {
			VTSArtMeshAtPositionRequestData request = new();
			request.data.x = position.x;
			request.data.y = position.y;
			request.data.visualize = visualize;
			this._vtsSocket.Send<VTSArtMeshAtPositionRequestData, VTSArtMeshAtPositionResponseData>(request, onSuccess, onError);
		}

		public Task<VTSArtMeshAtPositionResponseData> GetArtMeshesAtPosition(Pair position, float visualize) {
			return VTSExtensions.Async<CoreVTSPlugin, Pair, float, VTSArtMeshAtPositionResponseData, VTSErrorData>(
                static (plugin, position, visualize, onSuccess, onError) =>
                plugin.GetArtMeshesAtPosition(position, visualize, onSuccess, onError),
                this, position, visualize);
		}

		#endregion

		#region VTS Event Subscription API Wrapper

		private void SubscribeToEvent<R, E, C>(bool subscribed, C config, Action<E> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) where R : VTSEventSubscriptionRequestData<C>, new() where E : VTSEventData where C : VTSEventConfigData {
			R request = new();
			request.SetSubscribed(subscribed);
			if (config != null) {
				request.SetConfig(config);
			}
			this._vtsSocket.SendEventSubscription<R, E, C>(request, onEvent, onSubscribe, onError, () => {
				SubscribeToEvent<R, E, C>(subscribed, config, onEvent, onSubscribe, onError);
			});
		}
		private Task<VTSEventSubscriptionResponseData> SubscribeToEventAsync<T, K, V>(bool subscribed, V config, Action<K> onEvent) where T : VTSEventSubscriptionRequestData<V>, new() where K : VTSEventData where V : VTSEventConfigData {
			return VTSExtensions.Async<CoreVTSPlugin, bool, V, Action<K>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, subscribed, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToEvent<T, K, V>(subscribed, config, onEvent, onSuccess, onError),
                this, subscribed, config, onEvent);
		}

		public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSUnsubscribeFromAllRequestData, VTSTestEventData, VTSTestEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromAllEvents() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromAllEvents(onSuccess, onError),
                this);
		}

		// Test Event

		public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData, VTSTestEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSTestEventConfigOptions, Action<VTSTestEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToTestEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTestEventSubscriptionRequestData, VTSTestEventData, VTSTestEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromTestEventAsync() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromTestEvent(onSuccess, onError),
                this);
		}

		// Model Loaded Event

		public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSModelLoadedEventData, VTSModelLoadedEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSModelLoadedEventConfigOptions, Action<VTSModelLoadedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelLoadedEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelLoadedEventSubscriptionRequestData, VTSTestEventData, VTSModelLoadedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelLoadedEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelLoadedEvent(onSuccess, onError),
                this); 
		}

		// Tracking Changed Event

		public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData, VTSTrackingEventConfigOptions>(true, new VTSTrackingEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, Action<VTSTrackingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onEvent, onSuccess, onError) =>
                plugin.SubscribeToTrackingEvent(onEvent, onSuccess, onError),
                this, onEvent);
		}

		public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSTrackingEventSubscriptionRequestData, VTSTrackingEventData, VTSTrackingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromTrackingEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromTrackingEvent(onSuccess, onError),
                this);
		}

		// Background Changed Event

		public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData, VTSBackgroundChangedEventConfigOptions>(true, new VTSBackgroundChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, Action<VTSBackgroundChangedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onEvent, onSuccess, onError) =>
                plugin.SubscribeToBackgroundChangedEvent(onEvent, onSuccess, onError),
                this, onEvent);
		}

		public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSBackgroundChangedEventSubscriptionRequestData, VTSBackgroundChangedEventData, VTSBackgroundChangedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromBackgroundChangedEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromBackgroundChangedEvent(onSuccess, onError),
                this);
		}

		// Model Config Changed Event

		public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData, VTSModelConfigChangedEventConfigOptions>(true, new VTSModelConfigChangedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, Action<VTSModelConfigChangedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelConfigChangedEvent(onEvent, onSuccess, onError),
                this, onEvent);
		}

		public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelConfigChangedEventSubscriptionRequestData, VTSModelConfigChangedEventData, VTSModelConfigChangedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelConfigChangedEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelConfigChangedEvent(onSuccess, onError),
                this);
		}

		// Model Moved Event

		public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData, VTSModelMovedEventConfigOptions>(true, new VTSModelMovedEventConfigOptions(), onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, Action<VTSModelMovedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelMovedEvent(onEvent, onSuccess, onError),
                this, onEvent);
		}

		public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelMovedEventSubscriptionRequestData, VTSModelMovedEventData, VTSModelMovedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelMovedEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelMovedEvent(onSuccess, onError),
                this);
		}

		// Model Outline Event

		public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData, VTSModelOutlineEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSModelOutlineEventConfigOptions, Action<VTSModelOutlineEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelOutlineEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelOutlineEventSubscriptionRequestData, VTSModelOutlineEventData, VTSModelOutlineEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelOutlineEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelOutlineEvent(onSuccess, onError),
                this);
		}

		// Hotkey Triggered Event

		public void SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSHotkeyTriggeredEventSubscriptionRequestData, VTSHotkeyTriggeredEventData, VTSHotkeyTriggeredEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSHotkeyTriggeredEventConfigOptions, Action<VTSHotkeyTriggeredEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToHotkeyTriggeredEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromHotkeyTriggeredEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSHotkeyTriggeredEventSubscriptionRequestData, VTSHotkeyTriggeredEventData, VTSHotkeyTriggeredEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromHotkeyTriggeredEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromHotkeyTriggeredEvent(onSuccess, onError),
                this);
		}

		// Animation Event Triggered Event

		public void SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelAnimationEventSubscriptionRequestData, VTSModelAnimationEventData, VTSModelAnimationEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSModelAnimationEventConfigOptions, Action<VTSModelAnimationEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelAnimationEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromModelAnimationEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelAnimationEventSubscriptionRequestData, VTSModelAnimationEventData, VTSModelAnimationEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelAnimationEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelAnimationEvent(onSuccess, onError),
                this);
		}

		// Item Event

		public void SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSItemEventSubscriptionRequestData, VTSItemEventData, VTSItemEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSItemEventConfigOptions, Action<VTSItemEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToItemEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromItemEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSItemEventSubscriptionRequestData, VTSItemEventData, VTSItemEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}
		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromItemEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromItemEvent(onSuccess, onError),
                this);
		}

		// Model Clicked Event

		public void SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelClickedEventSubscriptionRequestData, VTSModelClickedEventData, VTSModelClickedEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSModelClickedEventConfigOptions, Action<VTSModelClickedEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToModelClickedEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromModelClickedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSModelClickedEventSubscriptionRequestData, VTSModelClickedEventData, VTSModelClickedEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelClickedEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromModelClickedEvent(onSuccess, onError),
                this);
		}

		// Post Processing Event

		public void SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSPostProcessingEventSubscriptionRequestData, VTSPostProcessingEventData, VTSPostProcessingEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSPostProcessingEventConfigOptions, Action<VTSPostProcessingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToPostProcessingEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromPostProcessingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSPostProcessingEventSubscriptionRequestData, VTSPostProcessingEventData, VTSPostProcessingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromPostProcessingEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromPostProcessingEvent(onSuccess, onError),
                this);
		}

		// Art Mesh Point Tracking Event

		public void SubscribeToArtMeshPointTrackingEvent(VTSArtMeshPointTrackingEventConfigOptions config, Action<VTSArtMeshPointTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSArtMeshPointTrackingEventSubscriptionRequestData, VTSArtMeshPointTrackingEventData, VTSArtMeshPointTrackingEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToArtMeshPointTrackingEvent(VTSArtMeshPointTrackingEventConfigOptions config, Action<VTSArtMeshPointTrackingEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSArtMeshPointTrackingEventConfigOptions, Action<VTSArtMeshPointTrackingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToArtMeshPointTrackingEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromArtMeshPointTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSArtMeshPointTrackingEventSubscriptionRequestData, VTSArtMeshPointTrackingEventData, VTSArtMeshPointTrackingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromArtMeshPointTrackingEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromArtMeshPointTrackingEvent(onSuccess, onError),
                this);
		}

		// Art Mesh Outline Tracking Event

		public void SubscribeToArtMeshOutlineTrackingEvent(VTSArtMeshOutlineTrackingEventConfigOptions config, Action<VTSArtMeshOutlineTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSArtMeshOutlineTrackingEventSubscriptionRequestData, VTSArtMeshOutlineTrackingEventData, VTSArtMeshOutlineTrackingEventConfigOptions>(true, config, onEvent, onSubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToArtMeshOutlineTrackingEvent(VTSArtMeshOutlineTrackingEventConfigOptions config, Action<VTSArtMeshOutlineTrackingEventData> onEvent) {
			return VTSExtensions.Async<CoreVTSPlugin, VTSArtMeshOutlineTrackingEventConfigOptions, Action<VTSArtMeshOutlineTrackingEventData>, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, config, onEvent, onSuccess, onError) =>
                plugin.SubscribeToArtMeshOutlineTrackingEvent(config, onEvent, onSuccess, onError),
                this, config, onEvent);
		}

		public void UnsubscribeFromArtMeshOutlineTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			SubscribeToEvent<VTSArtMeshOutlineTrackingEventSubscriptionRequestData, VTSArtMeshOutlineTrackingEventData, VTSArtMeshOutlineTrackingEventConfigOptions>(false, null, DoNothingCallback, onUnsubscribe, onError);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromArtMeshOutlineTrackingEvent() {
			return VTSExtensions.Async<CoreVTSPlugin, VTSEventSubscriptionResponseData, VTSErrorData>(
                static (plugin, onSuccess, onError) =>
                plugin.UnsubscribeFromArtMeshOutlineTrackingEvent(onSuccess, onError),
                this);
		}

		#endregion

		#region Helper Methods 

		/// <summary>
		/// Static VTS API callback method which does nothing. 
		/// Saves you from needing to make a new inline function each time.
		/// </summary>
		/// <param name="response"></param>
		public static void DoNothingCallback(VTSMessageData response) {
			// Do nothing!
		}

		private static string InjectParameterModeToString(VTSInjectParameterMode mode) {
			string result;
			switch (mode)
			{
				case VTSInjectParameterMode.SET:
				case VTSInjectParameterMode.UNKNOWN: return "set";
				case VTSInjectParameterMode.ADD: return "add";

                /// Note: see <see cref="MotionCurveToString"/> for reason
                /// why <see langword="default"/> state is implemented like this.
                default: result = "set"; break;
            }
			return result;
		}

		private static string MotionCurveToString(VTSItemMotionCurve curve) {
			string result;
			switch (curve) // Emits jump-table here - which is just 1 branch, instead of 5-6 branches.
			{
				case VTSItemMotionCurve.UNKNOW:
				case VTSItemMotionCurve.LINEAR: return "linear";
				case VTSItemMotionCurve.EASE_IN: return "easeIn";
				case VTSItemMotionCurve.EASE_OUT: return "easeOut";
				case VTSItemMotionCurve.EASE_BOTH: return "easeBoth";
				case VTSItemMotionCurve.OVERSHOOT: return "overshoot";
				case VTSItemMotionCurve.ZIP: return "zip";

				// We define default case using loval variable, so it won't be merged with LINEAR case in IL code.
				// If default case and LINEAR case will be merged - branch-predictor for jump-table will yield worse results.
				// Making sure that defaul state is different from any other state ensures switch-case's branch is predictable.
				// Predictable branch takes 4-5 instructions - unpredictable branches 12-15+ instructions (depending on source).
				default: result = "linear"; break;
            }
			return result;
		}

		private static string ItemSortSplitPointToString(VTSItemSortSplitPoint point) {
			string result;
			switch (point)
			{
				case VTSItemSortSplitPoint.Unchanged: return "Unchanged";
				case VTSItemSortSplitPoint.UseArtMeshID: return "UseArtMeshID";

                /// Note: see <see cref="MotionCurveToString"/> for reason
				/// why <see langword="default"/> state is implemented like this.
                default: result = "Unchanged"; break;
            }
			return result;
		}

		private static string ItemSortOrderToString(VTSItemSortOrder order) {
			string result;
			switch (order)
			{
				case VTSItemSortOrder.Unchanged: return "Unchanged";
				case VTSItemSortOrder.UseArtMeshID: return "UseArtMeshID";
				case VTSItemSortOrder.UseSpecialID: return "UseSpecialID";

                /// Note: see <see cref="MotionCurveToString"/> for reason
                /// why <see langword="default"/> state is implemented like this.
                default: result = "Unchanged"; break;
            }
			return result;
		}

		private static string ItemSortWithinModelOrderToString(VTSItemSortWithinModelOrder order) {
			string result;
			switch (order)
			{
				case VTSItemSortWithinModelOrder.FullyInFront: return "FullyInFront";
				case VTSItemSortWithinModelOrder.FullyInBack: return "FullyInBack";

                /// Note: see <see cref="MotionCurveToString"/> for reason
                /// why <see langword="default"/> state is implemented like this.
                default: result = "FullyInBack"; break;
            }
			return result;
		}

		private static readonly Regex ALPHANUMERIC = new(@"\W|");
		private static string SanitizeParameterName(string name) {
			// between 4 and 32 chars, alphanumeric, underscores allowed
			string output = name;
			output = ALPHANUMERIC.Replace(output, "");
			output.PadLeft(4, 'X');
			output = output.Substring(0, BranchlessMin(output.Length, 31));
			return output;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int BranchlessMin(int a, int b)
        {
            int difference = a - b;
            int signMask = difference >> 31;
            return b + (difference & signMask);
        }

        #endregion
    }
}
