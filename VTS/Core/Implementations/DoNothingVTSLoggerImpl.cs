using System;

namespace VTS {

	public class DoNothingVTSLoggerImpl : IVTSLogger {

		public void Log(string message) {
			// Do Nothing
		}

		public void LogError(string error) {
			// Do Nothing
		}
		
		public void LogError(Exception error) {
			// Do Nothing
		}

		public void LogWarning(string message) {
			// Do Nothing
		}
	}
}