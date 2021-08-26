using System;
using System.Collections.Generic;
using UnityEngine;
using VTS.Models;

namespace VTS.Networking {
    public class VTSWebSocket : MonoBehaviour
    {
        private string VTS_WS_URL = "ws://localhost:8001";
        private IWebSocket _ws = null;
        private IJsonUtility _json = null;
        private Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();

        private void Update(){
            if(_ws != null && _ws.RecieveQueue.Count > 0){
                string data;
                _ws.RecieveQueue.TryDequeue(out data);
                if(data != null){
                    Debug.Log("RECIEVE " + data);
                    VTSMessageData response = _json.FromJson<VTSMessageData>(data);
                    if(_callbacks.ContainsKey(response.requestID)){
                        switch(response.messageType){
                            case "APIError":
                            Debug.Log("Error!");
                                _callbacks[response.requestID].onError(_json.FromJson<VTSErrorData>(data));
                                break;
                            case "APIStateResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSStateData>(data));
                                break;
                            case "AuthenticationResponse":
                            case "AuthenticationTokenResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSAuthData>(data));
                                break;
                            case "VTSFolderInfoResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSFolderInfoData>(data));
                                break;
                            case "CurrentModelResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSCurrentModelData>(data));
                                break;
                            case "AvailableModelsResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSAvailableModelsData>(data));
                                break;
                            case "ModelLoadResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSModelLoadData>(data));
                                break;
                            case "MoveModelResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSMoveModelData>(data));
                                break;
                            case "HotkeysInCurrentModelResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSHotkeysInCurrentModelData>(data));
                                break;
                            case "HotkeyTriggerResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSHotkeysInCurrentModelData>(data));
                                break;
                            case "ArtMeshListResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSArtMeshListData>(data));
                                break;
                            case "ColorTintResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSColorTintData>(data));
                                break;
                            case "FaceFoundResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSFaceFoundData>(data));
                                break;
                            case "InputParameterListResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSInputParameterListData>(data));
                                break;
                            case "Live2DParameterListResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSLive2DParameterListData>(data));
                                break;
                            case "ParameterCreationResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSParameterCreationData>(data));
                                break;
                            case "ParameterDeletionResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSParameterDeletionData>(data));
                                break;
                            case "InjectParameterDataResponse":
                                _callbacks[response.requestID].onSuccess(_json.FromJson<VTSInjectParameterData>(data));
                                break;
                        }
                    }
                }
            }
        }

        public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility){
            this._ws = webSocket;
            this._json = jsonUtility;
        }

        public void Connect(System.Action onConnect, System.Action onError){
            if(this._ws != null){
                #pragma warning disable 
                this._ws.Connect(VTS_WS_URL, onConnect, onError);
                #pragma warning restore
            }else{
                onError();
            }
        }

        public void Send<T>(T request, Action<T> onSuccess, Action<VTSErrorData> onError) where T : VTSMessageData{
            if(this._ws != null){
                _callbacks.Add(request.requestID, new VTSCallbacks((t) => { onSuccess((T)t); } , onError));
                string output = RemoveNullProps(_json.ToJson(request));
                Debug.Log("Sending" + output);
                this._ws.Send(output);
            }
        }

        private string RemoveNullProps(string input){
            string[] props = input.Split(',', '{', '}');
            string output = input;
            foreach(string prop in props){
                // We're doing direct string manipulation on a serialized JSON, which is incredibly frail.
                // Please forgive my sins, as Unity's builtin JSON tool uses default field values instead of nulls,
                // and sometimes that is unacceptable behavior.
                // I'd use a more robust JSON library if I wasn't publishing this as a plugin.
                string[] pair = prop.Split(':');
                if(pair.Length > 1){
                    float nullable = 0.0f;
                    float.TryParse(pair[1], out nullable);
                    if(float.MinValue.Equals(nullable)){
                        output = output.Replace(prop+",", "");
                        output = output.Replace(prop, "");
                    }
                    else if("\"\"".Equals(pair[1])){
                        output = output.Replace(prop+",", "");
                        output = output.Replace(prop, "");
                    }
                }
            }
            output = output.Replace(",}", "}");
            return output;
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
