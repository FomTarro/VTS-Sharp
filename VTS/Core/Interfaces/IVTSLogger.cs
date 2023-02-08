namespace VTS {

	/// <summary>
	/// Interface for providing a logging implementation.
	/// </summary>
	public interface IVTSLogger {
		/// <summary>
		/// Logs a message.
		/// </summary>
		/// <param name="message">Message to log.</param>
		void Log(string message);
		/// <summary>
		/// Logs a warning.
		/// </summary>
		/// <param name="message">Warning to log.</param>
		void LogWarning(string message);
		/// <summary>
		/// Logs an error.
		/// </summary>
		/// <param name="error">Error to log.</param>
		void LogError(string error);
	}
}
