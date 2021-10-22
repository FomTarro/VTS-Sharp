using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;

using UnityEngine;
using UnityEngine.UI;

namespace VTS.Examples {

    public class ExamplePlugin : VTSPlugin
    {
        [SerializeField]
        private Text _text = null;

        [SerializeField]
        private Color _color = Color.black;

        [SerializeField]
        private bool _headRolling = false;

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

        public void PrintScreenColorData(){
            GetSceneColorOverlayInfo(
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void TintColor(){
            Models.ArtMeshMatcher matcher = new Models.ArtMeshMatcher();
            matcher.tintAll = true;
            TintArtMesh(
                _color,
                0.0f,
                matcher,
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void ToggleHeadRoll(){
            this._headRolling = !this._headRolling;
        }

        private void SyncValues(VTSParameterInjectionValue[] values){
            InjectParameterValues(
                values,
                (r) => { },
                (e) => { print(e.data.message); }
            );
	    }

        private void FixedUpdate(){
            if(this.IsAuthenticated && this._headRolling){
                float x = Mathf.Sin(Time.realtimeSinceStartup);
                float y = Mathf.Cos(Time.realtimeSinceStartup);
                SyncValues(new VTSParameterInjectionValue[] {
                    new VTSParameterInjectionValue { id = "FaceAngleX", value = x*20, weight = 1 },
                    new VTSParameterInjectionValue { id = "FaceAngleY", value = y*20, weight = 1 },
                    new VTSParameterInjectionValue { id = "FaceAngleZ", value = x*20, weight = 1 },
                    new VTSParameterInjectionValue { id = "EyeLeftX", value = x/2, weight = 1 },
                    new VTSParameterInjectionValue { id = "EyeLeftY", value = y/2, weight = 1 },
                    new VTSParameterInjectionValue { id = "EyeRightX", value = x/2, weight = 1 },
                    new VTSParameterInjectionValue { id = "EyeRightY", value = y/2, weight = 1 },
                });
            }
        }
    }
}
