using System.IO;
using System.Text;

namespace VTS {

	public class TokenStorageImpl : ITokenStorage {

		private static readonly UTF8Encoding ENCODER = new UTF8Encoding();
		private string _fileName = "token.json";
		private string _path = "";

		public TokenStorageImpl(string root) {
			this._path = Path.Combine(root, this._fileName);
		}

		public string LoadToken() {
			if (File.Exists(this._path)) {
				return File.ReadAllText(this._path);
			}
			return null;
		}

		public void SaveToken(string token) {
			File.WriteAllText(this._path, token, ENCODER);
		}

		public void DeleteToken() {
			if (File.Exists(this._path)) {
				File.Delete(this._path);
			}
		}
	}
}
