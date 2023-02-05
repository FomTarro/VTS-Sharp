using System.Collections.Generic;
using System;
using VTS.Models;
using VTS.Networking;

namespace VTS.Core {
	public interface IVTSWebSocket {
		int Port { get; }

		void Connect(Action onConnect, Action onDisconnect, Action onError);
		void Disconnect();
        void Dispose();
		Dictionary<int, VTSStateBroadcastData> GetPorts();
		void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility);
		void ResubscribeToEvents();
		void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError)
			where T : VTSMessageData
			where K : VTSMessageData;
		void SendEventSubscription<T, K>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe)
			where T : VTSEventSubscriptionRequestData
			where K : VTSEventData;
		bool SetIPAddress(string ipString);
		bool SetPort(int port);
	}
}