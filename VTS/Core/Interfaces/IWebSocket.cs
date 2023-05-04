using System;

namespace VTS.Core {

	/// <summary>
	/// Interface for providing a websocket implementation.
	/// </summary>
	public interface IWebSocket {
		/// <summary>
		/// Fetches the next response to process.
		/// </summary>
		/// <value></value>
		string GetNextResponse();
		/// <summary>
		/// Connects to the given URL and executes the relevant callback on completion.
		/// </summary>
		/// <param name="URL">URL to connect to.</param>
		/// <param name="onConnect">Callback executed upon conencting to the URL.</param>
		/// <param name="onDisconnect">Callback executed upon disconnecting from the URL.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		/// <returns></returns>
		void Start(string URL, Action onConnect, Action onDisconnect, Action<Exception> onError);
		/// <summary>
		/// Closes the websocket.
		/// 
		/// Executes the onDisconnect callback as specified in the Start method call.
		/// </summary>
		void Stop();
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
		/// <summary>
		/// Method that is called by the system once per tick, to process incoming events.
		/// </summary>
		/// <param name="timeDelta">The time since the last update tick, in seconds.</param>
		void Tick(float timeDelta);
	}
}
