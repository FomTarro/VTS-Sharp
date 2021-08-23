using UnityEngine;
using VTS.Networking.Impl;

namespace VTS.Networking{
    public class VTSWebSocket : MonoBehaviour
    {
        private string VTS_WS_URL = "ws://localhost:8001";
        private UnityWebSocket _ws = null;

        public VTSDataEvent onRecieve = new VTSDataEvent();
        // TODO: Implement these
        public VTSDataEvent onOpen = new VTSDataEvent();
        public VTSDataEvent onClose = new VTSDataEvent();

        private void Update(){
            if(_ws != null && _ws.RecieveQueue.Count > 0){
                string data;
                _ws.RecieveQueue.TryDequeue(out data);
                if(data != null){
                    VTSData response = JsonUtility.FromJson<VTSData>(data);
                    onRecieve.Invoke(response);
                }
            }
        }

        public void Connect(){
            this._ws = new UnityWebSocket(VTS_WS_URL);
            #pragma warning disable 
            this._ws.Connect();
            #pragma warning restore
        }

        public void Send(VTSData request){
            if(this._ws != null){
                this._ws.Send(JsonUtility.ToJson(request));
            }
        }
    }
}
