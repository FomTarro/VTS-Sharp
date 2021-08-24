using System;
using System.Collections.Generic;
using UnityEngine;
using VTS.Networking.Impl;

namespace VTS.Networking{
    public class VTSWebSocket : MonoBehaviour
    {
        private string VTS_WS_URL = "ws://localhost:8001";
        private UnityWebSocket _ws = null;
        private Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();

        private void Update(){
            if(_ws != null && _ws.RecieveQueue.Count > 0){
                string data;
                _ws.RecieveQueue.TryDequeue(out data);
                if(data != null){
                    VTSMessageData response = JsonUtility.FromJson<VTSMessageData>(data);
                    if(_callbacks.ContainsKey(response.requestID)){
                        switch(response.messageType){
                            case "APIError":
                                _callbacks[response.requestID].onError(JsonUtility.FromJson<VTSErrorData>(data));
                                break;
                            case "APIStateResponse":
                                _callbacks[response.requestID].onSuccess(JsonUtility.FromJson<VTSStateData>(data));
                                break;
                            case "AuthenticationResponse":
                            case "AuthenticationTokenResponse":
                                _callbacks[response.requestID].onSuccess(JsonUtility.FromJson<VTSAuthData>(data));
                                break;
                            case "VTSFolderInfoResponse":
                                _callbacks[response.requestID].onSuccess(JsonUtility.FromJson<VTSFolderInfoData>(data));
                                break;
                            case "CurrentModelResponse":
                                _callbacks[response.requestID].onSuccess(JsonUtility.FromJson<VTSCurrentModelData>(data));
                                break;
                            case "AvailableModelsResponse":
                                _callbacks[response.requestID].onSuccess(JsonUtility.FromJson<VTSAvailableModelsData>(data));
                                break;
                        }
                    }
                }
            }
        }

        public void Connect(System.Action onConnect, System.Action onError){
            this._ws = new UnityWebSocket(VTS_WS_URL);
            #pragma warning disable 
            this._ws.Connect(onConnect, onError);
            #pragma warning restore
        }

        public void Send<T>(T request, Action<T> onSuccess, Action<VTSErrorData> onError) where T : VTSMessageData{
            if(this._ws != null){
                _callbacks.Add(request.requestID, new VTSCallbacks((t) => { onSuccess((T)t); } , onError));
                string output = RemoveNullProps(request.ToString());
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
                }
            }
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
