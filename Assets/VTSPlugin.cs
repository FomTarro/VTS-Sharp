using System;
using System.Text.RegularExpressions;
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

        public void GetHotkeysInCurrentModel(Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError){
            VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
            this._socket.Send<VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
        }
        
        public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError){
            VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
            request.data.hotkeyID = hotkeyID;
            this._socket.Send<VTSHotkeyTriggerData>(request, onSuccess, onError);
        }

        public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError){
            VTSArtMeshListData request = new VTSArtMeshListData();
            this._socket.Send<VTSArtMeshListData>(request, onSuccess, onError);
        }

        public void TintArtMesh(ColorTint tint, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError){
            VTSColorTintData request = new VTSColorTintData();
            request.data.colorTint = tint;
            request.data.artMeshMatcher = matcher;
            this._socket.Send<VTSColorTintData>(request, onSuccess, onError);
        }

        public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError){
            VTSFaceFoundData request = new VTSFaceFoundData();
            this._socket.Send<VTSFaceFoundData>(request, onSuccess, onError);
        }

        public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError){
            VTSInputParameterListData request = new VTSInputParameterListData();
            this._socket.Send<VTSInputParameterListData>(request, onSuccess, onError);
        }

        public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterValueData request = new VTSParameterValueData();
            request.data.name = parameterName;
            this._socket.Send<VTSParameterValueData>(request, onSuccess, onError);
        }

        public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError){
            VTSLive2DParameterListData request = new VTSLive2DParameterListData();
            this._socket.Send<VTSLive2DParameterListData>(request, onSuccess, onError);
        }

        public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterCreationData request = new VTSParameterCreationData();
            request.data.parameterName = SanatizeParameterName(parameter.parameterName);
            request.data.explanation = parameter.explanation;
            request.data.min = parameter.min;
            request.data.max = parameter.max;
            request.data.defaultValue = parameter.defaultValue;
            this._socket.Send<VTSParameterCreationData>(request, onSuccess, onError);
        }

        public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterDeletionData request = new VTSParameterDeletionData();
            request.data.parameterName = SanatizeParameterName(parameterName);
            this._socket.Send<VTSParameterDeletionData>(request, onSuccess, onError);
        }

        public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError){
            VTSInjectParameterData request = new VTSInjectParameterData();
            foreach(VTSParameterInjectionValue value in values){
                value.id = SanatizeParameterName(value.id);
            }
            request.data.parameterValues = values;
            this._socket.Send<VTSInjectParameterData>(request, onSuccess, onError);
        }

        private Regex ALPHANUMERIC = new Regex(@"\W|_");
        private string SanatizeParameterName(string name){
            // between 4 and 32 chars, alphanumeric
            string output = name;
            output = ALPHANUMERIC.Replace(output, "");
            output.PadLeft(4, 'X');
            output = output.Substring(0, Math.Min(output.Length, 31));
            return output;
        }
        
    }
}
