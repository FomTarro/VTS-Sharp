using System;
using System.Text;
using System.Collections.Concurrent;
using WebSocketSharp;

namespace VTS {

    public class WebSocketSharpImpl : IWebSocket {
        private static UTF8Encoding ENCODER = new UTF8Encoding();

        private WebSocket _socket;
        private ConcurrentQueue<string> _intakeQueue = new ConcurrentQueue<string>();
        private ConcurrentQueue<Action> _responseQueue = new ConcurrentQueue<Action>();
        private bool _attemptReconnect = false;

        private Action _onConnect = () => {};
        private Action _onDisconnect = () => {};
        private Action<Exception> _onError = (e) => {};
        private string _url = "";
        private IVTSLogger _logger;

        public WebSocketSharpImpl(IVTSLogger logger){
            this._logger = logger;
            this._intakeQueue = new ConcurrentQueue<string>();
            this._responseQueue = new ConcurrentQueue<Action>();
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

        public void Start(string URL, Action onConnect, Action onDisconnect, Action<Exception> onError) {
            this._url = URL;
            // WebSocket oldSocket = this._socket;
            // if(this._socket != null){
            //     // this._socket.Close();
            // }
            this._socket = new WebSocket(this._url);
            this._logger.Log(string.Format("Attempting to connect to {0}", this._socket.Url.Host));
            this._socket.WaitTime = TimeSpan.FromSeconds(10);
            this._socket.Log.Output = (l, m) => {
                switch(l.Level){
                    case LogLevel.Fatal:
                    case LogLevel.Trace:
                    case LogLevel.Error:
                        this._logger.LogError(string.Format("[{0}] - Socket error: {1}", this._socket.Url.Host, l.Message));
                        break;
                    case LogLevel.Warn:
                        this._logger.LogError(string.Format("[{0}] - Socket warning: {1}", this._socket.Url.Host, l.Message));
                        break;
                    default:
                        this._logger.LogError(string.Format("[{0}] - Socket info: {1}", this._socket.Url.Host, l.Message));
                        break;
                }
            };
            this._onConnect = onConnect;
            this._onDisconnect = onDisconnect;
            this._onError = onError;
            this._socket.OnMessage += (sender, e) => {
                this._responseQueue.Enqueue(() => {
                    if(e != null && e.IsText){
                        this._intakeQueue.Enqueue(e.Data); 
                    }
                });
            };
            this._socket.OnOpen += (sender, e) => { 
                this._responseQueue.Enqueue(() => {
                    this._onConnect();
                    this._logger.Log(string.Format("[{0}] - Socket open!", this._socket.Url.Host));
                    this._attemptReconnect = true;
                });
            };
            this._socket.OnError += (sender, e) => { 
                this._responseQueue.Enqueue(() => {
                    this._logger.LogError(string.Format("[{0}] - Socket error...", this._socket.Url.Host));
                    if(e != null){
                        this._logger.LogError(string.Format("'{0}', {1}", e.Message, e.Exception));
                    }
                    this._onError(e.Exception);
                });
            };
            this._socket.OnClose += (sender, e) => { 
                this._responseQueue.Enqueue(() => {
                    string msg = string.Format("[{0}] - Socket closing: {1}, '{2}', {3}", this._socket.Url.Host, e.Code, e.Reason, e.WasClean);
                    if(e.WasClean){
                        this._logger.Log(msg);
                        this._onDisconnect();
                    }else{
                        this._logger.LogError(msg);
                        this._onError(new Exception(msg));
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

        public void Tick(float timeDelta){
            do{
                System.Action action = null;
                if(this._responseQueue.Count > 0 && _responseQueue.TryDequeue(out action)){
                    try{
                        action();
                    }catch(Exception e){
                        this._logger.LogError(String.Format("Socket error: {0}", e.StackTrace));
                    }
                }
            }while(this._responseQueue.Count > 0);
        }
    }
}
