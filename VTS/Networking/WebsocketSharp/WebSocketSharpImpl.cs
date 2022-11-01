using System;
using System.Text;
using System.Collections.Concurrent;
using UnityEngine;
using WebSocketSharp;

namespace VTS.Networking.Impl {

    public class WebSocketSharpImpl : IWebSocket {
        private static UTF8Encoding ENCODER = new UTF8Encoding();

        private WebSocket _socket;
        private ConcurrentQueue<string> _intakeQueue = new ConcurrentQueue<string>();
        private bool _attemptReconnect = false;

        private System.Action _onConnect = () => {};
        private System.Action _onDisconnect = () => {};
        private System.Action _onError = () => {};
        private string _url = "";

        public WebSocketSharpImpl(){
            this._intakeQueue = new ConcurrentQueue<string>();
        }

        public string GetNextResponse(){
            string response = null;
            this._intakeQueue.TryDequeue(out response);
            return response;
        }

        public bool IsConnecting(){
            return this._socket != null && this._socket.ReadyState == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen() {
            return this._socket != null && this._socket.ReadyState == WebSocketState.Open;
        }

        public void Send(string message){
            // byte[] buffer = ENCODER.GetBytes(message);
            this._socket.SendAsync(message, (success) => {});
        }

        public void Start(string URL, Action onConnect, Action onDisconnect, Action onError) {
            this._url = URL;
            // WebSocket oldSocket = this._socket;
            // if(this._socket != null){
            //     // this._socket.Close();
            // }
            this._socket = new WebSocket(this._url);
            Debug.Log(string.Format("Attempting to connect to {0}", this._socket.Url));
            this._socket.WaitTime = TimeSpan.FromSeconds(10);
            this._socket.Log.Output = (l, m) => {
                switch(l.Level){
                    case LogLevel.Fatal:
                    case LogLevel.Trace:
                    case LogLevel.Error:
                        Debug.LogError(string.Format("[{0}] - Socket error: {1}", this._socket.Url, l.Message));
                        break;
                    case LogLevel.Warn:
                        Debug.LogError(string.Format("[{0}] - Socket warning: {1}", this._socket.Url, l.Message));
                        break;
                    default:
                        Debug.LogError(string.Format("[{0}] - Socket info: {1}", this._socket.Url, l.Message));
                        break;
                }
            };
            this._onConnect = onConnect;
            this._onDisconnect = onDisconnect;
            this._onError = onError;
            this._socket.OnMessage += (sender, e) => {
                MainThreadUtil.Run(() => {
                    if(e != null && e.IsText){
                        this._intakeQueue.Enqueue(e.Data); 
                    }
                });
            };
            this._socket.OnOpen += (sender, e) => { 
                MainThreadUtil.Run(() => {
                    this._onConnect();
                    Debug.Log(string.Format("[{0}] - Socket open!", this._socket.Url));
                    this._attemptReconnect = true;
                });
            };
            this._socket.OnError += (sender, e) => { 
                MainThreadUtil.Run(() => {
                    Debug.LogError(string.Format("[{0}] - Socket error...", this._socket.Url));
                    if(e != null){
                        Debug.LogError(string.Format("'{0}', {1}", e.Message, e.Exception));
                    }
                    this._onError();
                });
            };
            this._socket.OnClose += (sender, e) => { 
                MainThreadUtil.Run(() => {
                    string msg = string.Format("[{0}] - Socket closing: {1}, '{2}', {3}", this._socket.Url, e.Code, e.Reason, e.WasClean);
                    if(e.WasClean){
                        Debug.Log(msg);
                        this._onDisconnect();
                    }else{
                        Debug.LogError(msg);
                        this._onError();
                        if(this._attemptReconnect){
                            Reconnect();
                        }
                    }
                });
            };

            this._socket.ConnectAsync();
        }

        public void Stop(){
            this._attemptReconnect = false;
            if(this._socket != null && this._socket.IsAlive){
                this._socket.Close();
            }
        }

        private void Reconnect(){
            Start(this._url, this._onConnect, this._onDisconnect, this._onError);
        }
    }

    /// <summary>
    /// Helper class for queueing method calls on to the main thread, which is necessary for most Unity methods.
    /// This class is not necessary for non-Unity uses.
    /// </summary>
    public class MainThreadUtil : MonoBehaviour {
        private static MainThreadUtil INSTANCE;
        public static MainThreadUtil Instance { get { return INSTANCE; } } 
        private static ConcurrentQueue<System.Action> CALL_QUEUE = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Setup(){
            INSTANCE = new GameObject("MainThreadUtil").AddComponent<MainThreadUtil>();
        }

        private void Awake(){
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Enqueue an action to be run on the main Unity thread.
        /// </summary>
        /// <param name="action">The action to run</param>
        public static void Run(System.Action action){
            CALL_QUEUE.Enqueue(action);
        }

        private void Update(){
            do{
                System.Action action = null;
                if(CALL_QUEUE.Count > 0 && CALL_QUEUE.TryDequeue(out action)){
                    try{
                        action();
                    }catch(Exception e){
                        Debug.LogError(String.Format("Socket error: {0}", e.StackTrace));
                    }
                }
            }while(CALL_QUEUE.Count > 0);
        }
    }
}
