using System;
using System.Collections.Generic;

using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

using UnityEngine;
using VTS.Models;

namespace VTS.Networking {
    /// <summary>
    /// Underlying VTS socket connection and response processor.
    /// </summary>
    public class VTSWebSocket : MonoBehaviour {
        // Dependencies
        private const string VTS_WS_URL = "ws://{0}:{1}";
        private IPAddress _ip = IPAddress.Loopback;
        private const int DEFAULT_PORT = 8001;
        private int _port = DEFAULT_PORT;
        public int Port { get { return this._port; } }
        private IWebSocket _ws = null;
        private IJsonUtility _json = null;

        // API Callbacks
        private Dictionary<string, VTSCallbacks> _callbacks = new Dictionary<string, VTSCallbacks>();
        private Dictionary<string, VTSEventCallbacks> _events = new Dictionary<string, VTSEventCallbacks>();

        // UDP 
        private const int UDP_DEFAULT_PORT = 47779;
        private static UdpClient UDP_CLIENT = null;
        private static Task<UdpReceiveResult> UDP_RESULT = null;
        private static readonly Dictionary<IPAddress, Dictionary<int, VTSStateBroadcastData>> PORTS_BY_IP = new Dictionary<IPAddress, Dictionary<int, VTSStateBroadcastData>>();
        // private static readonly Dictionary<int, VTSStateBroadcastData> PORTS = new Dictionary<int, VTSStateBroadcastData>();

        private static event Action<IPAddress, int> GLOBAL_PORT_DISCOVERY_EVENT;

        private Action<int> _onLocalPortDiscovered = null;
        private const float DEFAULT_PORT_DISCOVERY_TIMEOUT = 3f;
        private float _portDiscoveryTimer = 0;
        private Action _onPortDiscoveryTimeout = null;

        #region Lifecycle

        public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility){
            if(this._ws == null){
                // Only add this listener to the event the first time we initialize.
                GLOBAL_PORT_DISCOVERY_EVENT += OnPortDiscovered;
            }
            // Stop existing socket.
            Disconnect();

            this._ws = webSocket;
            this._json = jsonUtility;
            StartUDP();
        }

        private void OnDestroy(){
            GLOBAL_PORT_DISCOVERY_EVENT -= OnPortDiscovered;
            Disconnect();
        }

        private void Update(){
            Update(Time.deltaTime);
        }

        private void Update(float timeDelta){
            ProcessResponses();
            CheckPorts();
            UpdatePortDiscoveryTimeout(timeDelta);
        }

        #endregion

        #region UDP

        private void CheckPorts(){
            StartUDP();
            // Port Discovery is global, so any plugin instance with capacity can pick up this task.
            // If another plugin isn't already servicing this task...
            if(UDP_CLIENT != null && this._json != null){
                // If we have a result...
                if(UDP_RESULT != null){
                    if(UDP_RESULT.IsCanceled || UDP_RESULT.IsFaulted){
                        // If the task faults, try again
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                    }else if(UDP_RESULT.IsCompleted){
                        // Otherwise, collect the result
                        string text = Encoding.UTF8.GetString(UDP_RESULT.Result.Buffer);
                        IPAddress address = MapAddress(UDP_RESULT.Result.RemoteEndPoint.Address);
                        UDP_RESULT.Dispose();
                        UDP_RESULT = null;
                        VTSStateBroadcastData data = this._json.FromJson<VTSStateBroadcastData>(text);
                        // Debug.Log(string.Format("{0} - {1}", address, text));

                        // New IP addresses get new records made
                        if(!PORTS_BY_IP.ContainsKey(address)){
                            PORTS_BY_IP.Add(address, new Dictionary<int, VTSStateBroadcastData>());
                        }
                        // If this IP already knows about this port...
                        if(PORTS_BY_IP[address].ContainsKey(data.data.port)){
                            if(data.data.active){
                                // Update record if active
                                PORTS_BY_IP[address][data.data.port] = data;
                            }else{
                                // Remove record if inactive
                                PORTS_BY_IP[address].Remove(data.data.port);
                            }
                        }else{
                            if(data.data.active){
                                PORTS_BY_IP[address].Add(data.data.port, data);
                            }
                        }
                        
                        if(data.data.active){
                            GLOBAL_PORT_DISCOVERY_EVENT.Invoke(address, data.data.port);
                        }
                    }
                }
                // If our result has been collected and disposed of, start again
                if(UDP_RESULT == null){
                    UDP_RESULT = UDP_CLIENT.ReceiveAsync();
                }
            }
        }

        private static void StartUDP(){
            try{
                if(UDP_CLIENT == null){
                    // This configuration should prevent the UDP client from blocking other connections to the port
                    IPEndPoint LOCAL_PT = new IPEndPoint(IPAddress.Any, UDP_DEFAULT_PORT);
                    UDP_CLIENT = new UdpClient();
                    UDP_CLIENT.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    UDP_CLIENT.Client.Bind(LOCAL_PT);
                }
            }catch(Exception e){
                Debug.LogError(e);
            }
        }

        private void OnPortDiscovered(IPAddress address, int port){
            if(MapAddress(address).Equals(MapAddress(this._ip))){
                if(this._onLocalPortDiscovered != null){
                    Debug.Log(string.Format("Available VTube Studio port discovered: {0}!", port));
                    this._onLocalPortDiscovered.Invoke(port);
                }
            }
        }

        private void UpdatePortDiscoveryTimeout(float timeDelta){
            if(this._portDiscoveryTimer > 0f){
                this._portDiscoveryTimer -= timeDelta;
                if(this._portDiscoveryTimer <= 0f){
                    if(this._onPortDiscoveryTimeout != null){
                        this._onPortDiscoveryTimeout.Invoke();
                    }
                    this._portDiscoveryTimer = 0f;
                }
            }
        }
        
        /// <summary>
        /// Returns a map of ports available to the current IP Address. Indexed by port number.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, VTSStateBroadcastData> GetPorts(){
            return PORTS_BY_IP.ContainsKey(this._ip) 
                ? new Dictionary<int, VTSStateBroadcastData>(PORTS_BY_IP[this._ip]) 
                : new Dictionary<int, VTSStateBroadcastData>();
        }

        /// <summary>
        /// Sets the connection port to the given number. Returns true if the port is a valid VTube Studio port, returns false otherwise. 
        /// If the port number is changed while an active connection exists, you will need to reconnect.
        /// </summary>
        /// <param name="port">The port to connect to.</param>
        /// <returns>True if the port is a valid VTube Studio port, False otherwise.</returns>
        public bool SetPort(int port){
            Debug.Log(string.Format("Setting port: {0}...", port));
            this._port = port;
            if(PORTS_BY_IP.ContainsKey(this._ip) && PORTS_BY_IP[this._ip].ContainsKey(port)){
                Debug.Log(string.Format("Port {0} is a known VTube Studio Port.", port));
                return true;
            }
            Debug.LogWarning(string.Format("Port {0} is not a known VTube Studio Port!", port));
            return false;
        }

        /// <summary>
        /// Sets the connection IP address to the given string. Returns true if the string is a valid IP Address format, returns false otherwise.
        /// If the IP Address is changed while an active connection exists, you will need to reconnect.
        /// </summary>
        /// <param name="ipString">The string form of the IP address, in dotted-quad notation for IPv4.</param>
        /// <returns>True if the string is a valid IP Address format, False otherwise.</returns>
        public bool SetIPAddress(string ipString){
            IPAddress address;
            Debug.Log(string.Format("Setting IP address: {0}...", ipString));
            if(IPAddress.TryParse(ipString, out address)){
                this._ip = MapAddress(address);
                Debug.Log(string.Format("IP address {0} is valid IPv4 format.", ipString));
                return true;
            }
            Debug.LogWarning(string.Format("IP address {0} is not valid IPv4 format! Unable to set.", ipString));
            return false;
        }

        /// <summary>
        /// Maps all forms of loopback IP to 127.0.0.1. Non-loopback addresses are returned as-is.
        /// </summary>
        /// <returns>The mapped IP Address.</returns>
        private static IPAddress MapAddress(IPAddress address){
            foreach(NetworkInterface ip in NetworkInterface.GetAllNetworkInterfaces()){
                foreach(UnicastIPAddressInformation unicast in ip.GetIPProperties().UnicastAddresses){
                    if(address.Equals(unicast.Address) && unicast.Address.AddressFamily == AddressFamily.InterNetwork){
                        // If the provided address is one of the local machine's addresses,
                        // Then it is effectively loopback, so let's always use loopback for simplicity.
                        return IPAddress.Loopback;
                    }
                }
            }
            return address;
        }

        #endregion

        #region I/O

        /// <summary>
        /// Connects to VTube Studio on the current port, executing the provided callbacks during different phases of the connection lifecycle.
        /// Will first attempt to connect to the designated port. 
        /// If that fails, it will attempt to find the first port discovered by UDP. 
        /// If that takes too long and times out, it will attempt to connect to the default port.
        /// </summary>
        /// <param name="onConnect">Callback executed upon successful initialization.</param>
        /// <param name="onDisconnect">Callback executed upon disconnecting from VTS.</param>
        /// <param name="onError">The Callback executed upon failed initialization.</param>
        public void Connect(Action onConnect, Action onDisconnect, Action onError){
            // If the port we're trying to connect to isn't a known port...
            if(!(PORTS_BY_IP.ContainsKey(this._ip) && PORTS_BY_IP[this._ip].ContainsKey(this._port))){
                // First try to connect to the port we proclaim...
                ConnectImpl(this._port, onConnect, onDisconnect, () => {
                    // If that fails, let's try the first port we can find on UDP!
                    Debug.Log(string.Format("Unable to connect to VTube Studio port {0}, waiting for port discovery...", this._port));
                    // Create a callback that will forcibly try to connect to the default port, if we cannot discover a port in time.
                    this._portDiscoveryTimer = DEFAULT_PORT_DISCOVERY_TIMEOUT;
                    this._onPortDiscoveryTimeout = () => {
                        Debug.LogError(string.Format("Wait for port discovery has timed out. Finally, attempting connection on default port {1}.", this._port, DEFAULT_PORT));
                        ClearConnectionCallbacks();
                        ConnectImpl(DEFAULT_PORT, onConnect, onDisconnect, onError);
                    };
                    // Wait until we discover a functional port, then try to connect.
                    this._onLocalPortDiscovered = (port) => {
                        if(port != this._port){
                            ClearConnectionCallbacks();
                            ConnectImpl(port, onConnect, onDisconnect, onError);
                        }
                    };
                });
            }else{
                ConnectImpl(this._port, onConnect, onDisconnect, onError);
            }
        }

        private void ConnectImpl(int port, Action onConnect, Action onDisconnect, Action onError){
            if(this._ws != null){
                Disconnect();
                SetPort(port);
                this._ws.Start(string.Format(VTS_WS_URL, this._ip.ToString(), this._port), onConnect, onDisconnect, onError);
            }else{
                onError.Invoke();
            }
        }

        /// <summary>
        /// Disconnects from VTube Studio.
        /// </summary>
        public void Disconnect(){
            if(this._ws != null){
                ClearConnectionCallbacks();
                this._ws.Stop();
            }
        }

        private void ClearConnectionCallbacks(){
            // Wipe the callbacks so we don't keep re-firing them after a successful connection or disconnect.
            this._onLocalPortDiscovered = null;
            this._onPortDiscoveryTimeout = null;
            this._portDiscoveryTimer = 0f;
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
