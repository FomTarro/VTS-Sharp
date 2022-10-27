using System;
using System.Collections.Generic;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

using UnityEngine;
using VTS.Models;

namespace VTS.Networking {
    /// <summary>
    /// Underlying VTS socket connection and response processor.
    /// </summary>
    public class VTSWebSocket : MonoBehaviour
    {
        // Dependencies
        private const string VTS_WS_URL = "ws://{0}:{1}";
        private IPAddress _ip = IPAddress.Loopback;
        private int _port = 8001;
        private IWebSocket _ws = null;
        private IJsonUtility _json = null;

        // API Callbacks
        private Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();
        private Dictionary<string, VTSEventCallbacks> _events = new Dictionary<string, VTSEventCallbacks>();

        // UDP 
        private static UdpClient UDP_CLIENT = null;
        private static Task<UdpReceiveResult> UDP_RESULT = null;
        private static readonly Dictionary<int, VTSStateBroadcastData> PORTS = new Dictionary<int, VTSStateBroadcastData>();

        #region Lifecycle

        public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility){
            if(this._ws != null){
                this._ws.Stop();
            }
            this._ws = webSocket;
            this._json = jsonUtility;
            StartUDP();
        }

        private void OnDestroy(){
            this._ws.Stop();
        }

        private void FixedUpdate(){
            ProcessResponses();
            CheckPorts();
        }

        #endregion

        #region UDP

        private void CheckPorts(){
            StartUDP();
            if(UDP_CLIENT != null && this._json != null){
                if(UDP_RESULT != null){
                    if(UDP_RESULT.IsCanceled || UDP_RESULT.IsFaulted){
                        // If the task faults, try again
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                    }else if(UDP_RESULT.IsCompleted){
                        // Otherwise, collect the result
                        string text = Encoding.UTF8.GetString(UDP_RESULT.Result.Buffer);
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                        VTSStateBroadcastData data = this._json.FromJson<VTSStateBroadcastData>(text);
                        if(PORTS.ContainsKey(data.data.port)){
                            PORTS.Remove(data.data.port);
                        }
                        PORTS.Add(data.data.port, data);
                    }
                }
                
                if(UDP_RESULT == null){
                    UDP_RESULT = UDP_CLIENT.ReceiveAsync();
                }
            }
        }

        private void StartUDP(){
            try{
                if(UDP_CLIENT == null){
                    // This configuration should prevent the UDP client from blocking other connections to the port
                    IPEndPoint LOCAL_PT = new IPEndPoint(IPAddress.Any, 47779);
                    UDP_CLIENT = new UdpClient();
                    UDP_CLIENT.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UDP_CLIENT.Client.Bind(LOCAL_PT);
                }
            }catch(Exception e){
                Debug.LogError(e);
            }
        }
        
        public Dictionary<int, VTSStateBroadcastData> GetPorts(){
            return new Dictionary<int, VTSStateBroadcastData>(PORTS);
        }

        public bool SetPort(int port){
            if(PORTS.ContainsKey(port)){
                this._port = port;
                return true;
            }
            return false;
        }

        public bool SetIPAddress(string ipString){
            IPAddress address;
            if(IPAddress.TryParse(ipString, out address)){
                this._ip = address;
                return true;
            }
            return false;
        }

        #endregion

        #region I/O

        private void ProcessResponses(){
            string data = null;
            do{
                if(this._ws != null){
                    data = this._ws.GetNextResponse();
                    if(data != null){
                        VTSMessageData response = this._json.FromJson<VTSMessageData>(data);
                        if(this._events.ContainsKey(response.messageType)){
                            try{
                                switch(response.messageType){
                                    case "TestEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSTestEventData>(data));
                                        break;
                                    case "ModelLoadedEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSModelLoadedEventData>(data));
                                        break;
                                    case "TrackingStatusChangedEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSTrackingEventData>(data));
                                        break;
                                    case "BackgroundChangedEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSBackgroundChangedEventData>(data));
                                        break;
                                    case "ModelConfigChangedEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSModelConfigChangedEventData>(data));
                                        break;
                                    case "ModelMovedEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSModelMovedEventData>(data));
                                        break;
                                    case "ModelOutlineEvent":
                                        this._events[response.messageType].onEvent(this._json.FromJson<VTSModelOutlineEventData>(data));
                                        break;
                                }
                            }catch(Exception e){
                                // Neatly handle errors in case the deserialization or success callback throw an exception
                                VTSErrorData error = new VTSErrorData();
                                error.requestID = response.requestID;
                                error.data.message = e.Message;
                                this._events[response.messageType].onError(error);
                            }
                        }
                        else if(this._callbacks.ContainsKey(response.requestID)){
                            try{
                                switch(response.messageType){
                                    case "APIError":
                                        this._callbacks[response.requestID].onError(this._json.FromJson<VTSErrorData>(data));
                                        break;
                                    case "APIStateResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSStateData>(data));
                                        break;
                                    case "StatisticsResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSStatisticsData>(data));
                                        break;
                                    case "AuthenticationResponse":
                                    case "AuthenticationTokenResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSAuthData>(data));
                                        break;
                                    case "VTSFolderInfoResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSFolderInfoData>(data));
                                        break;
                                    case "CurrentModelResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSCurrentModelData>(data));
                                        break;
                                    case "AvailableModelsResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSAvailableModelsData>(data));
                                        break;
                                    case "ModelLoadResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSModelLoadData>(data));
                                        break;
                                    case "MoveModelResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSMoveModelData>(data));
                                        break;
                                    case "HotkeysInCurrentModelResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSHotkeysInCurrentModelData>(data));
                                        break;
                                    case "HotkeyTriggerResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSHotkeyTriggerData>(data));
                                        break;
                                    case "ArtMeshListResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSArtMeshListData>(data));
                                        break;
                                    case "ColorTintResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSColorTintData>(data));
                                        break;
                                    case "SceneColorOverlayInfoResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSSceneColorOverlayData>(data));
                                        break;
                                    case "FaceFoundResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSFaceFoundData>(data));
                                        break;
                                    case "InputParameterListResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSInputParameterListData>(data));
                                        break;
                                    case "ParameterValueResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSParameterValueData>(data));
                                        break;
                                    case "Live2DParameterListResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSLive2DParameterListData>(data));
                                        break;
                                    case "ParameterCreationResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSParameterCreationData>(data));
                                        break;
                                    case "ParameterDeletionResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSParameterDeletionData>(data));
                                        break;
                                    case "InjectParameterDataResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSInjectParameterData>(data));
                                        break;
                                    case "ExpressionStateResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSExpressionStateData>(data));
                                        break;
                                    case "ExpressionActivationResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSExpressionActivationData>(data));
                                        break;
                                    case "GetCurrentModelPhysicsResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSCurrentModelPhysicsData>(data));
                                        break;
                                    case "SetCurrentModelPhysicsResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSOverrideModelPhysicsData>(data));
                                        break;
                                    case "NDIConfigResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSNDIConfigData>(data));
                                        break;
                                    case "ItemListResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSItemListResponseData>(data));
                                        break;
                                    case "ItemLoadResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSItemLoadResponseData>(data));
                                        break;
                                    case "ItemUnloadResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSItemUnloadResponseData>(data));
                                        break;
                                    case "ItemAnimationControlResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSItemAnimationControlResponseData>(data));
                                        break;
                                    case "ItemMoveResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSItemMoveResponseData>(data));
                                        break;
                                    case "ArtMeshSelectionResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSArtMeshSelectionResponseData>(data));
                                        break;
                                    case "EventSubscriptionResponse":
                                        this._callbacks[response.requestID].onSuccess(this._json.FromJson<VTSEventSubscriptionResponseData>(data));
                                        break;
                                    default:
                                        VTSErrorData error = new VTSErrorData();
                                        error.data.message = "Unable to parse response as valid response type: " + data;
                                        this._callbacks[response.requestID].onError(error);
                                        break;

                                }
                            }catch(Exception e){
                                // Neatly handle errors in case the deserialization or success callback throw an exception
                                VTSErrorData error = new VTSErrorData();
                                error.requestID = response.requestID;
                                error.data.message = e.Message;
                                this._callbacks[response.requestID].onError(error);
                            }
                            this._callbacks.Remove(response.requestID);
                        }
                    }
                }
            }while(data != null);
        }

        public void Connect(System.Action onConnect, System.Action onDisconnect, System.Action onError){
            if(this._ws != null){
                this._ws.Start(string.Format(VTS_WS_URL, this._ip.ToString(), this._port), onConnect, onDisconnect, onError);
            }else{
                onError();
            }
        }

        public void Disconnect(){
            if(this._ws != null){
                this._ws.Stop();
            }
        }

        public void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError) where T : VTSMessageData where K : VTSMessageData{
            if(this._ws != null){
                try{
                    this._callbacks.Add(request.requestID, new VTSCallbacks((k) => { onSuccess((K)k); }, onError));
                    // make sure to remove null properties
                    string output = this._json.ToJson(request);
                    this._ws.Send(output);
                }catch(Exception e){
                    Debug.LogError(e);
                    VTSErrorData error = new VTSErrorData();
                    error.data.errorID = ErrorID.InternalServerError;
                    error.data.message = e.Message;
                    onError(error);
                }
            }else{
                VTSErrorData error = new VTSErrorData();
                error.data.errorID = ErrorID.InternalServerError;
                error.data.message = "No websocket data";
                onError(error);
            }
        }

        public void SendEventSubscription<T, K>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe) where T : VTSEventSubscriptionRequestData where K : VTSEventData{
            this.Send<T, VTSEventSubscriptionResponseData>(
                request, 
                (s) => {
                    // add event or remove event from register
                    if(this._events.ContainsKey(request.GetEventName())){
                        this._events.Remove(request.GetEventName());
                    }
                    if(request.GetSubscribed()){
                        this._events.Add(request.GetEventName(), new VTSEventCallbacks((k) => { onEvent((K)k); }, onError, resubscribe));
                    }
                    onSubscribe(s);
                },
                onError);
        }

        public void ResubscribeToEvents(){
            foreach(VTSEventCallbacks callback in this._events.Values){
                callback.resubscribe();
            }
        }

        #endregion

        private struct VTSCallbacks {
            public Action<VTSMessageData> onSuccess; 
            public Action<VTSErrorData> onError;
            public VTSCallbacks(Action<VTSMessageData> onSuccess, Action<VTSErrorData> onError){
                this.onSuccess = onSuccess;
                this.onError = onError;
            }
        }

        private struct VTSEventCallbacks {
            public Action<VTSEventData> onEvent;
            public Action<VTSErrorData> onError;
            public Action resubscribe;
            public VTSEventCallbacks(Action<VTSEventData> onEvent, Action<VTSErrorData> onError, Action resubscribe){
                this.onEvent = onEvent;
                this.onError = onError;
                this.resubscribe = resubscribe;
            }
        }
    }
}
