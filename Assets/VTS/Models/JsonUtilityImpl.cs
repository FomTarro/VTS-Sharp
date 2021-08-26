namespace VTS.Models.Impl{
    public class JsonUtilityImpl : IJsonUtility
    {
        public T FromJson<T>(string json)
        {
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }

        public string ToJson(object obj)
        {
            return UnityEngine.JsonUtility.ToJson(obj);
        }
    }
}
