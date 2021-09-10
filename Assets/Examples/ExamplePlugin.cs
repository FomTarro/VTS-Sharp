using VTS.Networking.Impl;
using VTS.Models.Impl;

using UnityEngine;
using UnityEngine.UI;

namespace VTS.Examples {

    public class ExamplePlugin : VTSPlugin
    {
        [SerializeField]
        private Text _text = null;

        [SerializeField]
        private Color _color = Color.black;

        private void Awake(){
            Initialize(new WebSocketImpl(), new JsonUtilityImpl(), new TokenStorageImpl(), 
            () => {},
            () => {}, 
            () => {});
        }

        public void PrintAPIStats(){
            GetStatistics(
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void PrintCurentModelHotkeys(){
            GetHotkeysInCurrentModel(
                null,
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void TintColor(){
            Models.ArtMeshMatcher matcher = new Models.ArtMeshMatcher();
            matcher.tintAll = true;
            TintArtMesh(
                _color,
                matcher,
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }
    }
}
