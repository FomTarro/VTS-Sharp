using VTS.Networking.Impl;
using VTS.Models.Impl;

using UnityEngine;
using UnityEngine.UI;

namespace VTS{

    public class ExamplePlugin : VTSPlugin
    {
        [SerializeField]
        private Text _text = null;

        private void Awake(){
            Initialize(new WebSocketImpl(), new JsonUtilityImpl());
        }

        public void PrintAPIStats(){
            GetAPIState((r) => { Debug.Log(r); _text.text = new JsonUtilityImpl().ToJson(r); }, (r) => { Debug.LogError(r.data.message); });
        }
    }
}
