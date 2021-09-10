using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace VTS.Networking.Impl{
    /// <summary>
    /// Basic Websocket implementation. 
    /// 
    /// It is strongly recommended that you replace this with a more robust solution, such as WebsocketSharp.
    /// </summary>
    public class WebSocketImpl : IWebSocket
    {
        private string _url = null;
        private static UTF8Encoding ENCODER = new UTF8Encoding();
        private const UInt64 MAX_READ_SIZE = 1 * 1024 * 1024;

        // WebSocket
        private ClientWebSocket _ws = new ClientWebSocket();

        // Queues
        private ConcurrentQueue<string> _receiveQueue { get; }
        private BlockingCollection<ArraySegment<byte>> _sendQueue { get; }
        private object _connectionLock = new object();
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        private System.Action _onReconnect = () => {};

        #region  Lifecycle
        public WebSocketImpl(){
            _receiveQueue = new ConcurrentQueue<string>();
            _sendQueue = new BlockingCollection<ArraySegment<byte>>();
        }

        ~WebSocketImpl(){
            this.Dispose();
        }

        public async Task Connect(string URL, System.Action onConnect, System.Action onError)
        {
            try{
                // Tasks
                if(this._tokenSource != null){
                    _tokenSource.Cancel();
                }
                // if(this._ws != null && this.IsConnectionOpen()){
                //     await this._ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting...", CancellationToken.None);
                // }
                this._ws = new ClientWebSocket();
                // _ws.Options.KeepAliveInterval = new TimeSpan(0, 0, 10);

                this._tokenSource = new CancellationTokenSource();
                this._token = _tokenSource.Token;
                Task send = new Task(() => RunSend(this._ws, this._token), this._token);
                send.Start();
                Task receive = new Task(() => RunReceive(this._ws, this._token), this._token);
                receive.Start();

                this._onReconnect = onConnect;
                this._url = URL;
                Uri serverUri = new Uri(URL);
                Debug.Log("Connecting to: " + serverUri);
                await this._ws.ConnectAsync(serverUri, this._token);
                while(IsConnecting())
                {
                    Debug.Log("Waiting to connect...");
                    await Task.Delay(10);
                }
                Debug.Log("Connect status: " + this._ws.State);
                if(this._ws.State == WebSocketState.Open){
                    onConnect();
                    // start routines
                }else{
                    onError();
                }
            }catch(Exception e){
                Debug.LogError(e);
            }
        }

        private async Task Reconnect(){
            await Connect(this._url, this._onReconnect, async () => { 
                // keep retrying 
                Debug.LogError("Reconnect failed, trying again!");
                await Reconnect();
            } );
        }

        public void Dispose(){
            Debug.LogWarning("Disposing of socket...");
            this._tokenSource.Cancel();
        }
        #endregion

        #region Status
        public bool IsConnecting()
        {   
            return this._ws != null && this._ws.State == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen()
        {
            return this._ws != null && this._ws.State == WebSocketState.Open && !this.IsConnecting();
        }
        #endregion

        #region Send
        public void Send(string message)
        {
            byte[] buffer = ENCODER.GetBytes(message);
            // Debug.Log("Message to queue for send: " + buffer.Length + ", message: " + message);
            ArraySegment<byte> sendBuf = new ArraySegment<byte>(buffer);
            _sendQueue.Add(sendBuf);
        }

        private async void RunSend(ClientWebSocket socket, CancellationToken token)
        {
            Debug.Log("WebSocket Message Sender looping.");
            ArraySegment<byte> msg;
            int counter = 0;
            while(!token.IsCancellationRequested)
            {
                if(!this._sendQueue.IsCompleted && this.IsConnectionOpen())
                {
                    try{
                        counter++;
                        if(counter >= 1000){
                            counter = 0;
                            throw new WebSocketException("CHAOS MONKEY");
                        }
                        Debug.Log("sending");
                        msg = _sendQueue.Take();
                        await socket.SendAsync(msg, WebSocketMessageType.Text, true /* is last part of message */, token);
                    }catch(Exception e){
                        Debug.LogError(e);
                        // put unsent messages back on the queue
                        _sendQueue.Add(msg);
                        if(e is WebSocketException 
                        || e is System.IO.IOException 
                        || e is System.Net.Sockets.SocketException){
                            Debug.LogWarning("Socket exception occured, reconnecting...");
                            await Reconnect();
                        }
                    }
                }
                await Task.Delay(2);
            }
        }
        #endregion

        #region Receive

        public string GetNextResponse()
        {
            string data = null;
            this._receiveQueue.TryDequeue(out data);
            return data;
        }

        private async Task<string> Receive(ClientWebSocket socket, CancellationToken token, UInt64 maxSize = MAX_READ_SIZE)
        {
            // A read buffer, and a memory stream to stuff unknown number of chunks into:
            byte[] buf = new byte[4 * 1024];
            MemoryStream ms = new MemoryStream();
            ArraySegment<byte> arrayBuf = new ArraySegment<byte>(buf);
            WebSocketReceiveResult chunkResult = null;
            if (IsConnectionOpen())
            {
                do
                {
                    chunkResult = await socket.ReceiveAsync(arrayBuf, token);
                    ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
                    if ((UInt64)(chunkResult.Count) > MAX_READ_SIZE)
                    {
                        Console.Error.WriteLine("Warning: Message is bigger than expected!");
                    }
                } while (!chunkResult.EndOfMessage);
                ms.Seek(0, SeekOrigin.Begin);
                // Looking for UTF-8 JSON type messages.
                if (chunkResult.MessageType == WebSocketMessageType.Text)
                {
                    return StreamToString(ms, Encoding.UTF8);
                }
            }
            return "";
        }

        private async void RunReceive(ClientWebSocket socket, CancellationToken token)
        {
            Debug.Log("WebSocket Message Receiver looping.");
            string result;
            while(!token.IsCancellationRequested)
            {
                result = await Receive(socket, token);
                if (result != null && result.Length > 0)
                {
                    _receiveQueue.Enqueue(result);
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }
        #endregion

        private static string StreamToString(MemoryStream ms, Encoding encoding)
        {
            string readString = "";
            if (encoding == Encoding.UTF8)
            {
                using (var reader = new StreamReader(ms, encoding))
                {
                    readString = reader.ReadToEnd();
                }
            }
            return readString;
        }
    }
}