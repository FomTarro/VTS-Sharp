using System;
using System.Text.RegularExpressions;
using UnityEngine;
using VTS.Networking;
using VTS.Models;

namespace VTS {
    /// <summary>
    /// The base class for VTS plugin creation.
    /// 
    /// This implementation will attempt to Authenticate on Awake.
    /// </summary>
    [RequireComponent(typeof(VTSWebSocket))]
    public abstract class VTSPlugin : MonoBehaviour
    {
        [SerializeField]
        protected string _pluginName = "ExamplePlugin";
        /// <summary>
        /// The name of this plugin.
        /// </summary>
        /// <value></value>
        public string PluginName { get { return this._pluginName; } }
        [SerializeField]
        protected string _pluginAuthor = "ExampleAuthor";
        /// <summary>
        /// The name of this plugin's author. 
        /// </summary>
        /// <value></value>
        public string PluginAuthor { get { return this._pluginAuthor; } }

        private VTSWebSocket _socket = null;
        /// <summary>
        /// The underlying WebSocket for connecting to VTS.
        /// </summary>
        /// <value></value>
        protected VTSWebSocket Socket { get { return _socket; } }

        private string _token = null;
        /// <summary>
        /// The stored Authentication Token.
        /// </summary>
        /// <value></value>
        protected string AuthenticationToken { get { return _token; }}

        /// <summary>
        /// Authenticates the plugin as well as selects the Websocket and JSON utility implementations.
        /// </summary>
        /// <param name="webSocket">The websocket implementation.</param>
        /// <param name="jsonUtility">Thge JSON serializer/deserializer implementation.</param>
        public void Initialize(IWebSocket webSocket, IJsonUtility jsonUtility){
            this._socket = GetComponent<VTSWebSocket>();
            this._socket.Initialize(webSocket, jsonUtility);
            // TODO: clean this way the hell up.
            this._socket.Connect(() => {
                Authenticate(
                    (r) => { Debug.Log(r); }, 
                    (r) => { Debug.LogError(r); }
                );
            },
            () => { 
                Debug.LogError("Unable to connect ");
            });
        }

        private void Authenticate(Action<VTSAuthData> onSuccess, Action<VTSErrorData> onError){
            VTSAuthData tokenRequest = new VTSAuthData();
            tokenRequest.data.pluginName = this._pluginName;
            tokenRequest.data.pluginDeveloper = this._pluginAuthor;
            this._socket.Send<VTSAuthData>(tokenRequest, (a) => { 
                this._token = a.data.authenticationToken; 
                VTSAuthData authRequest = new VTSAuthData();
                authRequest.messageType = "AuthenticationRequest";
                authRequest.data.pluginName = this._pluginName;
                authRequest.data.pluginDeveloper = this._pluginAuthor;
                authRequest.data.authenticationToken = this._token;
                this._socket.Send<VTSAuthData>(authRequest, onSuccess, onError);
            }, onError);
        }

        /// <summary>
        /// Gets the current state of the VTS API.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#status">https://github.com/DenchiSoft/VTubeStudio#status</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetAPIState(Action<VTSStateData> onSuccess, Action<VTSErrorData> onError){
            VTSStateData request = new VTSStateData();
            request.messageType = "nonsense";
            this._socket.Send<VTSStateData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets current metrics about the VTS application.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics">https://github.com/DenchiSoft/VTubeStudio#getting-current-vts-statistics</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetStatistics(Action<VTSStatisticsData> onSuccess, Action<VTSErrorData> onError){
            VTSStatisticsData request = new VTSStatisticsData();
            this._socket.Send<VTSStatisticsData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the list of VTS folders.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders">https://github.com/DenchiSoft/VTubeStudio#getting-list-of-vts-folders</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetFolderInfo(Action<VTSFolderInfoData> onSuccess, Action<VTSErrorData> onError){
            VTSFolderInfoData request = new VTSFolderInfoData();
            this._socket.Send<VTSFolderInfoData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets information about the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model">https://github.com/DenchiSoft/VTubeStudio#getting-the-currently-loaded-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetCurrentModel(Action<VTSCurrentModelData> onSuccess, Action<VTSErrorData> onError){
            VTSCurrentModelData request = new VTSCurrentModelData();
            Debug.Log(request);
            this._socket.Send<VTSCurrentModelData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the list of all available VTS models.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models">https://github.com/DenchiSoft/VTubeStudio#getting-a-list-of-available-vts-models</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetAvailableModels(Action<VTSAvailableModelsData> onSuccess, Action<VTSErrorData> onError){
            VTSAvailableModelsData request = new VTSAvailableModelsData();
            this._socket.Send<VTSAvailableModelsData>(request, onSuccess, onError);
        }
        
        /// <summary>
        /// Loads a VTS model by its Model ID. Will return an error if the model cannot be loaded.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id">https://github.com/DenchiSoft/VTubeStudio#loading-a-vts-model-by-its-id</a>
        /// </summary>
        /// <param name="modelID">The Model ID/Name.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void LoadModel(string modelID, Action<VTSModelLoadData> onSuccess, Action<VTSErrorData> onError){
            VTSModelLoadData request = new VTSModelLoadData();
            request.data.modelID = modelID;
            this._socket.Send<VTSModelLoadData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Moves the currently loaded VTS model.
        /// 
        /// For more info, particularly about what each position value field does, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model">https://github.com/DenchiSoft/VTubeStudio#moving-the-currently-loaded-vts-model</a>
        /// </summary>
        /// <param name="position">The desired position information. Fields will be null-valued by default.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void MoveModel(VTSMoveModelData.Data position, Action<VTSMoveModelData> onSuccess, Action<VTSErrorData> onError){
            VTSMoveModelData request = new VTSMoveModelData();
            request.data = position;
            this._socket.Send<VTSMoveModelData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of available hotkeys.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-hotkeys-available-in-current-or-other-vts-model</a>
        /// </summary>
        /// <param name="modelID">Optional, the model ID to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetHotkeysInCurrentModel(string modelID, Action<VTSHotkeysInCurrentModelData> onSuccess, Action<VTSErrorData> onError){
            VTSHotkeysInCurrentModelData request = new VTSHotkeysInCurrentModelData();
            request.data.modelID = modelID;
            this._socket.Send<VTSHotkeysInCurrentModelData>(request, onSuccess, onError);
        }
        
        /// <summary>
        /// Triggers a given hotkey.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys">https://github.com/DenchiSoft/VTubeStudio#requesting-execution-of-hotkeys</a>
        /// </summary>
        /// <param name="hotkeyID">The model ID to get hotkeys for.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void TriggerHotkey(string hotkeyID, Action<VTSHotkeyTriggerData> onSuccess, Action<VTSErrorData> onError){
            VTSHotkeyTriggerData request = new VTSHotkeyTriggerData();
            request.data.hotkeyID = hotkeyID;
            this._socket.Send<VTSHotkeyTriggerData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of all available art meshes in the current VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-artmeshes-in-current-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetArtMeshList(Action<VTSArtMeshListData> onSuccess, Action<VTSErrorData> onError){
            VTSArtMeshListData request = new VTSArtMeshListData();
            this._socket.Send<VTSArtMeshListData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Tints matched components of the current art mesh.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color">https://github.com/DenchiSoft/VTubeStudio#tint-artmeshes-with-color</a>
        /// </summary>
        /// <param name="tint">The tint to be applied.</param>
        /// <param name="matcher">The ArtMesh matcher search parameters.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void TintArtMesh(Color32 tint, ArtMeshMatcher matcher, Action<VTSColorTintData> onSuccess, Action<VTSErrorData> onError){
            VTSColorTintData request = new VTSColorTintData();
            ColorTint colorTint = new ColorTint();
            colorTint.colorA = tint.a;
            colorTint.colorB = tint.b;
            colorTint.colorG = tint.g;
            colorTint.colorR = tint.r;
            request.data.colorTint = colorTint;
            request.data.artMeshMatcher = matcher;
            this._socket.Send<VTSColorTintData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Checks to see if a face is being tracked.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker">https://github.com/DenchiSoft/VTubeStudio#checking-if-face-is-currently-found-by-tracker</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetFaceFound(Action<VTSFaceFoundData> onSuccess, Action<VTSErrorData> onError){
            VTSFaceFoundData request = new VTSFaceFoundData();
            this._socket.Send<VTSFaceFoundData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of input parameters for the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetInputParameterList(Action<VTSInputParameterListData> onSuccess, Action<VTSErrorData> onError){
            VTSInputParameterListData request = new VTSInputParameterListData();
            this._socket.Send<VTSInputParameterListData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets the value fr the specified parameter.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters">https://github.com/DenchiSoft/VTubeStudio#requesting-list-of-available-tracking-parameters</a>
        /// </summary>
        /// <param name="parameterName">The name of the parameter to get the value of.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetParameterValue(string parameterName, Action<VTSParameterValueData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterValueData request = new VTSParameterValueData();
            request.data.name = parameterName;
            this._socket.Send<VTSParameterValueData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Gets a list of input parameters for the currently loaded Live2D model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model">https://github.com/DenchiSoft/VTubeStudio#get-the-value-for-all-live2d-parameters-in-the-current-model</a>
        /// </summary>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void GetLive2DParameterList(Action<VTSLive2DParameterListData> onSuccess, Action<VTSErrorData> onError){
            VTSLive2DParameterListData request = new VTSLive2DParameterListData();
            this._socket.Send<VTSLive2DParameterListData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Adds a custom parameter to the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#adding-new-tracking-parameters-custom-parameters</a>
        /// </summary>
        /// <param name="parameter">Information about the parameter to add. Parameter name must be 4-32 characters, alphanumeric.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void AddCustomParameter(VTSCustomParameter parameter, Action<VTSParameterCreationData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterCreationData request = new VTSParameterCreationData();
            request.data.parameterName = SanatizeParameterName(parameter.parameterName);
            request.data.explanation = parameter.explanation;
            request.data.min = parameter.min;
            request.data.max = parameter.max;
            request.data.defaultValue = parameter.defaultValue;
            this._socket.Send<VTSParameterCreationData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Removes a custom parameter from the currently loaded VTS model.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#delete-custom-parameters</a>
        /// </summary>
        /// <param name="parameterName">The name f the parameter to remove.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void RemoveCustomParameter(string parameterName, Action<VTSParameterDeletionData> onSuccess, Action<VTSErrorData> onError){
            VTSParameterDeletionData request = new VTSParameterDeletionData();
            request.data.parameterName = SanatizeParameterName(parameterName);
            this._socket.Send<VTSParameterDeletionData>(request, onSuccess, onError);
        }

        /// <summary>
        /// Sends a list of parameter names and corresponding values to assign to them.
        /// 
        /// For more info, see 
        /// <a href="https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters">https://github.com/DenchiSoft/VTubeStudio#feeding-in-data-for-default-or-custom-parameters</a>
        /// </summary>
        /// <param name="values">A listo of parameters and the values to assign to them.</param>
        /// <param name="onSuccess">Callback executed upon receiving a response.</param>
        /// <param name="onError">Callback executed upon receiving an error.</param>
        public void InjectParameterValues(VTSParameterInjectionValue[] values, Action<VTSInjectParameterData> onSuccess, Action<VTSErrorData> onError){
            VTSInjectParameterData request = new VTSInjectParameterData();
            foreach(VTSParameterInjectionValue value in values){
                value.id = SanatizeParameterName(value.id);
            }
            request.data.parameterValues = values;
            this._socket.Send<VTSInjectParameterData>(request, onSuccess, onError);
        }

        private static Regex ALPHANUMERIC = new Regex(@"\W|_");
        private string SanatizeParameterName(string name){
            // between 4 and 32 chars, alphanumeric
            string output = name;
            output = ALPHANUMERIC.Replace(output, "");
            output.PadLeft(4, 'X');
            output = output.Substring(0, Math.Min(output.Length, 31));
            return output;
        }
        
    }
}
