using System;
using System.Collections.Generic;

namespace VTS.Core {

	public interface IVTSWebSocket {
		/// <summary>
		/// The port number of the socket.
		/// </summary>
		/// <value></value>
		int Port { get; }
		/// <summary>
		/// Connects to VTube Studio on the current port, executing the provided callbacks during different phases of the connection lifecycle.
		/// Will first attempt to connect to the designated port. 
		/// If that fails, it will attempt to find the first port discovered by UDP. 
		/// If that takes too long and times out, it will attempt to connect to the default port.
		/// </summary>
		/// <param name="onConnect">Callback executed upon successful initialization.</param>
		/// <param name="onDisconnect">Callback executed upon disconnecting from VTS.</param>
		/// <param name="onError">The Callback executed upon failed initialization.</param>
		void Connect(Action onConnect, Action onDisconnect, Action<Exception> onError);
		/// <summary>
		/// Disconnects from VTube Studio.
		/// </summary>
		void Disconnect();
		/// <summary>
		/// Disposes of the socket.
		/// </summary>
		void Dispose();
		/// <summary>
		/// Returns a map of ports available to the current IP Address. Indexed by port number.
		/// </summary>
		/// <returns></returns>
		Dictionary<int, VTSStateBroadcastData> GetPorts();
		void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, IVTSLogger logger);
		void ResubscribeToEvents();
		/// <summary>
		/// Sends a payload of type T, expecting a response of type K. 
		/// </summary>
		/// <param name="request">The request payload.</param>
		/// <param name="onSuccess">Callback executed upon receiving a response.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		/// <typeparam name="T">The request type.</typeparam>
		/// <typeparam name="K">The response type.</typeparam>
		/// <returns></returns>
		void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError)
			where T : VTSMessageData
			where K : VTSMessageData;
		/// <summary>
		/// Sends an event subscription payload of type T, expecting a response of type K.
		/// </summary>
		/// <param name="request">The subscription request payload.</param>
		/// <param name="onEvent">Callback executed upon receiving an event.</param>
		/// <param name="onSubscribe">Callback executed upon subscribing.</param>
		/// <param name="onError">Callback executed upon receiving an error.</param>
		/// <param name="resubscribe">Callback that executes a resubscription.</param>
		/// <typeparam name="T">The request type.</typeparam>
		/// <typeparam name="K">The response type.</typeparam>
		/// <returns></returns>
		void SendEventSubscription<T, K, V>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe)
			where T : VTSEventSubscriptionRequestData<V>
			where K : VTSEventData
			where V : VTSEventConfigData;
		/// <summary>
		/// Sets the connection IP address to the given string. Returns true if the string is a valid IP Address format, returns false otherwise.
		/// If the IP Address is changed while an active connection exists, you will need to reconnect.
		/// </summary>
		/// <param name="ipString">The string form of the IP address, in dotted-quad notation for IPv4.</param>
		/// <returns>True if the string is a valid IP Address format, False otherwise.</returns>
		bool SetIPAddress(string ipString);
		/// <summary>
		/// Sets the connection port to the given number. Returns true if the port is a valid VTube Studio port, returns false otherwise. 
		/// If the port number is changed while an active connection exists, you will need to reconnect.
		/// </summary>
		/// <param name="port">The port to connect to.</param>
		/// <returns>True if the port is a valid VTube Studio port, False otherwise.</returns>
		bool SetPort(int port);
		/// <summary>
		/// Method that is to be called by the system once per tick.
		/// </summary>
		/// <param name="timeDelta">The time since the last update tick, in seconds.</param>
		void Tick(float timeDelta);
	}
}