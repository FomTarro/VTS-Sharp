using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace VTS.Networking{
    public interface IWebSocket {
        ConcurrentQueue<string> RecieveQueue { get; }
        Task Connect(string URL, System.Action onConnect, System.Action onError);
        bool IsConnecting();
        bool IsConnectionOpen();
        void Send(string message);
    }
}
