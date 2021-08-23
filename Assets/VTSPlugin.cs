using UnityEngine;
using VTS.Networking;

namespace VTS {
    [RequireComponent(typeof(VTSWebSocket))]
    public abstract class VTSPlugin : MonoBehaviour
    {
        [SerializeField]
        private string _pluginName = "ExamplePlugin";
        public string PluginName { get { return this._pluginName; } }
        [SerializeField]
        private string _pluginAuthor ="ExampleAuthor";
        public string PluginAuthor { get { return this._pluginAuthor; } }

        protected VTSWebSocket _socket = null;

        private void Awake(){
            this._socket = GetComponent<VTSWebSocket>();
            Setup();
            this._socket.onRecieve.AddListener( (v) => { Debug.Log(v); });
            this._socket.Connect();
            Authenticate();
        }

        private void Authenticate(){
            VTSData request = new VTSData();
            request.requestID = "authRequest";
            request.messageType = "AuthenticationTokenRequest";
            request.data.pluginName = this._pluginName;
            request.data.pluginDeveloper = this._pluginAuthor;
            _socket.Send(request);
        }

        protected abstract void Setup();

    }
}
