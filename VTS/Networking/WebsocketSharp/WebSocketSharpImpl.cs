using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;

namespace VTS.Networking.Impl{

    public class WebSocketSharpImpl : IWebSocket
    {
        private static UTF8Encoding ENCODER = new UTF8Encoding();

        private WebSocket _socket;
        private Queue<string> _intakeQueue = new Queue<string>();
        public WebSocketSharpImpl(){
            this._intakeQueue = new Queue<string>();
        }

        public string GetNextResponse()
        {
            return this._intakeQueue.Count > 0 ? this._intakeQueue.Dequeue() : null;
        }

        public bool IsConnecting()
        {
            return this._socket != null && this._socket.ReadyState == WebSocketState.CONNECTING;
        }

        public bool IsConnectionOpen()
        {
            return this._socket != null && this._socket.ReadyState == WebSocketState.OPEN;
        }

        public void Send(string message)
        {
            byte[] buffer = ENCODER.GetBytes(message);
            ArraySegment<byte> sendBuf = new ArraySegment<byte>(buffer);;
            this._socket.Send(buffer);
        }

        public void Start(string URL, Action onConnect, Action onDisconnect, Action onError)
        {
            this._socket = new WebSocket(URL);
            this._socket.OnMessage += (sender, e) => { this._intakeQueue.Enqueue(e.Data); };
            this._socket.OnOpen += (sender, e) => { onConnect(); };
            this._socket.OnError += (sender, e) => { 
                Debug.LogError(e.Message);
                onError(); 
            };
            this._socket.OnClose += (sender, e) => { onDisconnect(); };
            this._socket.ConnectAsync();
        }

        public void Stop()
        {
            this._socket.Close();
        }
    }
}
