﻿namespace VTS.Core {

	/// <summary>
	/// Interface for providing an Auth Token Storage implementation.
	/// </summary>
	public interface ITokenStorage {
		/// <summary>
		/// Loads the auth token.
		/// </summary>
		/// <returns></returns>
		string LoadToken();
		/// <summary>
		/// Saves an auth token.
		/// </summary>
		/// <param name="token">The token to save.</param>
		void SaveToken(string token);
		/// <summary>
		/// Deletes the auth token.
		/// </summary>
		void DeleteToken();
	}
}
