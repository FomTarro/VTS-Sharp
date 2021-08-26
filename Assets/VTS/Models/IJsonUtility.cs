namespace VTS.Models{
    public interface IJsonUtility
    {
        T FromJson<T>(string json);
        string ToJson(object obj);
    }
}
