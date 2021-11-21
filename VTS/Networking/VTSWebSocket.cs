using System;
using System.Collections.Generic;
using UnityEngine;
using VTS.Models;

namespace VTS.Networking {
    public class VTSWebSocket : MonoBehaviour
    {
        private const string VTS_WS_URL = "ws://localhost:8001";
        private IWebSocket _ws = null;
        private IJsonUtility _json = null;
        private Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();

        public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility){
            this._ws = webSocket;
            this._json = jsonUtility;
        }

        private void OnDestroy(){
            this._ws.Stop();
        }

        private void FixedUpdate(){
            ProcessResponses();
        }

        private void ProcessResponses(){
            string data = this._ws.GetNextResponse();
            if(this._ws != null && data != null){
                VTSMessageData response = _json.FromJson<VTSMessageData>(data);
                if(this._callbacks.ContainsKey(response.requestID)){
                    switch(response.messageType){
                        case "APIError":
                            this._callbacks[response.requestID].onError(_json.FromJson<VTSErrorData>(data));
                            break;
                        case "APIStateResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSStateData>(data));
                            break;
                        case "StatisticsResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSStatisticsData>(data));
                            break;
                        case "AuthenticationResponse":
                        case "AuthenticationTokenResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSAuthData>(data));
                            break;
                        case "VTSFolderInfoResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSFolderInfoData>(data));
                            break;
                        case "CurrentModelResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSCurrentModelData>(data));
                            break;
                        case "AvailableModelsResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSAvailableModelsData>(data));
                            break;
                        case "ModelLoadResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSModelLoadData>(data));
                            break;
                        case "MoveModelResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSMoveModelData>(data));
                            break;
                        case "HotkeysInCurrentModelResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSHotkeysInCurrentModelData>(data));
                            break;
                        case "HotkeyTriggerResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSHotkeyTriggerData>(data));
                            break;
                        case "ArtMeshListResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSArtMeshListData>(data));
                            break;
                        case "ColorTintResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSColorTintData>(data));
                            break;
                        case "SceneColorOverlayInfoResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSSceneColorOverlayData>(data));
                            break;
                        case "FaceFoundResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSFaceFoundData>(data));
                            break;
                        case "InputParameterListResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSInputParameterListData>(data));
                            break;
                        case "ParameterValueResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSParameterValueData>(data));
                            break;
                        case "Live2DParameterListResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSLive2DParameterListData>(data));
                            break;
                        case "ParameterCreationResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSParameterCreationData>(data));
                            break;
                        case "ParameterDeletionResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSParameterDeletionData>(data));
                            break;
                        case "InjectParameterDataResponse":
                            this._callbacks[response.requestID].onSuccess(_json.FromJson<VTSInjectParameterData>(data));
                            break;
                        default:
                            VTSErrorData error = new VTSErrorData();
                            error.data.message = "Unable to parse response as valid response type: " + data;
                            this._callbacks[response.requestID].onError(error);
                            break;

                    }
                    this._callbacks.Remove(response.requestID);
                }
            }
        }

        public void Connect(System.Action onConnect, System.Action onDisconnect, System.Action onError){
            if(this._ws != null){
                #pragma warning disable 
                this._ws.Start(VTS_WS_URL, onConnect, onDisconnect, onError);
                #pragma warning restore
            }else{
                onError();
            }
        }

        public void Send<T>(T request, Action<T> onSuccess, Action<VTSErrorData> onError) where T : VTSMessageData{
            if(this._ws != null){
                try{
                    _callbacks.Add(request.requestID, new VTSCallbacks((t) => { onSuccess((T)t); } , onError));
                    // make sure to remove null properties
                    string output = _json.ToJson(request);
                    this._ws.Send(output);
                }catch(Exception e){
                    Debug.Log(e);
                }
            }else{
                VTSErrorData error = new VTSErrorData();
                error.data.errorID = ErrorID.InternalServerError;
                error.data.message = "No websocket data";
            }
        }

        private struct VTSCallbacks{
            public Action<VTSMessageData> onSuccess; 
            public Action<VTSErrorData> onError;
            public VTSCallbacks(Action<VTSMessageData> onSuccess, Action<VTSErrorData> onError){
                this.onSuccess = onSuccess;
                this.onError = onError;
            }
        }
    }
}
