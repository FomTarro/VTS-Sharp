using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Concurrent;

namespace VTS.Networking.Impl{
    public class WebSocketImpl : IWebSocket
    {
        private string _url = null;
        private static UTF8Encoding ENCODER = new UTF8Encoding();
        private const UInt64 MAX_READ_SIZE = 1 * 1024 * 1024;

        // WebSocket
        private ClientWebSocket _ws = new ClientWebSocket();

        // Queues
        private ConcurrentQueue<string> recieveQueue { get; }
        private BlockingCollection<ArraySegment<byte>> _sendQueue { get; }
        
        // Threads
        private Thread _receiveThread { get; set; }
        private Thread _sendThread { get; set; }

        private bool _reconnecting = true;
        private bool _disposed = false;
        private object _connectionLock = new object();

        public WebSocketImpl(){
            _ws = new ClientWebSocket();
            recieveQueue = new ConcurrentQueue<string>();
            _receiveThread = new Thread(RunReceive);
            _receiveThread.Start();
            _sendQueue = new BlockingCollection<ArraySegment<byte>>();
            _sendThread = new Thread(RunSend);
            _sendThread.Start();
        }

        ~WebSocketImpl(){
            this.Dispose();
        }

        public async Task Connect(string URL, System.Action onConnect, System.Action onError)
        {
            this._url = URL;
            lock(this._connectionLock){
                this._reconnecting = true;
            }
            Uri serverUri = new Uri(URL);
            Debug.Log("Connecting to: " + serverUri);
            await _ws.ConnectAsync(serverUri, CancellationToken.None);
            while(IsConnecting())
            {
                Debug.Log("Waiting to connect...");
                Task.Delay(50).Wait();
            }
            Debug.Log("Connect status: " + _ws.State);
            if(_ws.State == WebSocketState.Open){
                lock(this._connectionLock){
                    this._reconnecting = false;
                }
                onConnect();
            }else{
                onError();
            }
        }

        private async Task Reconnect(){
            lock(this._ws){
                this._ws = new ClientWebSocket();
            }
            await Connect(this._url, () => { Debug.Log("Reconnected!"); }, async () => { await Reconnect() ;} );
        }

        public void Dispose(){
            Debug.LogWarning("Disposing of socket...");
            this._disposed = true;
            this._ws.Dispose();
        }

        #region Status
        public bool IsConnecting()
        {   
            return _ws.State == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen()
        {
            return _ws.State == WebSocketState.Open && !this.IsConnecting();
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

        private async void RunSend()
        {
            Debug.Log("WebSocket Message Sender looping.");
            ArraySegment<byte> msg;
            bool proceed = true;
            int counter = 0;
            while (!this._disposed)
            {
                while (!_sendQueue.IsCompleted && this.IsConnectionOpen() && !this._disposed)
                {
                    lock(this._connectionLock){
                        proceed = !this._reconnecting;
                    }
                    if(!proceed){
                        continue;
                    }
                    try{
                        counter++;
                        if(counter >= 1000){
                            //throw new WebSocketException("CHAOS MONKEY");
                        }
                        msg = _sendQueue.Take();
                        // Debug.Log("Dequeued this message to send: " + msg);
                        await _ws.SendAsync(msg, WebSocketMessageType.Text, true /* is last part of message */, CancellationToken.None);
                    }catch(Exception e){
                        Debug.LogError(e);
                        // put unsent messages back on the queue
                        _sendQueue.Add(msg);
                        if(e is WebSocketException 
                        || e is System.IO.IOException 
                        || e is System.Net.Sockets.SocketException){
                            counter = 0;
                            lock(this._connectionLock){
                                this._reconnecting = true;
                            }
                            Debug.LogWarning("Socket exception occured, reconnecting...");
                            await Reconnect();
                        }
                    }
                }
            }
            Debug.Log("WebSocket Message Sender ending.");
        }
        #endregion

        #region Receive

        public string GetNextResponse()
        {
            string data = null;
            this.recieveQueue.TryDequeue(out data);
            return data;
        }

        private async Task<string> Receive(UInt64 maxSize = MAX_READ_SIZE)
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
                    chunkResult = await _ws.ReceiveAsync(arrayBuf, CancellationToken.None);
                    ms.Write(arrayBuf.Array, arrayBuf.Offset, chunkResult.Count);
                    //Debug.Log("Size of Chunk message: " + chunkResult.Count);
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

        private async void RunReceive()
        {
            Debug.Log("WebSocket Message Receiver looping.");
            string result;
            while (!this._disposed)
            {
                result = await Receive();
                if (result != null && result.Length > 0)
                {
                    recieveQueue.Enqueue(result);
                }
                else
                {
                    Task.Delay(50).Wait();
                }
            }
            Debug.Log("WebSocket Message Receiver thread ending.");
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