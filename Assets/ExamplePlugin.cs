namespace VTS{

    public class ExamplePlugin : VTSPlugin
    {
        protected override void Setup()
        {
            this._socket.onRecieve.AddListener((v) => { UnityEngine.Debug.Log(v); });
        }
    }
}
