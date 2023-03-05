using System;

namespace VTS {

	#region Common

	[System.Serializable]
	public class VTSMessageData {
		public string apiName = "VTubeStudioPublicAPI";
		public long timestamp;
		public string apiVersion = "1.0";
		public string requestID = Guid.NewGuid().ToString();
		public string messageType;
	}

	[System.Serializable]
	public class VTSErrorData : VTSMessageData {
		public VTSErrorData() {
			this.messageType = "APIError";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public ErrorID errorID;
			public string message;
		}
	}

	[System.Serializable]
	public struct Pair {
		public float x;
		public float y;
	}

	#endregion

	#region General API

	[System.Serializable]
	public class VTSStateData : VTSMessageData {
		public VTSStateData() {
			this.messageType = "APIStateRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool active;
			public string vTubeStudioVersion;
			public bool currentSessionAuthenticated;
		}
	}

	[System.Serializable]
	public class VTSAuthData : VTSMessageData {
		public VTSAuthData() {
			this.messageType = "AuthenticationTokenRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string pluginName;
			public string pluginDeveloper;
			public string pluginIcon;
			public string authenticationToken;
			public bool authenticated;
			public string reason;
		}
	}

	[System.Serializable]
	public class VTSStatisticsData : VTSMessageData {
		public VTSStatisticsData() {
			this.messageType = "StatisticsRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public long uptime;
			public int framerate;
			public int allowedPlugins;
			public int connectedPlugins;
			public bool startedWithSteam;
			public int windowWidth;
			public int windowHeight;
			public bool windowIsFullscreen;
		}
	}

	[System.Serializable]
	public class VTSFolderInfoData : VTSMessageData {
		public VTSFolderInfoData() {
			this.messageType = "VTSFolderInfoRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string models;
			public string backgrounds;
			public string items;
			public string config;
			public string logs;
			public string backup;
		}
	}

	[System.Serializable]
	public class ModelData {
		public bool modelLoaded;
		public string modelName;
		public string modelID;
	}

	[System.Serializable]
	public class VTSModelData : ModelData {
		public string vtsModelName;
		public string vtsModelIconName;
	}

	[System.Serializable]
	public class ModelPosition {
		public float positionX = float.NaN;
		public float positionY = float.NaN;
		public float rotation = float.NaN;
		public float size = float.NaN;

	}

	[System.Serializable]
	public class VTSCurrentModelData : VTSMessageData {
		public VTSCurrentModelData() {
			this.messageType = "CurrentModelRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSModelData {
			public string live2DModelName;
			public long modelLoadTime;
			public long timeSinceModelLoaded;
			public int numberOfLive2DParameters;
			public int numberOfLive2DArtmeshes;
			public bool hasPhysicsFile;
			public int numberOfTextures;
			public int textureResolution;
			public ModelPosition modelPosition;
		}
	}

	[System.Serializable]
	public class VTSAvailableModelsData : VTSMessageData {
		public VTSAvailableModelsData() {
			this.messageType = "AvailableModelsRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public int numberOfModels;
			public VTSModelData[] availableModels;
		}
	}

	[System.Serializable]
	public class VTSModelLoadData : VTSMessageData {
		public VTSModelLoadData() {
			this.messageType = "ModelLoadRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string modelID;
		}
	}

	[System.Serializable]
	public class VTSMoveModelData : VTSMessageData {
		public VTSMoveModelData() {
			this.messageType = "MoveModelRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : ModelPosition {
			public float timeInSeconds;
			public bool valuesAreRelativeToModel;
		}
	}

	[System.Serializable]
	public class HotkeyData {
		public string name;
		public HotkeyAction type;
		public string file;
		public string hotkeyID;
	}

	[System.Serializable]
	public class VTSHotkeysInCurrentModelData : VTSMessageData {
		public VTSHotkeysInCurrentModelData() {
			this.messageType = "HotkeysInCurrentModelRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool modelLoaded;
			public string modelName;
			public string modelID;
			public string live2DItemFileName;
			public HotkeyData[] availableHotkeys;
		}
	}

	[System.Serializable]
	public class VTSHotkeyTriggerData : VTSMessageData {
		public VTSHotkeyTriggerData() {
			this.messageType = "HotkeyTriggerRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string hotkeyID;
			public string itemInstanceID;
		}
	}

	[System.Serializable]
	public class VTSArtMeshListData : VTSMessageData {
		public VTSArtMeshListData() {
			this.messageType = "ArtMeshListRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool modelLoaded;
			public int numberOfArtMeshNames;
			public int numberOfArtMeshTags;
			public string[] artMeshNames;
			public string[] artMeshTags;
		}
	}

	// must be from 1-255
	[System.Serializable]
	public class ColorTint {
		public byte colorR;
		public byte colorG;
		public byte colorB;
		public byte colorA;

		/// <summary>
		/// Converts the color into a Unity color struct.
		/// </summary>
		/// <returns></returns>
		public UnityEngine.Color32 ToColor32() {
			return new UnityEngine.Color32(colorR, colorG, colorB, colorA);
		}

		/// <summary>
		/// Loads color data from a Unity color struct
		/// </summary>
		/// <param name="color"></param>
		public void FromColor32(UnityEngine.Color32 color) {
			this.colorA = color.a;
			this.colorB = color.b;
			this.colorG = color.g;
			this.colorR = color.r;
		}
	}

	[System.Serializable]
	public class ArtMeshColorTint : ColorTint {
		public float mixWithSceneLightingColor = 1.0f;
	}

	[System.Serializable]
	public class ArtMeshMatcher {
		public bool tintAll = true;
		public int[] artMeshNumber;
		public string[] nameExact;
		public string[] nameContains;
		public string[] tagExact;
		public string[] tagContains;
	}

	[System.Serializable]
	public class VTSColorTintData : VTSMessageData {
		public VTSColorTintData() {
			this.messageType = "ColorTintRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public ArtMeshColorTint colorTint;
			public ArtMeshMatcher artMeshMatcher;
			public int matchedArtMeshes;
		}
	}

	[System.Serializable]
	public class ColorCapturePart : ColorTint {
		public bool active;
	}

	[System.Serializable]
	public class VTSSceneColorOverlayData : VTSMessageData {
		public VTSSceneColorOverlayData() {
			this.messageType = "SceneColorOverlayInfoRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool active;
			public bool itemsIncluded;
			public bool isWindowCapture;
			public int baseBrightness;
			public int colorBoost;
			public int smoothing;
			public int colorOverlayR;
			public int colorOverlayG;
			public int colorOverlayB;
			public ColorCapturePart leftCapturePart;
			public ColorCapturePart middleCapturePart;
			public ColorCapturePart rightCapturePart;
		}
	}

	[System.Serializable]
	public class VTSFaceFoundData : VTSMessageData {
		public VTSFaceFoundData() {
			this.messageType = "FaceFoundRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool found;
		}
	}

	[System.Serializable]
	public class VTSParameter {
		public string name;
		public string addedBy;
		public float value;
		public float min;
		public float max;
		public float defaultValue;
	}

	[System.Serializable]
	public class VTSInputParameterListData : VTSMessageData {
		public VTSInputParameterListData() {
			this.messageType = "InputParameterListRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool modelLoaded;
			public string modelName;
			public string modelID;
			public VTSParameter[] customParameters;
			public VTSParameter[] defaultParameters;
		}
	}

	[System.Serializable]
	public class VTSParameterValueData : VTSMessageData {
		public VTSParameterValueData() {
			this.messageType = "ParameterValueRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSParameter { }
	}

	[System.Serializable]
	public class VTSLive2DParameterListData : VTSMessageData {
		public VTSLive2DParameterListData() {
			this.messageType = "Live2DParameterListRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool modelLoaded;
			public string modelName;
			public string modelID;
			public VTSParameter[] parameters;
		}
	}

	[System.Serializable]
	public class VTSCustomParameter {
		// 4-32 characters, alphanumeric
		public string parameterName;
		public string explanation;
		public float min;
		public float max;
		public float defaultValue;
	}

	[System.Serializable]
	public class VTSParameterCreationData : VTSMessageData {
		public VTSParameterCreationData() {
			this.messageType = "ParameterCreationRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSCustomParameter { }
	}

	[System.Serializable]
	public class VTSParameterDeletionData : VTSMessageData {
		public VTSParameterDeletionData() {
			this.messageType = "ParameterDeletionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string parameterName;
		}
	}

	[System.Serializable]
	public class VTSParameterInjectionValue {
		public string id;
		public float value = float.NaN;
		public float weight = float.NaN;
	}

	[System.Serializable]
	public enum VTSInjectParameterMode : int {
		UNKNOWN = -1,
		SET = 0,
		ADD = 1
	}

	[System.Serializable]
	public class VTSInjectParameterData : VTSMessageData {
		public VTSInjectParameterData() {
			this.messageType = "InjectParameterDataRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string mode;
			public bool faceFound;
			public VTSParameterInjectionValue[] parameterValues;
		}
	}

	[System.Serializable]
	public class ExpressionData {
		public string name;
		public string file;
		public bool active;
		public bool deactivateWhenKeyIsLetGo;
		public bool autoDeactivateAfterSeconds;
		public float secondsRemaining;
		public HotkeyData[] usedInHotkeys;
		public VTSParameter[] parameters;
	}

	[System.Serializable]
	public class VTSExpressionStateData : VTSMessageData {
		public VTSExpressionStateData() {
			this.messageType = "ExpressionStateRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool details;
			public string expressionFile;
			public bool modelLoaded;
			public string modelName;
			public string modelID;
			public ExpressionData[] expressions;

		}
	}

	[System.Serializable]
	public class VTSExpressionActivationData : VTSMessageData {
		public VTSExpressionActivationData() {
			this.messageType = "ExpressionActivationRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string expressionFile;
			public bool active;
		}
	}

	[System.Serializable]
	public class VTSCurrentModelPhysicsData : VTSMessageData {
		public VTSCurrentModelPhysicsData() {
			this.messageType = "GetCurrentModelPhysicsRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool modelLoaded;
			public string modelName;
			public string modelID;
			public bool modelHasPhysics;
			public bool physicsSwitchedOn;
			public bool usingLegacyPhysics;
			public int physicsFPSSetting;
			public int baseStrength;
			public int baseWind;
			public bool apiPhysicsOverrideActive;
			public string apiPhysicsOverridePluginName;
			public VTSPhysicsGroup[] physicsGroups;
		}
	}

	[System.Serializable]
	public class VTSOverrideModelPhysicsData : VTSMessageData {
		public VTSOverrideModelPhysicsData() {
			this.messageType = "SetCurrentModelPhysicsRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public VTSPhysicsOverride[] strengthOverrides;
			public VTSPhysicsOverride[] windOverrides;
		}
	}

	[System.Serializable]
	public class VTSPhysicsGroup {
		public string groupID;
		public string groupName;
		public float strengthMultiplier;
		public float windMultiplier;
	}

	[System.Serializable]
	public class VTSPhysicsOverride {
		public string id;
		public float value;
		public bool setBaseValue;
		public float overrideSeconds;
	}


	[System.Serializable]
	public class VTSNDIConfigData : VTSMessageData {
		public VTSNDIConfigData() {
			this.messageType = "NDIConfigRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool setNewConfig;
			public bool ndiActive;
			public bool useNDI5;
			public bool useCustomResolution;
			public int customWidthNDI;
			public int customHeightNDI;

		}
	}

	[System.Serializable]
	public class VTSStateBroadcastData : VTSMessageData {
		public VTSStateBroadcastData() {
			this.messageType = "VTubeStudioAPIStateBroadcast";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool active;
			public int port;
			public string instanceID;
			public string windowTitle;
		}
	}

	/// <summary>
	/// A container for holding the numerous retrieval options for an Item List request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-items-or-items-in-scene</a>
	/// </summary>
	[System.Serializable]
	public class VTSItemListOptions {
		public VTSItemListOptions() {
			this.includeAvailableSpots = false;
			this.includeItemInstancesInScene = false;
			this.includeAvailableItemFiles = false;
			this.onlyItemsWithFileName = string.Empty;
			this.onlyItemsWithInstanceID = string.Empty;
		}

		public VTSItemListOptions(
			bool includeAvailableSpots,
			bool includeItemInstancesInScene,
			bool includeAvailableItemFiles,
			string onlyItemsWithFileName,
			string onlyItemsWithInstanceID
		) {
			this.includeAvailableSpots = includeAvailableSpots;
			this.includeItemInstancesInScene = includeItemInstancesInScene;
			this.includeAvailableItemFiles = includeAvailableItemFiles;
			this.onlyItemsWithFileName = onlyItemsWithFileName;
			this.onlyItemsWithInstanceID = onlyItemsWithInstanceID;
		}

		public bool includeAvailableSpots;
		public bool includeItemInstancesInScene;
		public bool includeAvailableItemFiles;
		public string onlyItemsWithFileName;
		public string onlyItemsWithInstanceID;
	}

	[System.Serializable]
	public class ItemInstance {
		public string fileName;
		public string instanceID;
		public int order;
		public string type;
		public bool censored;
		public bool flipped;
		public bool locked;
		public float smoothing;
		public float framerate;
		public int frameCount;
		public int currentFrame;
		public bool pinnedToModel;
		public string pinnedModelID;
		public string pinnedArtMeshID;
		public string groupName;
		public string sceneName;
		public bool fromWorkshop;
	}

	[System.Serializable]
	public class ItemFile {
		public string fileName;
		public string type;
		public int loadedCount;
	}

	[System.Serializable]
	public class VTSItemListRequestData : VTSMessageData {
		public VTSItemListRequestData() {
			this.messageType = "ItemListRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool includeAvailableSpots;
			public bool includeItemInstancesInScene;
			public bool includeAvailableItemFiles;
			public string onlyItemsWithFileName;
			public string onlyItemsWithInstanceID;
		}
	}

	[System.Serializable]
	public class VTSItemListResponseData : VTSMessageData {
		public VTSItemListResponseData() {
			this.messageType = "ItemListResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public int itemsInSceneCount;
			public int totalItemsAllowedCount;
			public bool canLoadItemsRightNow;
			public int[] availableSpots;
			public ItemInstance[] itemInstancesInScene;
			public ItemFile[] availableItemFiles;
		}
	}

	/// <summary>
	/// A container for holding the numerous loading options for an Item Load request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene">https://github.com/DenchiSoft/VTubeStudio#loading-item-into-the-scene</a>
	/// </summary>
	[System.Serializable]
	public class VTSItemLoadOptions {
		public VTSItemLoadOptions() {
			this.positionX = 0;
			this.positionY = 0;
			this.size = 0.32f;
			this.rotation = 0f;
			this.fadeTime = 0;
			this.order = 1;
			this.failIfOrderTaken = false;
			this.smoothing = 0f;
			this.censored = false;
			this.flipped = false;
			this.locked = false;
			this.unloadWhenPluginDisconnects = true;
		}

		public VTSItemLoadOptions(
			float positionX,
			float positionY,
			float size,
			float rotation,
			float fadeTime,
			int order,
			bool failIfOrderTaken,
			float smoothing,
			bool censored,
			bool flipped,
			bool locked,
			bool unloadWhenPluginDisconnects
		) {
			this.positionX = positionX;
			this.positionY = positionY;
			this.size = size;
			this.rotation = rotation;
			this.fadeTime = fadeTime;
			this.order = order;
			this.failIfOrderTaken = failIfOrderTaken;
			this.smoothing = smoothing;
			this.censored = censored;
			this.flipped = flipped;
			this.locked = locked;
			this.unloadWhenPluginDisconnects = unloadWhenPluginDisconnects;
		}

		public float positionX;
		public float positionY;
		public float size;
		public float rotation;
		public float fadeTime;
		public int order;
		public bool failIfOrderTaken;
		public float smoothing;
		public bool censored;
		public bool flipped;
		public bool locked;
		public bool unloadWhenPluginDisconnects;
	}

	[System.Serializable]
	public class VTSItemLoadRequestData : VTSMessageData {
		public VTSItemLoadRequestData() {
			this.messageType = "ItemLoadRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string fileName;
			public float positionX;
			public float positionY;
			public float size;
			public float rotation;
			public float fadeTime;
			public int order;
			public bool failIfOrderTaken;
			public float smoothing;
			public bool censored;
			public bool flipped;
			public bool locked;
			public bool unloadWhenPluginDisconnects;
		}
	}

	[System.Serializable]
	public class VTSItemLoadResponseData : VTSMessageData {
		public VTSItemLoadResponseData() {
			this.messageType = "ItemLoadResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string instanceID;
		}
	}

	/// <summary>
	/// A container for holding the numerous unloading options for an Item Unload request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene">https://github.com/DenchiSoft/VTubeStudio#removing-item-from-the-scene</a>
	/// </summary>
	[System.Serializable]
	public class VTSItemUnloadOptions {
		public VTSItemUnloadOptions() {
			this.itemInstanceIDs = new string[0];
			this.fileNames = new string[0];
			this.unloadAllInScene = false;
			this.unloadAllLoadedByThisPlugin = false;
			this.allowUnloadingItemsLoadedByUserOrOtherPlugins = false;
		}

		public VTSItemUnloadOptions(
			string[] itemInstanceIDs,
			string[] fileNames,
			bool unloadAllInScene,
			bool unloadAllLoadedByThisPlugin,
			bool allowUnloadingItemsLoadedByUserOrOtherPlugins
		) {
			this.itemInstanceIDs = itemInstanceIDs;
			this.fileNames = fileNames;
			this.unloadAllInScene = unloadAllInScene;
			this.unloadAllLoadedByThisPlugin = unloadAllLoadedByThisPlugin;
			this.allowUnloadingItemsLoadedByUserOrOtherPlugins = allowUnloadingItemsLoadedByUserOrOtherPlugins;
		}

		public string[] itemInstanceIDs;
		public string[] fileNames;
		public bool unloadAllInScene;
		public bool unloadAllLoadedByThisPlugin;
		public bool allowUnloadingItemsLoadedByUserOrOtherPlugins;
	}

	[System.Serializable]
	public class UnloadedItem {
		public string instanceID;
		public string fileName;
	}

	[System.Serializable]
	public class VTSItemUnloadRequestData : VTSMessageData {
		public VTSItemUnloadRequestData() {
			this.messageType = "ItemUnloadRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool unloadAllInScene;
			public bool unloadAllLoadedByThisPlugin;
			public bool allowUnloadingItemsLoadedByUserOrOtherPlugins;
			public string[] instanceIDs;
			public string[] fileNames;
		}
	}

	[System.Serializable]
	public class VTSItemUnloadResponseData : VTSMessageData {
		public VTSItemUnloadResponseData() {
			this.messageType = "ItemUnloadResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public UnloadedItem[] unloadedItems;
		}
	}

	/// <summary>
	/// A container for holding the numerous animation options for an Item Animation Control request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations">https://github.com/DenchiSoft/VTubeStudio#controling-items-and-item-animations</a>
	/// </summary>
	[System.Serializable]
	public class VTSItemAnimationControlOptions {
		public VTSItemAnimationControlOptions() {
			this.framerate = -1;
			this.frame = -1;
			this.brightness = -1;
			this.opacity = -1;
			this.setAutoStopFrames = false;
			this.autoStopFrames = new int[0];
			this.setAnimationPlayState = false;
			this.animationPlayState = false;
		}

		public VTSItemAnimationControlOptions(
			int framerate,
			int frame,
			float brightness,
			float opacity,
			bool setAutoStopFrames,
			int[] autoStopFrames,
			bool setAnimationPlayState,
			bool animationPlayState
		) {
			this.framerate = framerate;
			this.frame = frame;
			this.brightness = brightness;
			this.opacity = opacity;
			this.setAutoStopFrames = setAutoStopFrames;
			this.autoStopFrames = autoStopFrames;
			this.setAnimationPlayState = setAnimationPlayState;
			this.animationPlayState = animationPlayState;
		}

		public int framerate;
		public int frame;
		public float brightness;
		public float opacity;
		public bool setAutoStopFrames;
		public int[] autoStopFrames;
		public bool setAnimationPlayState;
		public bool animationPlayState;
	}

	[System.Serializable]
	public class VTSItemAnimationControlRequestData : VTSMessageData {
		public VTSItemAnimationControlRequestData() {
			this.messageType = "ItemAnimationControlRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string itemInstanceID;
			public int framerate;
			public int frame;
			public float brightness;
			public float opacity;
			public bool setAutoStopFrames;
			public int[] autoStopFrames;
			public bool setAnimationPlayState;
			public bool animationPlayState;
		}
	}

	[System.Serializable]
	public class VTSItemAnimationControlResponseData : VTSMessageData {
		public VTSItemAnimationControlResponseData() {
			this.messageType = "ItemAnimationControlResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public int frame;
			public bool animationPlaying;
		}
	}

	[System.Serializable]
	public enum VTSItemMotionCurve : int {
		UNKNOW = -1,
		LINEAR = 0,
		EASE_IN = 1,
		EASE_OUT = 2,
		EASE_BOTH = 3,
		OVERSHOOT = 4,
		ZIP = 5
	}

	/// <summary>
	/// A container for holding the numerous movement options for an Item Move request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
	/// </summary>
	[System.Serializable]
	public class VTSItemMoveOptions {
		public VTSItemMoveOptions() {
			this.timeInSeconds = 0f;
			this.fadeMode = VTSItemMotionCurve.LINEAR;
			this.positionX = -1000;
			this.positionY = -1000;
			this.size = -1000;
			this.rotation = -1000;
			this.order = -1000;
			this.setFlip = false;
			this.flip = false;
			this.userCanStop = false;
		}

		public VTSItemMoveOptions(
			float timeInSeconds,
			VTSItemMotionCurve fadeMode,
			float positionX,
			float positionY,
			float size,
			float rotation,
			int order,
			bool setFlip,
			bool flip,
			bool userCanStop
		) {
			this.timeInSeconds = timeInSeconds;
			this.fadeMode = fadeMode;
			this.positionX = positionX;
			this.positionY = positionY;
			this.size = size;
			this.rotation = rotation;
			this.order = order;
			this.setFlip = setFlip;
			this.flip = flip;
			this.userCanStop = userCanStop;
		}

		public float timeInSeconds;
		public VTSItemMotionCurve fadeMode;
		public float positionX;
		public float positionY;
		public int order;
		public float size;
		public float rotation;
		public bool setFlip;
		public bool flip;
		public bool userCanStop;
	}

	/// <summary>
	/// A container for linking an Item Instance ID to its corresponding options for an Item Move request.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene">https://github.com/DenchiSoft/VTubeStudio#moving-items-in-the-scene</a>
	/// </summary>
	[System.Serializable]
	public struct VTSItemMoveEntry {
		public VTSItemMoveEntry(string itemInsanceID, VTSItemMoveOptions options) {
			this.itemInsanceID = itemInsanceID;
			this.options = options;
		}

		public string itemInsanceID;
		public VTSItemMoveOptions options;
	}

	[System.Serializable]
	public struct VTSItemToMove {
		public VTSItemToMove(
			string itemInstanceID,
			float timeInSeconds,
			string fadeMode,
			float positionX,
			float positionY,
			float size,
			float rotation,
			int order,
			bool setFlip,
			bool flip,
			bool userCanStop
		) {
			this.itemInstanceID = itemInstanceID;
			this.timeInSeconds = timeInSeconds;
			this.fadeMode = fadeMode;
			this.positionX = positionX;
			this.positionY = positionY;
			this.size = size;
			this.rotation = rotation;
			this.order = order;
			this.setFlip = setFlip;
			this.flip = flip;
			this.userCanStop = userCanStop;
		}

		public string itemInstanceID;
		public float timeInSeconds;
		public string fadeMode;
		public float positionX;
		public float positionY;
		public int order;
		public float size;
		public float rotation;
		public bool setFlip;
		public bool flip;
		public bool userCanStop;
	}

	[System.Serializable]
	public class VTSItemMoveRequestData : VTSMessageData {
		public VTSItemMoveRequestData() {
			this.messageType = "ItemMoveRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public VTSItemToMove[] itemsToMove;
		}
	}

	[System.Serializable]
	public struct MovedItem {
		public string itemInstanceID;
		public bool success;
		public ErrorID errorID;
	}

	[System.Serializable]
	public class VTSItemMoveResponseData : VTSMessageData {
		public VTSItemMoveResponseData() {
			this.messageType = "ItemMoveResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public MovedItem[] movedItems;
		}
	}

	[System.Serializable]
	public class VTSArtMeshSelectionRequestData : VTSMessageData {
		public VTSArtMeshSelectionRequestData() {
			this.messageType = "ArtMeshSelectionRequest";
			this.data = new Data();

		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string textOverride;
			public string helpOverride;
			public int requestedArtMeshCount;
			public string[] activeArtMeshes;

		}
	}

	[System.Serializable]
	public class VTSArtMeshSelectionResponseData : VTSMessageData {
		public VTSArtMeshSelectionResponseData() {
			this.messageType = "ArtMeshSelectionResponse";
			this.data = new Data();

		}
		public Data data;

		[System.Serializable]
		public class Data {

			public bool success;
			public string[] activeArtMeshes;
			public string[] inactiveArtMeshes;
		}
	}

	#endregion

	#region Event API

	[System.Serializable]
	public abstract class VTSEventSubscriptionRequestData : VTSMessageData {
		public abstract void SetEventName(string eventName);
		public abstract string GetEventName();
		public abstract void SetSubscribed(bool subscribe);
		public abstract bool GetSubscribed();
		public abstract void SetConfig(VTSEventConfigData config);
	}

	[System.Serializable]
	public abstract class VTSEventSubscriptonData {
		public string eventName;
		public bool subscribe;
	}

	[System.Serializable]
	public abstract class VTSEventConfigData { }

	[System.Serializable]
	public abstract class VTSEventData : VTSMessageData { }

	[System.Serializable]
	public class VTSEventSubscriptionResponseData : VTSMessageData {
		public VTSEventSubscriptionResponseData() {
			this.messageType = "EventSubscriptionResponse";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public int subscribedEventCount;
			public string[] subscribedEvents;
		}
	}

	// Test Event

	[System.Serializable]
	public class VTSTestEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSTestEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSTestEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSTestEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Test Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#test-event</a>
	/// </summary>
	[System.Serializable]
	public class VTSTestEventConfigOptions : VTSEventConfigData {
		public VTSTestEventConfigOptions() {
			this.testMessageForEvent = null;
		}

		public VTSTestEventConfigOptions(string message) {
			this.testMessageForEvent = message;
		}
		public string testMessageForEvent;
	}

	[System.Serializable]
	public class VTSTestEventData : VTSEventData {
		public VTSTestEventData() {
			this.messageType = "TestEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string yourTestMessage;
			public long counter;
		}
	}

	// Model Loaded Event

	[System.Serializable]
	public class VTSModelLoadedEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSModelLoadedEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSModelLoadedEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = "ModelLoadedEvent";
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSModelLoadedEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Model Loaded Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-loadedunloaded</a>
	/// </summary>
	[System.Serializable]
	public class VTSModelLoadedEventConfigOptions : VTSEventConfigData {
		public VTSModelLoadedEventConfigOptions() {
			this.modelID = null;
		}
		public VTSModelLoadedEventConfigOptions(string modelID) {
			this.modelID = modelID;
		}
		public string modelID;
	}

	[System.Serializable]
	public class VTSModelLoadedEventData : VTSEventData {
		public VTSModelLoadedEventData() {
			this.messageType = "ModelLoadedEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : ModelData { }
	}

	// Tracking Changed Event

	[System.Serializable]
	public class VTSTrackingEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSTrackingEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSTrackingEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSTrackingEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Lost Tracking Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#lostfound-tracking</a>
	/// </summary>
	[System.Serializable]
	public class VTSTrackingEventConfigOptions : VTSEventConfigData {
		public VTSTrackingEventConfigOptions() { }
	}

	[System.Serializable]
	public class VTSTrackingEventData : VTSEventData {
		public VTSTrackingEventData() {
			this.messageType = "TrackingStatusChangedEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public bool faceFound;
			public bool leftHandFound;
			public bool rightHandFound;
		}
	}

	// Background Changed Event

	[System.Serializable]
	public class VTSBackgroundChangedEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSBackgroundChangedEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSBackgroundChangedEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSBackgroundChangedEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Background Changed Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#background-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#background-changed</a>
	/// </summary>
	[System.Serializable]
	public class VTSBackgroundChangedEventConfigOptions : VTSEventConfigData {
		public VTSBackgroundChangedEventConfigOptions() { }
	}

	[System.Serializable]
	public class VTSBackgroundChangedEventData : VTSEventData {
		public VTSBackgroundChangedEventData() {
			this.messageType = "BackgroundChangedEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string backgroundName;
		}
	}

	// Model Config Changed Event

	[System.Serializable]
	public class VTSModelConfigChangedEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSModelConfigChangedEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSModelConfigChangedEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSModelConfigChangedEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Model Config Changed Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-config-modified</a>
	/// </summary>
	[System.Serializable]
	public class VTSModelConfigChangedEventConfigOptions : VTSEventConfigData {
		public VTSModelConfigChangedEventConfigOptions() { }
	}

	[System.Serializable]
	public class VTSModelConfigChangedEventData : VTSEventData {
		public VTSModelConfigChangedEventData() {
			this.messageType = "ModelConfigChangedEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string modelID;
			public string modelName;
			public bool hotkeyConfigChanged;
		}
	}

	// Model Moved Event

	[System.Serializable]
	public class VTSModelMovedEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSModelMovedEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSModelMovedEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSModelMovedEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Model Config Changed Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-movedresizedrotated</a>
	/// </summary>
	[System.Serializable]
	public class VTSModelMovedEventConfigOptions : VTSEventConfigData {
		public VTSModelMovedEventConfigOptions() { }
	}

	[System.Serializable]
	public class VTSModelMovedEventData : VTSEventData {
		public VTSModelMovedEventData() {
			this.messageType = "ModelMovedEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string modelID;
			public string modelName;
			public ModelPosition modelPosition;
		}
	}

	// Model Outline Event

	[System.Serializable]
	public class VTSModelOutlineEventSubscriptionRequestData : VTSEventSubscriptionRequestData {
		public VTSModelOutlineEventSubscriptionRequestData() {
			this.messageType = "EventSubscriptionRequest";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data : VTSEventSubscriptonData {
			public VTSModelOutlineEventConfigOptions config;
		}

		public override void SetEventName(string eventName) {
			this.data.eventName = eventName;
		}

		public override string GetEventName() {
			return this.data.eventName;
		}

		public override void SetSubscribed(bool subscribe) {
			this.data.subscribe = subscribe;
		}

		public override bool GetSubscribed() {
			return this.data.subscribe;
		}

		public override void SetConfig(VTSEventConfigData config) {
			this.data.config = (VTSModelOutlineEventConfigOptions)config;
		}
	}

	/// <summary>
	/// A container for providing subscription options for a Model Outline Event subscription.
	/// 
	/// For more info about what each field does, see 
	/// <a href="https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed">https://github.com/DenchiSoft/VTubeStudio/blob/master/Events/README.md#model-outline-changed</a>
	/// </summary>
	[System.Serializable]
	public class VTSModelOutlineEventConfigOptions : VTSEventConfigData {
		public VTSModelOutlineEventConfigOptions() {
			this.draw = false;
		}

		public VTSModelOutlineEventConfigOptions(bool draw) {
			this.draw = draw;
		}

		public bool draw = false;
	}

	[System.Serializable]
	public class VTSModelOutlineEventData : VTSEventData {
		public VTSModelOutlineEventData() {
			this.messageType = "ModelOutlineEvent";
			this.data = new Data();
		}
		public Data data;

		[System.Serializable]
		public class Data {
			public string modelID;
			public string modelName;
			public Pair[] convexHull;
			public Pair convexHullCenter;
			public Pair windowSize;
		}
	}

	#endregion
}
