using System;
using System.Collections.Generic;
using UnityEngine;

namespace VTS.Unity {

	/// <summary>
	/// Underlying VTS socket connection and response processor.
	/// </summary>
	public class VTSWebSocket : MonoBehaviour, IVTSWebSocket {
		private VTS.Core.VTSWebSocket _socket = null;
		public VTS.Core.VTSWebSocket Socket {
			get {
				return this._socket;
			}
		}

		public int Port { get { return this.Socket.Port; } }

		public void Tick(float timeDelta){
			this.Socket.Tick(timeDelta);
		}

		private void OnDestroy() {
			Dispose();
		}

		public void Connect(Action onConnect, Action onDisconnect, Action<Exception> onError) {
			this.Socket.Connect(onConnect, onDisconnect, onError);
		}

		public void Disconnect() {
			this.Socket.Disconnect();
		}

		public void Dispose() {
			this.Socket.Dispose();
		}

		public Dictionary<int, VTSStateBroadcastData> GetPorts() {
			return this.Socket.GetPorts();
		}

		public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility, IVTSLogger logger) {
			this._socket = new Core.VTSWebSocket();
			this.Socket.Initialize(webSocket, jsonUtility, logger);
		}

		public void ResubscribeToEvents() {
			this.Socket.ResubscribeToEvents();
		}

		public void Send<T, K>(T request, Action<K> onSuccess, Action<VTSErrorData> onError)
			where T : VTSMessageData
			where K : VTSMessageData {
			this.Socket.Send<T, K>(request, onSuccess, onError);
		}

		public void SendEventSubscription<T, K>(T request, Action<K> onEvent, Action<VTSEventSubscriptionResponseData> onSubscribe, Action<VTSErrorData> onError, Action resubscribe)
			where T : VTSEventSubscriptionRequestData
			where K : VTSEventData {
			this.Socket.SendEventSubscription<T, K>(request, onEvent, onSubscribe, onError, resubscribe);
		}

		public bool SetIPAddress(string ipString) {
			return this.Socket.SetIPAddress(ipString);
		}

		public bool SetPort(int port) {
			return this.Socket.SetPort(port);
		}
	}
}
