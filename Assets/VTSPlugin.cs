using System;
using UnityEngine;
using VTS.Networking;

namespace VTS {
    [RequireComponent(typeof(VTSWebSocket))]
    public abstract class VTSPlugin : MonoBehaviour
    {
        [SerializeField]
        protected string _pluginName = "ExamplePlugin";
        public string PluginName { get { return this._pluginName; } }
        [SerializeField]
        protected string _pluginAuthor = "ExampleAuthor";
        public string PluginAuthor { get { return this._pluginAuthor; } }

        private VTSWebSocket _socket = null;
        protected VTSWebSocket Socket { get { return _socket; } }

        private string _token = null;
        protected string AuthenticationToken { get { return _token; }}

        private void Awake(){
            this._socket = GetComponent<VTSWebSocket>();
            Setup();
            // TODO: clean this way the hell up.
            this._socket.Connect(() => {
                Authenticate((r) => { 
                Debug.Log(r); 
                GetCurrentModel((m) => {
                    Debug.Log(m);
                },
                (m) => {
                    Debug.LogError(m);
                });
                }, (r) => { Debug.LogError(r); });
            GetAPIState((r) => { Debug.Log(r); }, (r) => { Debug.LogError(r); });
            },
            () => { 
                Debug.LogError("Unable to connect ");
            });
        }

        protected abstract void Setup();

        private void Authenticate(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError){
            VTSAuthData tokenRequest = new VTSAuthData();
            tokenRequest.data.pluginName = this._pluginName;
            tokenRequest.data.pluginDeveloper = this._pluginAuthor;
            this._socket.Send<VTSAuthData>(tokenRequest, (a) => { 
                this._token = a.data.authenticationToken; 
                VTSAuthData authRequest = new VTSAuthData();
                authRequest.messageType = "AuthenticationRequest";
                authRequest.data.pluginName = this._pluginName;
                authRequest.data.pluginDeveloper = this._pluginAuthor;
                authRequest.data.authenticationToken = this._token;
                this._socket.Send<VTSAuthData>(authRequest, onSuccess, onError);
            }, onError);
        }

        public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError){
            VTSStateData request = new VTSStateData();
            this._socket.Send<VTSStateData>(request, onSuccess, onError);
        }

        public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError){
            VTSStatisticsData request = new VTSStatisticsData();
            this._socket.Send<VTSStatisticsData>(request, onSuccess, onError);
        }

        public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError){
            VTSFolderInfoData request = new VTSFolderInfoData();
            this._socket.Send<VTSFolderInfoData>(request, onSuccess, onError);
        }

        public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError){
            VTSCurrentModelData request = new VTSCurrentModelData();
            Debug.Log(request);
            this._socket.Send<VTSCurrentModelData>(request, onSuccess, onError);
        }

        public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError){
            VTSAvailableModelsData request = new VTSAvailableModelsData();
            this._socket.Send<VTSAvailableModelsData>(request, onSuccess, onError);
        }

        public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError){
            VTSModelLoadData request = new VTSModelLoadData();
            request.data.modelID = modelID;
            this._socket.Send<VTSModelLoadData>(request, onSuccess, onError);
        }

        public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError){
            VTSMoveModelData request = new VTSMoveModelData();
            request.data = position;
            this._socket.Send<VTSMoveModelData>(request, onSuccess, onError);
        }

    }
}
