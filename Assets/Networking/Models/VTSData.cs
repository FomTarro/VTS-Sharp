namespace VTS.Networking {
    // This is a boundary model, TODO: make a domain model
    [System.Serializable]
    public class VTSData
    {
        public string apiName = "VTubeStudioPublicAPI";
        public string apiVersion = "1.0";
        public string requestID;
        public string messageType;
        public Data data = new Data();

        public override string ToString()
        {
            return UnityEngine.JsonUtility.ToJson(this);
        }
        
        [System.Serializable]
        public class Data {
            public string message;
            public string pluginName;
            public string pluginDeveloper;
            public string pluginIcon;
            public string authenticationToken;
            public bool active;
		    public string vTubeStudioVersion;
		    public bool currentSessionAuthenticated;
        }
    }
}

