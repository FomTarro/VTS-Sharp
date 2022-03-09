﻿using System.Collections.Generic;

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

        [SerializeField]
        private Button _portConnectButtonPrefab = null;

        [SerializeField]
        private RectTransform _portConnectButtonParent = null;

        [SerializeField]
        private Image _connectionLight = null;
        [SerializeField]
        private Text _connectionText = null;

        private void Awake(){
            Connect();
        }

        public void Connect(){
            this._connectionLight.color = Color.yellow;
            this._connectionText.text = "Connecting...";
            Initialize(new WebSocketSharpImpl(), new JsonUtilityImpl(), new TokenStorageImpl(), 
            () => {
                Debug.Log("Connected!");
                this._connectionLight.color = Color.green;
                this._connectionText.text = "Connected!";
            },
            () => {
                Debug.LogWarning("Disconnected!");
                this._connectionLight.color = Color.gray;
                this._connectionText.text = "Disconnected.";
            },
            () => {
                Debug.LogError("Error!");
                this._connectionLight.color = Color.red;
                this._connectionText.text = "Error!";
            });
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

        public void ActivateExpression(string expressionName){
            GetExpressionStateList(
                (r) => { 
                    _text.text = new JsonUtilityImpl().ToJson(r); 
                    ExpressionData expression = new List<ExpressionData>(r.data.expressions).Find((e) => { return e.file.ToLower().Contains(expressionName.ToLower()); });
                    if(expression != null){
                        SetExpressionState(expression.file ,true, 
                            (x) => { _text.text = new JsonUtilityImpl().ToJson(x); }, 
                            (e2) => { _text.text = e2.data.message; });
                    }else{
                        throw new System.Exception("No Expression with " + expressionName + " in the file name was found.");
                    }
                }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        public void GetPhysicsData(){
            GetCurrentModelPhysics(
                (r) => { _text.text = new JsonUtilityImpl().ToJson(r); }, 
                (e) => { _text.text = e.data.message; }
            );
        }

        private void SyncValues(VTSParameterInjectionValue[] values){
            InjectParameterValues(
                values,
                (r) => { },
                (e) => { print(e.data.message); }
            );
	    }

        public void RefreshPortList(){
            List<int> ports = new List<int>(GetPorts().Keys);
            foreach(Transform child in this._portConnectButtonParent){
                Destroy(child.gameObject);
            }
            foreach(int port in ports){
                Button button = Instantiate<Button>(this._portConnectButtonPrefab, Vector3.zero, Quaternion.identity, this._portConnectButtonParent);
                button.name = port.ToString();
                button.GetComponentInChildren<Text>().text = button.name;
                button.onClick.AddListener(() => {
                    if(SetPort(int.Parse(button.name))){
                        Connect();
                    }
                });
            }
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
