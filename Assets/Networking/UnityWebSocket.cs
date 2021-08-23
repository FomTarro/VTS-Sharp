using UnityEngine;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace VTS.Networking.Impl{
    public class UnityWebSocket
    {
        private static UTF8Encoding ENCODER = new UTF8Encoding();
        private const UInt64 MAX_READ_SIZE = 1 * 1024 * 1024;

        // WebSocket
        private ClientWebSocket _ws = new ClientWebSocket();

        // Server address
        private Uri _serverUri;

        // Queues
        public ConcurrentQueue<String> RecieveQueue { get; }
        public BlockingCollection<ArraySegment<byte>> SendQueue { get; }
        
        // Threads
        private Thread _receiveThread { get; set; }
        private Thread _sendThread { get; set; }

        public UnityWebSocket(string serverURL){
            _ws = new ClientWebSocket();
            _serverUri = new Uri(serverURL);
            RecieveQueue = new ConcurrentQueue<string>();
            _receiveThread = new Thread(RunReceive);
            _receiveThread.Start();
            SendQueue = new BlockingCollection<ArraySegment<byte>>();
            _sendThread = new Thread(RunSend);
            _sendThread.Start();
        }

        public async Task Connect()
        {
            Debug.Log("Connecting to: " + _serverUri);
            await _ws.ConnectAsync(_serverUri, CancellationToken.None);
            while (IsConnecting())
            {
                Debug.Log("Waiting to connect...");
                Task.Delay(50).Wait();
            }
            Debug.Log("Connect status: " + _ws.State);
        }

        #region [Status]
        public bool IsConnecting()
        {
            return _ws.State == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen()
        {
            return _ws.State == WebSocketState.Open;
        }
        #endregion

        #region [Send]
        public void Send(string message)
        {
            byte[] buffer = ENCODER.GetBytes(message);
            // Debug.Log("Message to queue for send: " + buffer.Length + ", message: " + message);
            ArraySegment<byte> sendBuf = new ArraySegment<byte>(buffer);
            SendQueue.Add(sendBuf);
        }

        private async void RunSend()
        {
            Debug.Log("WebSocket Message Sender looping.");
            ArraySegment<byte> msg;
            while (true)
            {
                while (!SendQueue.IsCompleted && this.IsConnectionOpen())
                {
                    msg = SendQueue.Take();
                    // Debug.Log("Dequeued this message to send: " + msg);
                    await _ws.SendAsync(msg, WebSocketMessageType.Text, true /* is last part of message */, CancellationToken.None);
                }
            }
        }
        #endregion

        #region [Receive]
        private async Task<string> Receive(UInt64 maxSize = MAX_READ_SIZE)
        {
            // A read buffer, and a memory stream to stuff unknown number of chunks into:
            byte[] buf = new byte[4 * 1024];
            var ms = new MemoryStream();
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
            while (true)
            {
                result = await Receive();
                if (result != null && result.Length > 0)
                {
                    RecieveQueue.Enqueue(result);
                }
                else
                {
                    Task.Delay(50).Wait();
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