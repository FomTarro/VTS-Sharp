using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace VTS.Networking{
    /// <summary>
    /// Interface for providing a websocket implementation.
    /// </summary>
    public interface IWebSocket {
        /// <summary>
        /// The queue of recieved payloads. 
        /// 
        /// Because Unity can only do most tasks on the main thread, 
        /// response payloads will need to be placed into this queue, as they will be dequeued via a poller in the Update lifecycle method.
        /// </summary>
        /// <value></value>
        ConcurrentQueue<string> RecieveQueue { get; }
        /// <summary>
        /// Connects to the given URL and executes the relevant callback on completion.
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="onConnect">Callback executed upon conencting to the URL/</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        /// <returns></returns>
        Task Connect(string URL, System.Action onConnect, System.Action onError);
        void Abort();
        /// <summary>
        /// Is the socket in the process of connecting?
        /// </summary>
        /// <returns>Is the socket in the process of connecting?</returns>
        bool IsConnecting();
        /// <summary>
        /// Has the socket successfully connected?
        /// </summary>
        /// <returns>Has the socket successfully connected?</returns>
        bool IsConnectionOpen();
        /// <summary>
        /// Send a payload to the websocket server.
        /// </summary>
        /// <param name="message">The payload to send.</param>
        void Send(string message);
    }
}
