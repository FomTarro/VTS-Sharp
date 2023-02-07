using System;
using System.Collections.Generic;

namespace VTS {

	public interface IVTSWebSocket {
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
		void Connect(Action onConnect, Action onDisconnect, Action onError);
		/// <summary>
		/// Disconnects from VTube Studio.
		/// </summary>
		void Disconnect();
		void Dispose();
		/// <summary>
		/// Returns a map of ports available to the current IP Address. Indexed by port number.
		/// </summary>
		/// <returns></returns>
		Dictionary<int, VTSStateBroadcastData> GetPorts();
		void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, IVTSLogger logger);
		void ResubscribeToEvents();
		void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError)
			where T : VTSMessageData
			where K : VTSMessageData;
		void SendEventSubscription<T, K>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe)
			where T : VTSEventSubscriptionRequestData
			where K : VTSEventData;
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
	}
}