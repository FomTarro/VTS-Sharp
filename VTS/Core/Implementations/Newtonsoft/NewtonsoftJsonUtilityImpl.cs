using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace VTS {

	public class NewtonsoftJsonUtilityImpl : IJsonUtility {
        private StringEnumConverter _converter = new StringEnumConverter();

		public T FromJson<T>(string json) {
			return JsonConvert.DeserializeObject<T>(json, this._converter);
		}

		public string ToJson(object obj) {
			return JsonConvert.SerializeObject(obj, this._converter);
		}
	}
}