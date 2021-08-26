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
            GetStatistics(
                (r) => { Debug.Log(r); _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void PrintCurentModelHotkeys(){
            GetHotkeysInCurrentModel(
                null,
                (r) => { Debug.Log(r); _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }
    }
}
