using System;

using UnityEngine;

using VTS.Core;

namespace VTS.Unity {

	public class UnityVTSLoggerImpl : IVTSLogger {
		public void Log(string message) {
			Debug.Log(message);
		}

		public void LogError(string error) {
			Debug.LogError(error);
		}

		public void LogError(Exception error) {
			Debug.LogError(error);
		}

		public void LogWarning(string message) {
			Debug.LogWarning(message);
		}
	}
}
