using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VTS.Core {
    public class WebSocketImpl : IWebSocket {
        private static readonly UTF8Encoding Encoder = new UTF8Encoding();

        private ClientWebSocket _socket;
        private readonly ConcurrentQueue<string> _intakeQueue;
        private readonly ConcurrentQueue<Action> _responseQueue;
        private bool _attemptReconnect;

        private Action _onConnect = () => { };
        private Action _onDisconnect = () => { };
        private Action<Exception> _onError = (e) => { };

        private string _url = "";
        private readonly IVTSLogger _logger;

        public WebSocketImpl(IVTSLogger logger) {
            _logger = logger;
            _intakeQueue = new ConcurrentQueue<string>();
            _responseQueue = new ConcurrentQueue<Action>();
        }

        public string GetNextResponse() {
            _intakeQueue.TryDequeue(out var response);
            return response;
        }

        public bool IsConnecting() {
            return _socket?.State == WebSocketState.Connecting;
        }

        public bool IsConnectionOpen() {
            return _socket?.State == WebSocketState.Open;
        }

        public void Send(string message) {
            var buffer = Encoder.GetBytes(message);
            var arraySegment = new ArraySegment<byte>(buffer);
            _socket?.SendAsync(arraySegment, WebSocketMessageType.Text, true, default)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public void Start(string url, Action onConnect, Action onDisconnect, Action<Exception> onError) {
            _url = url;
            _socket = new ClientWebSocket();
            _logger.Log($"Attempting to connect to {_url}");
            _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);
            _onConnect = onConnect;
            _onDisconnect = onDisconnect;
            _onError = onError;

            Task.Run<Task>(async () => {
                try {
                    await _socket.ConnectAsync(new Uri(_url), CancellationToken.None);
                } catch (Exception e) {
                    _responseQueue.Enqueue(() => {
                        _logger.LogError($"[{_url}] - Socket error...");
                        _logger.LogError($"'{e.Message}', {e}");
                        _onError(e);
                    });
                    return;
                }

                _responseQueue.Enqueue(() => {
                    _onConnect();
                    _logger.Log($"[{_url}] - Socket open!");
                    _attemptReconnect = true;
                });

                while (true) {
                    var result = await _socket.ReceiveAsync(CancellationToken.None);
                    if (result.closeStatus == null) {
                        _responseQueue.Enqueue(() => {
                            if (result.buffer != null && result.messageType == WebSocketMessageType.Text) {
                                _intakeQueue.Enqueue(Encoder.GetString(result.buffer));
                            }
                        });
                    } else {
                        _responseQueue.Enqueue(() => {
                            var msg =
                                $"[{_url}] - Socket closing: {result.closeStatus}, '{result.closeStatusDescription}', {result.closeStatus == WebSocketCloseStatus.NormalClosure}";
                            if (result.closeStatus == WebSocketCloseStatus.NormalClosure) {
                                _logger.Log(msg);
                                _onDisconnect();
                            } else {
                                _logger.LogError(msg);
                                _onError(new Exception(msg));
                                if (_attemptReconnect) {
                                    Reconnect();
                                }
                            }
                        });
                    }
                }
            }, CancellationToken.None);
        }

        public void Stop() {
            _attemptReconnect = false;
            if (_socket != null && _socket.State == WebSocketState.Open) {
                _socket.Abort();
            }
        }

        private void Reconnect() {
            Start(_url, _onConnect, _onDisconnect, _onError);
        }

        public void Tick(float timeDelta) {
            do {
                if (_responseQueue.IsEmpty || !_responseQueue.TryDequeue(out var action))
                    continue;

                try {
                    action();
                } catch (Exception e) {
                    _logger.LogError($"Socket error: {e.StackTrace}");
                }
            } while (!_responseQueue.IsEmpty);
        }
    }
}


internal static class WebSocketExtensions {
    public static async Task<(
        byte[] buffer,
        WebSocketMessageType messageType,
        WebSocketCloseStatus? closeStatus,
        string closeStatusDescription
        )> ReceiveAsync(this ClientWebSocket client, CancellationToken cancellationToken) {
        const int maxFrameSize = 1024 * 1024 * 10; // 10 MB
        const int bufferSize = 1024; // 1 KB
        var buffer = new byte[bufferSize];
        var offset = 0;
        var free = buffer.Length;

        while (true) {
            var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer, offset, free), cancellationToken);
            offset += result.Count;
            free -= result.Count;

            if (result.EndOfMessage || result.CloseStatus != null) {
                return (buffer, result.MessageType, result.CloseStatus, result.CloseStatusDescription);
            }

            if (free == 0) {
                // No free space
                // Resize the outgoing buffer
                var newSize = buffer.Length + bufferSize;

                // Check if the new size exceeds a limit
                // It should suit the data it receives
                // This limit however has a max value of 2 billion bytes (2 GB)
                if (newSize > maxFrameSize) {
                    throw new Exception("Maximum size exceeded");
                }

                var newBuffer = new byte[newSize];
                Array.Copy(buffer, 0, newBuffer, 0, offset);
                buffer = newBuffer;
                free = buffer.Length - offset;
            }
        }
    }
}