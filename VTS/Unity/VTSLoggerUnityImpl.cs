﻿using UnityEngine;

namespace VTS.Unity {

	public class VTSLoggerUnityImpl : IVTSLogger {
		public void Log(string message) {
			Debug.Log(message);
		}

		public void LogError(string error) {
			Debug.LogError(error);
		}

		public void LogWarning(string message) {
			Debug.LogWarning(message);
		}
	}
}
