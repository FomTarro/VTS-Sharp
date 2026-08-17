#if GODOT

using System;
using Godot;
using VTS.Core;

namespace VTS.Godot {

	public class GodotVTSLoggerImpl : IVTSLogger {
		public void Log(string message) {
			GD.Print(message);
		}

		public void LogError(string error) {
			GD.PushError(error);
		}

		public void LogError(Exception error) {
			GD.PushError(error);
		}

		public void LogWarning(string message) {
			GD.PushWarning(message);
		}
	}
}

#endif