namespace VTS {

	public interface IVTSLogger {
		void Log(string message);
		void LogWarning(string message);
		void LogError(string error);
	}
}
