#if GODOT

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using VTS.Core;

namespace VTS.Godot
{
	/// <summary>
	/// The base class for VTS plugin creation as a Node in Godot.
	/// </summary>
	[GlobalClass]
	public partial class GodotVTSPlugin : Node, IVTSPlugin
	{
		/// <summary>
		/// Struct that provides the implementations for the various dependencies needed.
		/// </summary>Description("The base class for VTube Studio plugin creation as a Node in Godot.")
		protected struct VTSPluginDependencies
		{
			public IWebSocket socket;
			public IJsonUtility jsonUtility;
			public ITokenStorage tokenStorage;
			public IVTSLogger logger;
		}

		/// <summary>
		/// Accessor which provides the implementations for the various dependencies needed for plugin creation.
		/// Default implementations are provided, but this accessor can be overridden in order to specify custom implementations.
		/// </summary>
		protected virtual VTSPluginDependencies DependencyImplementations
		{
			get
			{
				IVTSLogger logger = new GodotVTSLoggerImpl();
				return new()
				{
					socket = new WebSocketImpl(logger),
					jsonUtility = new NewtonsoftJsonUtilityImpl(),
					tokenStorage = new TokenStorageImpl(ProjectSettings.GlobalizePath("user://")),
					logger = logger
				};
			}
		}

		#region Properties
		private IVTSPlugin _plugin;
		private IVTSPlugin Plugin
		{
			get
			{
				if (this._plugin == null)
				{
					GD.Print("Making plugin...");
					VTSPluginDependencies deps = DependencyImplementations;
					this._plugin = new CoreVTSPlugin(
						deps.socket,
						deps.jsonUtility,
						deps.tokenStorage,
						deps.logger,
						(int)(1000f / Math.Max(30, Engine.MaxFps)),
						this.PluginName,
						this.PluginAuthor,
						EncodeIcon(this._pluginIcon, deps.logger)
					);
				}
				return this._plugin;
			}
		}

		public bool IsAuthenticated { get { return this.Plugin.IsAuthenticated; } }

		[Export]
		private string _pluginName = "ExamplePlugin";
		public string PluginName { get { return this._pluginName; } }
		[Export]
		private string _pluginAuthor = "ExampleAuthor";
		public string PluginAuthor { get { return this._pluginAuthor; } }
		[Export]
		private Texture2D _pluginIcon = null;
		public string PluginIcon { get { return EncodeIcon(this._pluginIcon, this.Logger); } }

		public IWebSocket Socket => this.Plugin.Socket;
		public IJsonUtility JsonUtility => this.Plugin.JsonUtility;
		public ITokenStorage TokenStorage => this.Plugin.TokenStorage;
		public IVTSLogger Logger => this.Plugin.Logger;

		#endregion

		#region Initialization

		public void Initialize(Action onConnect, Action onDisconnect, Action<VTSErrorData> onError)
		{
			this.Plugin.Initialize(onConnect, onDisconnect, onError);
		}

		public Task InitializeAsync(Action onDisconnect)
		{
			return this.Plugin.InitializeAsync(onDisconnect);
		}

		public void Disconnect()
		{
			this.Plugin.Disconnect();
		}

		#endregion

		#region Port Discovery

		public Dictionary<int, VTSStateBroadcastData> GetPorts()
		{
			return this.Plugin.GetPorts();
		}

		public int GetPort()
		{
			return this.Plugin.GetPort();
		}

		public bool SetPort(int port)
		{
			return this.Plugin.SetPort(port);
		}

		public bool SetIPAddress(string ipString)
		{
			return this.Plugin.SetIPAddress(ipString);
		}

		#endregion

		#region VTS General API Wrapper

		public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetAPIState(onSuccess, onError);
		}

		public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetStatistics(onSuccess, onError);
		}

		public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetFolderInfo(onSuccess, onError);
		}

		public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetCurrentModel(onSuccess, onError);
		}

		public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetAvailableModels(onSuccess, onError);
		}

		public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.LoadModel(modelID, onSuccess, onError);
		}

		public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.MoveModel(position, onSuccess, onError);
		}

		public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetHotkeysInCurrentModel(modelID, onSuccess, onError);
		}

		public void GetHotkeysInLive2DItem(string live2DItemFileName, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetHotkeysInLive2DItem(live2DItemFileName, onSuccess, onError);
		}

		public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.TriggerHotkey(hotkeyID, onSuccess, onError);
		}

		public void TriggerHotkeyForLive2DItem(string itemInstanceID, string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.TriggerHotkeyForLive2DItem(itemInstanceID, hotkeyID, onSuccess, onError);
		}

		public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetArtMeshList(onSuccess, onError);
		}

		public void TintArtMesh(Color tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.TintArtMesh(ColorToColorTint(tint), mixWithSceneLightingColor, matcher, onSuccess, onError);
		}

		public void TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.TintArtMesh(tint, mixWithSceneLightingColor, matcher, onSuccess, onError);
		}

		public void GetSceneColorOverlayInfo(Action<VTSSceneColorOverlayData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetSceneColorOverlayInfo(onSuccess, onError);
		}

		public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetFaceFound(onSuccess, onError);
		}

		public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetInputParameterList(onSuccess, onError);
		}

		public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetParameterValue(parameterName, onSuccess, onError);
		}

		public void GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter, Action<VTSPostProcessingStateResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetPostProcessingEffectStateList(fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter, onSuccess, onError);
		}

		public void SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values, Action<VTSPostProcessingUpdateResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.SetPostProcessingEffectValues(options, values, onSuccess, onError);
		}

		public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetLive2DParameterList(onSuccess, onError);
		}

		public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.AddCustomParameter(parameter, onSuccess, onError);
		}

		public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.RemoveCustomParameter(parameterName, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.InjectParameterValues(values, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.InjectParameterValues(values, mode, false, onSuccess, onError);
		}

		public void InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.InjectParameterValues(values, mode, faceFound, onSuccess, onError);
		}

		public void GetExpressionStateList(Action<VTSExpressionStateData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetExpressionStateList(onSuccess, onError);
		}

		public void SetExpressionState(string expression, bool active, Action<VTSExpressionActivationData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.SetExpressionState(expression, active, onSuccess, onError);
		}

		public void GetCurrentModelPhysics(Action<VTSCurrentModelPhysicsData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetCurrentModelPhysics(onSuccess, onError);
		}

		public void SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides, Action<VTSOverrideModelPhysicsData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.SetCurrentModelPhysics(strengthOverrides, windOverrides, onSuccess, onError);
		}

		public void SetNDIConfig(VTSNDIConfigData config, Action<VTSNDIConfigData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.SetNDIConfig(config, onSuccess, onError);
		}

		public void GetItemList(VTSItemListOptions options, Action<VTSItemListResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetItemList(options, onSuccess, onError);
		}

		public void LoadItem(string fileName, VTSItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.LoadItem(fileName, options, onSuccess, onError);
		}

		public void LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options, Action<VTSItemLoadResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.LoadCustomDataItem(fileName, base64, options, onSuccess, onError);
		}

		public void UnloadItem(VTSItemUnloadOptions options, Action<VTSItemUnloadResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.UnloadItem(options, onSuccess, onError);
		}

		public void AnimateItem(string itemInstanceID, VTSItemAnimationControlOptions options, Action<VTSItemAnimationControlResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.AnimateItem(itemInstanceID, options, onSuccess, onError);
		}

		public void MoveItem(VTSItemMoveEntry[] items, Action<VTSItemMoveResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.MoveItem(items, onSuccess, onError);
		}

		public void RequestArtMeshSelection(string textOverride, string helpOverride, int count,
			ICollection<string> activeArtMeshes,
			Action<VTSArtMeshSelectionResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.RequestArtMeshSelection(textOverride, helpOverride, count, activeArtMeshes, onSuccess, onError);
		}

		public void RequestPermission(VTSPermission permission, Action<VTSPermissionResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.RequestPermission(permission, onSuccess, onError);
		}

		public void PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.PinItemToCenter(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError);
		}

		public void PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.PinItemToRandom(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, onSuccess, onError);
		}
		public void PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.PinItemToPoint(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point, onSuccess, onError);
		}

		public void UnpinItem(string itemInsanceID, Action<VTSItemPinResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.UnpinItem(itemInsanceID, onSuccess, onError);
		}

		#endregion

		#region VTS Event Subscription API Wrapper

		public void UnsubscribeFromAllEvents(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromAllEvents(onUnsubscribe, onError);
		}

		public void SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToTestEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTestEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromTestEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelLoadedEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelLoadedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelLoadedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToTrackingEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromTrackingEvent(onUnsubscribe, onError);
		}

		public void SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToBackgroundChangedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromBackgroundChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromBackgroundChangedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelConfigChangedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelConfigChangedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelConfigChangedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelMovedEvent(onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelMovedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelMovedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelOutlineEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelOutlineEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelOutlineEvent(onUnsubscribe, onError);
		}

		public void SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToHotkeyTriggeredEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromHotkeyTriggeredEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromHotkeyTriggeredEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelAnimationEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelAnimationEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelAnimationEvent(onUnsubscribe, onError);
		}

		public void SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToItemEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromItemEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromItemEvent(onUnsubscribe, onError);
		}

		public void SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToModelClickedEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromModelClickedEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromModelClickedEvent(onUnsubscribe, onError);
		}

		public void SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToPostProcessingEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromPostProcessingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromPostProcessingEvent(onUnsubscribe, onError);
		}

		public void SortItemWithinModel(VTSItemSortOptions options, Action<VTSItemSortResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.SortItemWithinModel(options, onSuccess, onError);
		}

		public void GetArtMeshesAtPosition(Pair position, float visualize, Action<VTSArtMeshAtPositionResponseData> onSuccess, Action<VTSErrorData> onError)
		{
			this.Plugin.GetArtMeshesAtPosition(position, visualize, onSuccess, onError);
		}

		public void SubscribeToArtMeshPointTrackingEvent(VTSArtMeshPointTrackingEventConfigOptions config, Action<VTSArtMeshPointTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToArtMeshPointTrackingEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromArtMeshPointTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromArtMeshPointTrackingEvent(onUnsubscribe, onError);
		}

		public void SubscribeToArtMeshOutlineTrackingEvent(VTSArtMeshOutlineTrackingEventConfigOptions config, Action<VTSArtMeshOutlineTrackingEventData> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.SubscribeToArtMeshOutlineTrackingEvent(config, onEvent, onSubscribe, onError);
		}

		public void UnsubscribeFromArtMeshOutlineTrackingEvent(Action<VTSEventSubscriptionResponseData> onUnsubscribe, Action<VTSErrorData> onError)
		{
			this.Plugin.UnsubscribeFromArtMeshOutlineTrackingEvent(onUnsubscribe, onError);
		}

		#endregion

		#region Async/Await Wrappers

		public Task<VTSParameterCreationData> AddCustomParameter(VTSCustomParameter parameter)
		{
			return this.Plugin.AddCustomParameter(parameter);
		}

		public Task<VTSItemAnimationControlResponseData> AnimateItem(string itemInstanceId, VTSItemAnimationControlOptions options)
		{
			return this.Plugin.AnimateItem(itemInstanceId, options);
		}

		public Task<VTSStateData> GetAPIState()
		{
			return this.Plugin.GetAPIState();
		}

		public Task<VTSArtMeshListData> GetArtMeshList()
		{
			return this.Plugin.GetArtMeshList();
		}

		public Task<VTSAvailableModelsData> GetAvailableModels()
		{
			return this.Plugin.GetAvailableModels();
		}

		public Task<VTSCurrentModelData> GetCurrentModel()
		{
			return this.Plugin.GetCurrentModel();
		}

		public Task<VTSCurrentModelPhysicsData> GetCurrentModelPhysics()
		{
			return this.Plugin.GetCurrentModelPhysics();
		}

		public Task<VTSExpressionStateData> GetExpressionStateList()
		{
			return this.Plugin.GetExpressionStateList();
		}

		public Task<VTSFaceFoundData> GetFaceFound()
		{
			return this.Plugin.GetFaceFound();
		}

		public Task<VTSFolderInfoData> GetFolderInfo()
		{
			return this.Plugin.GetFolderInfo();
		}

		public Task<VTSHotkeysInCurrentModelData> GetHotkeysInCurrentModel(string modelId)
		{
			return this.Plugin.GetHotkeysInCurrentModel(modelId);
		}

		public Task<VTSHotkeysInCurrentModelData> GetHotkeysInLive2DItem(string live2DItemFileName)
		{
			return this.Plugin.GetHotkeysInLive2DItem(live2DItemFileName);
		}

		public Task<VTSInputParameterListData> GetInputParameterList()
		{
			return this.Plugin.GetInputParameterList();
		}

		public Task<VTSItemListResponseData> GetItemList(VTSItemListOptions options)
		{
			return this.Plugin.GetItemList(options);
		}

		public Task<VTSLive2DParameterListData> GetLive2DParameterList()
		{
			return this.Plugin.GetLive2DParameterList();
		}

		public Task<VTSParameterValueData> GetParameterValue(string parameterName)
		{
			return this.Plugin.GetParameterValue(parameterName);
		}

		public Task<VTSSceneColorOverlayData> GetSceneColorOverlayInfo()
		{
			return this.Plugin.GetSceneColorOverlayInfo();
		}

		public Task<VTSStatisticsData> GetStatistics()
		{
			return this.Plugin.GetStatistics();
		}

		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values)
		{
			return this.Plugin.InjectParameterValues(values);
		}

		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode)
		{
			return this.Plugin.InjectParameterValues(values, mode);
		}

		public Task<VTSInjectParameterData> InjectParameterValues(VTSParameterInjectionValue[] values, VTSInjectParameterMode mode, bool faceFound)
		{
			return this.Plugin.InjectParameterValues(values, mode, faceFound);
		}

		public Task<VTSItemLoadResponseData> LoadItem(string fileName, VTSItemLoadOptions options)
		{
			return this.Plugin.LoadItem(fileName, options);
		}

		public Task<VTSItemLoadResponseData> LoadCustomDataItem(string fileName, string base64, VTSCustomDataItemLoadOptions options)
		{
			return this.Plugin.LoadCustomDataItem(fileName, base64, options);
		}

		public Task<VTSModelLoadData> LoadModel(string modelId)
		{
			return this.Plugin.LoadModel(modelId);
		}

		public Task<VTSItemMoveResponseData> MoveItem(VTSItemMoveEntry[] items)
		{
			return this.Plugin.MoveItem(items);
		}

		public Task<VTSMoveModelData> MoveModel(VTSMoveModelData.Data position)
		{
			return this.Plugin.MoveModel(position);
		}

		public Task<VTSParameterDeletionData> RemoveCustomParameter(string parameterName)
		{
			return this.Plugin.RemoveCustomParameter(parameterName);
		}

		public Task<VTSArtMeshSelectionResponseData> RequestArtMeshSelection(string textOverride, string helpOverride, int count, ICollection<string> activeArtMeshes)
		{
			return this.Plugin.RequestArtMeshSelection(textOverride, helpOverride, count, activeArtMeshes);
		}

		public Task<VTSPermissionResponseData> RequestPermission(VTSPermission permission)
		{
			return this.Plugin.RequestPermission(permission);
		}

		public Task<VTSItemPinResponseData> PinItemToCenter(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo)
		{
			return this.Plugin.PinItemToCenter(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}

		public Task<VTSItemPinResponseData> PinItemToRandom(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo)
		{
			return this.Plugin.PinItemToRandom(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo);
		}

		public Task<VTSItemPinResponseData> PinItemToPoint(string itemInstanceID, string modelID, string artMeshID, float angle, VTSItemAngleRelativityMode angleRelativeTo, float size, VTSItemSizeRelativityMode sizeRelativeTo, BarycentricCoordinate point)
		{
			return this.Plugin.PinItemToPoint(itemInstanceID, modelID, artMeshID, angle, angleRelativeTo, size, sizeRelativeTo, point);
		}

		public Task<VTSItemPinResponseData> UnpinItem(string itemInstanceID)
		{
			return this.Plugin.UnpinItem(itemInstanceID);
		}

		public Task<VTSOverrideModelPhysicsData> SetCurrentModelPhysics(VTSPhysicsOverride[] strengthOverrides, VTSPhysicsOverride[] windOverrides)
		{
			return this.Plugin.SetCurrentModelPhysics(strengthOverrides, windOverrides);
		}

		public Task<VTSExpressionActivationData> SetExpressionState(string expression, bool active)
		{
			return this.Plugin.SetExpressionState(expression, active);
		}

		public Task<VTSNDIConfigData> SetNDIConfig(VTSNDIConfigData config)
		{
			return this.Plugin.SetNDIConfig(config);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToBackgroundChangedEvent(Action<VTSBackgroundChangedEventData> onEvent)
		{
			return this.Plugin.SubscribeToBackgroundChangedEvent(onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToHotkeyTriggeredEvent(VTSHotkeyTriggeredEventConfigOptions config, Action<VTSHotkeyTriggeredEventData> onEvent)
		{
			return this.Plugin.SubscribeToHotkeyTriggeredEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelAnimationEvent(VTSModelAnimationEventConfigOptions config, Action<VTSModelAnimationEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelAnimationEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelConfigChangedEvent(Action<VTSModelConfigChangedEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelConfigChangedEvent(onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelLoadedEvent(VTSModelLoadedEventConfigOptions config, Action<VTSModelLoadedEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelLoadedEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelMovedEvent(Action<VTSModelMovedEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelMovedEvent(onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelOutlineEvent(VTSModelOutlineEventConfigOptions config, Action<VTSModelOutlineEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelOutlineEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToTestEvent(VTSTestEventConfigOptions config, Action<VTSTestEventData> onEvent)
		{
			return this.Plugin.SubscribeToTestEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToTrackingEvent(Action<VTSTrackingEventData> onEvent)
		{
			return this.Plugin.SubscribeToTrackingEvent(onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToItemEvent(VTSItemEventConfigOptions config, Action<VTSItemEventData> onEvent)
		{
			return this.Plugin.SubscribeToItemEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToModelClickedEvent(VTSModelClickedEventConfigOptions config, Action<VTSModelClickedEventData> onEvent)
		{
			return this.Plugin.SubscribeToModelClickedEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToPostProcessingEvent(VTSPostProcessingEventConfigOptions config, Action<VTSPostProcessingEventData> onEvent)
		{
			return this.Plugin.SubscribeToPostProcessingEvent(config, onEvent);
		}

		public Task<VTSColorTintData> TintArtMesh(ColorTint tint, float mixWithSceneLightingColor, ArtMeshMatcher matcher)
		{
			return this.Plugin.TintArtMesh(tint, mixWithSceneLightingColor, matcher);
		}

		public Task<VTSHotkeyTriggerData> TriggerHotkey(string hotkeyId)
		{
			return this.Plugin.TriggerHotkey(hotkeyId);
		}

		public Task<VTSHotkeyTriggerData> TriggerHotkeyForLive2DItem(string itemInstanceId, string hotkeyId)
		{
			return this.Plugin.TriggerHotkeyForLive2DItem(itemInstanceId, hotkeyId);
		}

		public Task<VTSItemUnloadResponseData> UnloadItem(VTSItemUnloadOptions options)
		{
			return this.Plugin.UnloadItem(options);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromAllEvents()
		{
			return this.Plugin.UnsubscribeFromAllEvents();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromBackgroundChangedEvent()
		{
			return this.Plugin.UnsubscribeFromBackgroundChangedEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelConfigChangedEvent()
		{
			return this.Plugin.UnsubscribeFromModelConfigChangedEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelLoadedEvent()
		{
			return this.Plugin.UnsubscribeFromModelLoadedEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelMovedEvent()
		{
			return this.Plugin.UnsubscribeFromModelMovedEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelOutlineEvent()
		{
			return this.Plugin.UnsubscribeFromModelOutlineEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromTestEventAsync()
		{
			return this.Plugin.UnsubscribeFromTestEventAsync();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromTrackingEvent()
		{
			return this.Plugin.UnsubscribeFromTrackingEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromHotkeyTriggeredEvent()
		{
			return this.Plugin.UnsubscribeFromHotkeyTriggeredEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelAnimationEvent()
		{
			return this.Plugin.UnsubscribeFromModelAnimationEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromItemEvent()
		{
			return this.Plugin.UnsubscribeFromItemEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromModelClickedEvent()
		{
			return this.Plugin.UnsubscribeFromModelClickedEvent();
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromPostProcessingEvent()
		{
			return this.Plugin.UnsubscribeFromPostProcessingEvent();
		}

		public Task<VTSPostProcessingStateResponseData> GetPostProcessingEffectStateList(bool fillPostProcessingPresetsArray, bool fillPostProcessingEffectsArray, Effects[] effectIDFilter)
		{
			return this.Plugin.GetPostProcessingEffectStateList(fillPostProcessingPresetsArray, fillPostProcessingEffectsArray, effectIDFilter);
		}

		public Task<VTSPostProcessingUpdateResponseData> SetPostProcessingEffectValues(VTSPostProcessingUpdateOptions options, PostProcessingValue[] values)
		{
			return this.Plugin.SetPostProcessingEffectValues(options, values);
		}

		public Task<VTSItemSortResponseData> SortItemWithinModel(VTSItemSortOptions options)
		{
			return this.Plugin.SortItemWithinModel(options);
		}

		public Task<VTSArtMeshAtPositionResponseData> GetArtMeshesAtPosition(Pair position, float visualize)
		{
			return this.Plugin.GetArtMeshesAtPosition(position, visualize);
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToArtMeshPointTrackingEvent(VTSArtMeshPointTrackingEventConfigOptions config, Action<VTSArtMeshPointTrackingEventData> onEvent)
		{
			return this.Plugin.SubscribeToArtMeshPointTrackingEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromArtMeshPointTrackingEvent()
		{
			return this.Plugin.UnsubscribeFromArtMeshPointTrackingEvent();
		}

		public Task<VTSEventSubscriptionResponseData> SubscribeToArtMeshOutlineTrackingEvent(VTSArtMeshOutlineTrackingEventConfigOptions config, Action<VTSArtMeshOutlineTrackingEventData> onEvent)
		{
			return this.Plugin.SubscribeToArtMeshOutlineTrackingEvent(config, onEvent);
		}

		public Task<VTSEventSubscriptionResponseData> UnsubscribeFromArtMeshOutlineTrackingEvent()
		{
			return this.Plugin.UnsubscribeFromArtMeshOutlineTrackingEvent();
		}

		#endregion

		#region Helper Methods

		/// <summary>
		/// Static VTS API callback method which does nothing. Saves you from needing to make a new inline function each time.
		/// </summary>
		/// <param name="messageData">The message to ignore.</param>
		public static void DoNothingCallback(VTSMessageData messageData)
		{
			// DO NOTHING!
		}

		private static string EncodeIcon(Texture2D icon, IVTSLogger logger)
		{
			try
			{
				if (icon.GetWidth() != 128 && icon.GetHeight() != 128)
				{
					logger.LogWarning("Icon resolution must be exactly 128*128 pixels!");
					return null;
				}
				return Convert.ToBase64String(icon.GetImage().SavePngToBuffer());
			}
			catch (Exception e)
			{
				logger.LogError(e);
			}
			return null;
		}

		/// <summary>
		/// Converts the VTS Pair struct to a Godot Vector2 struct.
		/// </summary>
		/// <param name="pair">The Pair to convert</param>
		/// <returns></returns>
		public static Vector2 PairToVector2(Pair pair)
		{
			return new Vector2(pair.x, pair.y);
		}

		/// <summary>
		/// Converts the Godot Vector2 struct to a VTS Pair struct.
		/// </summary>
		/// <param name="pair">The Pair to convert</param>
		/// <returns></returns>
		public static Pair Vector2ToPair(Vector2 vector)
		{
			return new Pair(vector.X, vector.Y);
		}

		/// <summary>
		/// Converts the VTS ColorTint struct to a Godot Color struct.
		/// </summary>
		/// <param name="color">The color to convert</param>
		/// <returns></returns>
		public static Color ColorTintToColor(ColorTint color)
		{
			return new Color(
				color.colorR,
				color.colorG,
				color.colorB,
				color.colorA
			);
		}

		/// <summary>
		/// Converts the Godot Color struct to a VTS ColorTint struct.
		/// </summary>
		/// <param name="color">The color to convert</param>
		/// <returns></returns>
		public static ColorTint ColorToColorTint(Color color)
		{
			return new ColorTint()
			{
				colorR = (byte)(0xFF * color.R),
				colorG = (byte)(0xFF * color.G),
				colorB = (byte)(0xFF * color.B),
				colorA = (byte)(0xFF * color.A)
			};
		}

		#endregion

	}
}

#endif