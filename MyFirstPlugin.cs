using UnityEngine;
using VTS;
using VTS.Unity;

public class MyFirstPlugin : VTSPlugin {
	// Start is called before the first frame update
	void Start() {
		// Everything you need to get started!
		Initialize(
			new WebSocketSharpImpl(),
			new JsonUtilityImpl(),
			new TokenStorageImpl(Application.persistentDataPath),
			// onConnect
			() => { Debug.Log("Connected!"); },
			// onDisconnect
			() => { Debug.LogWarning("Disconnected!"); },
			// onError
			() => { Debug.LogError("Error!"); });
	}
}
