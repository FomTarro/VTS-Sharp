namespace VTS.Networking {
    public interface ITokenStorage{
        string LoadToken();
        void SaveToken(string token);
    }
}
