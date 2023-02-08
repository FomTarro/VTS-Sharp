using UnityEngine;
using VTS;
using VTS.Unity;

public class MyFirstPlugin : VTSPlugin {
	// Start is called before the first frame update
	void Start() {
		// Everything you need to get started!
		Initialize(
			new WebSocketSharpImpl(this.Logger),
			new JsonUtilityUnityImpl(),
			new TokenStorageImpl(Application.persistentDataPath),
			// onConnect
			() => { this.Logger.Log("Connected!"); },
			// onDisconnect
			() => { this.Logger.LogWarning("Disconnected!"); },
			// onError
			() => { this.Logger.LogError("Error!"); });
	}
}
