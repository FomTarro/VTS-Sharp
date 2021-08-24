using System;
namespace VTS.Networking {
    public class VTSMessageData
    {
        public string apiName = "VTubeStudioPublicAPI";
        public string apiVersion = "1.0";
        public string requestID = Guid.NewGuid().ToString();
        public string messageType;

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this);
        }
    }

    
    [System.Serializable]
    public class VTSErrorData : VTSMessageData{
         public VTSErrorData(){
            this.messageType = "APIError";
            this.data = new Data();
        }
        public Data data;

        [System.Serializable]
        public class Data {
            public int errorID;
            public string message;
        }
    }

    [System.Serializable]
    public class VTSStateData : VTSMessageData{
        public VTSStateData(){
            this.messageType = "APIStateRequest";
            this.data = new Data();
        }
        public Data data;

        [System.Serializable]
        public class Data{
            public bool active;
            public string vTubeStudioVersion;
            public bool currentSessionAuthenticated;
        }
    }

    [System.Serializable]
    public class VTSAuthData : VTSMessageData{
        public VTSAuthData(){
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
        }
    }

    [System.Serializable]
    public class VTSStatisticsData : VTSMessageData{
         public VTSStatisticsData(){
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
    public class VTSFolderInfoData : VTSMessageData{
         public VTSFolderInfoData(){
            this.messageType = "VTSFolderInfoRequestuest";
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
    public class VTSModelData {
        public bool modelLoaded;
        public string modelName;
        public string modelID;
        public string vtsModelName;
        public string vtsModelIconName;
    }

    [System.Serializable]
    public class ModelPosition{
        public float positionX = float.MinValue;
        public float positionY = float.MinValue;
        public float rotation = float.MinValue;
        public float size = float.MinValue;

    }

    [System.Serializable]
    public class VTSCurrentModelData : VTSMessageData{
         public VTSCurrentModelData(){
            this.messageType = "CurrentModelRequest";
            this.data = new Data();
        }
        public Data data;

        [System.Serializable]
        public class Data : VTSModelData{
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
    public class VTSAvailableModelsData : VTSMessageData{
         public VTSAvailableModelsData(){
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
    public class VTSModelLoadData : VTSMessageData{
        public VTSModelLoadData(){
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
    public class VTSMoveModelData : VTSMessageData{
        public VTSMoveModelData(){
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

    public class HotkeyData {
        public string name;
		public string type;
		public string file;
		public string hotkeyID;
    }

    [System.Serializable]
    public class VTSHotkeysInCurrentModelData : VTSMessageData{
        public VTSHotkeysInCurrentModelData(){
            this.messageType = "HotkeysInCurrentModelResponse";
            this.data = new Data();
        }
        public Data data;

        [System.Serializable]
        public class Data {
            public bool modelLoaded;
            public string modelName;
            public string modelID;
            public HotkeyData[] availableHotkeys;
        }
    }

    [System.Serializable]
    public class VTSHotkeyTriggerData : VTSMessageData{
        public VTSHotkeyTriggerData(){
            this.messageType = "HotkeyTriggerResponse";
            this.data = new Data();
        }
        public Data data;

        [System.Serializable]
        public class Data {
            public string hotkeyID;
        }
    }
}

