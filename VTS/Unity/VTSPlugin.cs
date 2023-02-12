using System;
using System.Collections.Generic;
using UnityEngine;

namespace VTS.Unity {

	/// <summary>
	/// The base class for VTS plugin creation in Unity.
	/// </summary>
	public abstract class VTSPlugin : MonoBehaviour, IVTSPlugin {

		#region Properties
		private VTS.Core.VTSPlugin _plugin;
		private VTS.Core.VTSPlugin Plugin {
			get {
				if (this._plugin == null) {
					this._plugin = new VTS.Core.VTSPlugin(this.Logger, this.PluginName, this.PluginAuthor, this.PluginIcon);
				}
				return this._plugin;
			}
		}

		[SerializeField]
		protected string _pluginName = "ExamplePlugin";
		public string PluginName { get { return this._pluginName; } }
		[SerializeField]
		protected string _pluginAuthor = "ExampleAuthor";
		public string PluginAuthor { get { return this._pluginAuthor; } }
		[SerializeField]
		protected Texture2D _pluginIcon = null;
		public string PluginIcon { get { return EncodeIcon(this._pluginIcon, this.Logger); } }

		/// <summary>
		/// The underlying WebSocket for connecting to VTS.
		/// </summary>
		/// <value></value>
		protected IVTSWebSocket Socket { get { return this.Plugin.Socket; } }

		public bool IsAuthenticated { get { return this.Plugin.IsAuthenticated; } }

		public IJsonUtility JsonUtility { get { return this.Plugin.JsonUtility; } }
		public ITokenStorage TokenStorage { get { return this.Plugin.TokenStorage; } }
		private IVTSLogger _logger = new UnityVTSLoggerImpl();
		public IVTSLogger Logger { get { return this._logger; } }

		#endregion

		#region Initialization

		public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, ITokenStorage tokenStorage, Action onConnect, Action onDisconnect, Action<VTSErrorData> onError) {
			this.Plugin.Initialize(webSocket, jsonUtility, tokenStorage, onConnect, onDisconnect, onError);
		}

		public void Disconnect() {
			this.Plugin.Disconnect();
		}

		#endregion

		#region Port Discovery

		public Dictionary<int, VTSStateBroadcastData> GetPorts() {
			return this.Plugin.GetPorts();
		}

		public int GetPort() {
			return this.Plugin.GetPort();
		}

		public bool SetPort(int port) {
			return this.Plugin.SetPort(port);
		}

		public bool SetIPAddress(string ipString) {
			return this.Plugin.SetIPAddress(ipString);
		}

		#endregion

		#region VTS General API Wrapper

		public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetAPIState(onSuccess, onError);
		}

		public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetStatistics(onSuccess, onError);
		}

		public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetFolderInfo(onSuccess, onError);
		}

		public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetCurrentModel(onSuccess, onError);
		}

		public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetAvailableModels(onSuccess, onError);
		}

		public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.LoadModel(modelID, onSuccess, onError);
		}

		public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.MoveModel(position, onSuccess, onError);
		}

		public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetHotkeysInCurrentModel(modelID, onSuccess, onError);
		}

		public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetHotkeysInLive2DItem(live2DItemFileName, onSuccess, onError);
		}

		public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
			request.data.hotkeyID = hotkeyID;
			this.Plugin.TriggerHotkey(hotkeyID, onSuccess, onError);
		}

		public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.TriggerHotkeyForLive2DItem(itemInstanceID, hotkeyID, onSuccess, onError);
		}

		public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetArtMeshList(onSuccess, onError);
		}

		public void TintArtMesh(Color32 tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.TintArtMesh(ColorToColorTint(tint), mixWithSceneLightingColor, matcher, onSuccess, onError);
		}

		public void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.TintArtMesh(tint, mixWithSceneLightingColor, matcher, onSuccess, onError);
		}

		public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetSceneColorOverlayInfo(onSuccess, onError);
		}

		public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetFaceFound(onSuccess, onError);
		}

		public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetInputParameterList(onSuccess, onError);
		}

		public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetParameterValue(parameterName, onSuccess, onError);
		}

		public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetLive2DParameterList(onSuccess, onError);
		}

		public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.AddCustomParameter(parameter, onSuccess, onError);
		}

		public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.RemoveCustomParameter(parameterName, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.InjectParameterValues(values, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.InjectParameterValues(values, mode, false, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.InjectParameterValues(values, mode, faceFound, onSuccess, onError);
		}

		public void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetExpressionStateList(onSuccess, onError);
		}

		public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.SetExpressionState(expression, active, onSuccess, onError);
		}

		public void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetCurrentModelPhysics(onSuccess, onError);
		}

		public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.SetCurrentModelPhysics(strengthOverrides, windOverrides, onSuccess, onError);
		}

		public void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.SetNDIConfig(config, onSuccess, onError);
		}

		public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.GetItemList(options, onSuccess, onError);
		}

		public void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.LoadItem(fileName, options, onSuccess, onError);
		}

		public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.UnloadItem(options, onSuccess, onError);
		}

		public void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.AnimateItem(itemInstanceID, options, onSuccess, onError);
		}

		public void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.MoveItem(items, onSuccess, onError);
		}

		public void RequestArtMeshSelection(string textOverride, string helpOverride, int count,
			ICollection<string> activeArtMeshes,
			Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError) {
			this.Plugin.RequestArtMeshSelection(textOverride, helpOverride, count, activeArtMeshes, onSuccess, onError);
		}

		#endregion

		#region VTS Event Subscription API Wrapper

		public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromAllEvents(onUnsubscribe, onError);
		}

		public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToTestEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromTestEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToModelLoadedEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromModelLoadedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToTrackingEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromTrackingEvent(onUnsubscribe, onError);
		}

		public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToBackgroundChangedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromBackgroundChangedEvent(onUnsubscribe, onError);
		}


		public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToModelConfigChangedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromModelConfigChangedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToModelMovedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromModelMovedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError) {
			this.Plugin.SubscribeToModelOutlineEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError) {
			this.Plugin.UnsubscribeFromModelOutlineEvent(onUnsubscribe, onError);
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Static VTS API callback method which does nothing. Saves you from needing to make a new inline function each time.
		/// </summary>
		/// <param name="messageData">The message to ignore.</param>
		public static void DoNothingCallback(VTSMessageData messageData) {
			// DO NOTHING!
		}

		private static string EncodeIcon(Texture2D icon, IVTSLogger logger) {
			try {
				if (icon.width != 128 && icon.height != 128) {
					logger.LogWarning("Icon resolution must be exactly 128*128 pixels!");
					return null;
				}
				return Convert.ToBase64String(icon.EncodeToPNG());
			}
			catch (Exception e) {
				logger.LogError(e);
			}
			return null;
		}

		/// <summary>
		/// Converts the VTS Pair struct to a Unity Vector2 struct.
		/// </summary>
		/// <param name="pair">The Pair to convert</param>
		/// <returns></returns>
		public static Vector2 PairToVector2(Pair pair) {
			return new Vector2(pair.x, pair.y);
		}

		/// <summary>
		/// Converts the VTS Color struct to a Unity Color32 struct.
		/// </summary>
		/// <param name="color">The color to convert</param>
		/// <returns></returns>
		public static Color32 ColorTintToColor(ColorTint color) {
			return new Color32(color.colorR, color.colorG, color.colorB, color.colorA);
		}

		/// <summary>
		/// Converts the Unity Color32 struct to a VTS ColorTint struct.
		/// </summary>
		/// <param name="color">The color to convert</param>
		/// <returns></returns>
		public static ColorTint ColorToColorTint(Color32 color) {
			return new ColorTint() {
				colorR = color.r,
				colorG = color.g,
				colorB = color.b,
				colorA = color.a
			};
		}

		#endregion
	}
}
