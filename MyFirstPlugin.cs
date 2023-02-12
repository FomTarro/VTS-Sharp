﻿using UnityEngine;
using VTS;
using VTS.Unity;

public class MyFirstPlugin : VTSPlugin {
	// Start is called before the first frame update
	private void Start() {
		// Everything you need to get started!
		Initialize(
			new WebSocketSharpImpl(this.Logger),
			new NewtonsoftJsonUtilityImpl(),
			new TokenStorageImpl(Application.persistentDataPath),
			// onConnect
			() => { this.Logger.Log("Connected!"); },
			// onDisconnect
			() => { this.Logger.LogWarning("Disconnected!"); },
			// onError
			(error) => { this.Logger.LogError("Error! - " + error.data.message); });
	}

	private void Update(){
		Tick(Time.deltaTime);
	}
}
