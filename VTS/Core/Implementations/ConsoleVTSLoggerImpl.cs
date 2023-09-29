using System;

namespace VTS.Core {

	public class ConsoleVTSLoggerImpl : IVTSLogger {

		public void Log(string message) {
			Console.WriteLine(string.Format("[Info] - {0}", message));
		}

		public void LogError(string error) {
			Console.WriteLine(string.Format("[Error] - {0}", error));
		}

		public void LogError(Exception error) {
			Console.WriteLine(string.Format("[Error] - {0}", error));
		}

		public void LogWarning(string warning) {
			Console.WriteLine(string.Format("[Warn] - {0}", warning));
		}
	}
}