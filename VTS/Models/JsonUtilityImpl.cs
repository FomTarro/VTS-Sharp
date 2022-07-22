namespace VTS.Models.Impl{  
    public class JsonUtilityImpl : IJsonUtility
    {
        public T FromJson<T>(string json)
        {
            if(IsMessageType(json, "HotkeysInCurrentModelResponse")){
                json = ReplaceStringWithEnum<HotkeyAction>(json, "type");
            }else if(IsMessageType(json, "APIError")){
                json = ReplaceStringWithEnum<ErrorID>(json, "type");
            }
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }

        public string ToJson(object obj)
        {
            string json = UnityEngine.JsonUtility.ToJson(obj);
            return RemoveNullProps(json);
        }

        private bool IsMessageType(string json, string messageType)
        {
            return json.Contains(string.Format("\"messageType\":\"{0}\"", messageType));
        }

        /// <summary>
        /// Helper function to replace enum names with underyling values.
        /// </summary>
        /// <param name="json">json to inspect</param>
        /// <param name="fieldName">Field name to inspect</param>
        /// <typeparam name="T">Enum type to replace</typeparam>
        /// <returns>The modified json</returns>
        private string ReplaceStringWithEnum<T>(string json, string fieldName) where T : System.Enum
        {
            System.Type underlyingType = System.Enum.GetUnderlyingType(typeof(T));
            foreach(T entry in System.Enum.GetValues(typeof(T))){
                object value = System.Convert.ChangeType(entry, underlyingType);
                string name = System.Enum.GetName(typeof(T), entry);
                json = json.Replace(
                    string.Format("\"{0}\":\"{1}\"", fieldName, name), 
                    string.Format("\"{0}\":\"{1}\"", fieldName, value));
            }
            return json;
        }

        private string RemoveNullProps(string input){
            string[] props = input.Split(',', '{', '}');
            string output = input;
            foreach(string prop in props){
                // We're doing direct string manipulation on a serialized JSON, which is incredibly frail.
                // Please forgive my sins, as Unity's builtin JSON tool uses default field values instead of nulls,
                // and sometimes that is unacceptable behavior.
                // I'd use a more robust JSON library if I wasn't publishing this as a plugin.
                string[] pair = prop.Split(':');
                if(pair.Length > 1){
                    float nullable = 0.0f;
                    float.TryParse(pair[1], out nullable);
                    if("NaN".Equals(pair[1]) || float.MinValue.Equals(nullable) || int.MinValue.Equals(nullable)){
                        output = output.Replace(prop+",", "");
                        output = output.Replace(prop, "");
                    }
                    else if("\"\"".Equals(pair[1])){
                        output = output.Replace(prop+",", "");
                        output = output.Replace(prop, "");
                    }
                }
            }
            output = output.Replace(",}", "}");
            return output;
        }
    }
}
