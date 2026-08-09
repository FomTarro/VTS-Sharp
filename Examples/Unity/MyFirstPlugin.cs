using UnityEngine;

using VTS.Core;

namespace VTS.Unity.Examples {

	public class MyFirstPlugin : UnityVTSPlugin {
		public override IWebSocket Socket => new WebSocketImpl(this.Logger);

		public override IJsonUtility JsonUtility => new NewtonsoftJsonUtilityImpl();

		public override ITokenStorage TokenStorage => new TokenStorageImpl(Application.persistentDataPath);

		public override IVTSLogger Logger => new UnityVTSLoggerImpl();

		// Start is called before the first frame update
		private void Start() {
			// Everything you need to get started!
			Initialize(
				// onConnect
				() => this.Logger.Log("Connected!"),
				// onDisconnect
				() => this.Logger.LogWarning("Disconnected!"),
				// onError
				(error) => this.Logger.LogError("Error! - " + error.data.message));
		}
	}
}
