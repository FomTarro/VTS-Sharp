using UnityEngine;

using VTS.Core;

namespace VTS.Unity.Examples {

	public class MyFirstPlugin : UnityVTSPlugin {
		protected override VTSPluginDependencies DependencyImplementations
		{
			get
			{
				IVTSLogger logger = new UnityVTSLoggerImpl();
				return new()
				{
					socket=new WebSocketImpl(logger),
					jsonUtility=new NewtonsoftJsonUtilityImpl(),
					tokenStorage=new TokenStorageImpl(Application.persistentDataPath),
					logger=logger
				};
			}
		}

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
