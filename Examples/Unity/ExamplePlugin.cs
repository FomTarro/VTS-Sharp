using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using VTS.Core;

namespace VTS.Unity.Examples {

	public class ExamplePlugin : UnityVTSPlugin {
		[SerializeField]
		private Text _text = null;

		[SerializeField]
		private Text _eventText = null;

		[SerializeField]
		private Color _color = Color.black;

		[SerializeField]
		private bool _headRolling = false;

		[SerializeField]
		private Image _connectionLight = null;
		[SerializeField]
		private Text _connectionText = null;


		private void Awake() {
			Connect();
		}

		public void Connect() {
			this._connectionLight.color = Color.yellow;
			this._connectionText.text = "Connecting...";
			Initialize(new WebSocketSharpImpl(this.Logger), new NewtonsoftJsonUtilityImpl(), new TokenStorageImpl(Application.persistentDataPath),
			() => {
				this.Logger.Log("Connected!");
				this._connectionLight.color = Color.green;
				this._connectionText.text = "Connected!";
			},
			() => {
				this.Logger.LogWarning("Disconnected!");
				this._connectionLight.color = Color.gray;
				this._connectionText.text = "Disconnected.";
			},
			(error) => {
				this.Logger.LogError("Error! - " + error.data.message);
				this._connectionLight.color = Color.red;
				this._connectionText.text = "Error!";
			});
		}

		public void PrintAPIStats() {
			GetStatistics(
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; }
			);
		}

		public void PrintCurentModelHotkeys() {
			GetHotkeysInCurrentModel(
				null,
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; }
			);
		}

		public void PrintScreenColorData() {
			GetSceneColorOverlayInfo(
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; }
			);
		}

		public void PrintPostProcessingEffects() {
			GetPostProcessingEffectStateList(
				true, true, new Effects[0],
				(r) => {
					Debug.Log(this.JsonUtility.ToJson(r));
					_text.text = this.JsonUtility.ToJson(r);
				},
				(e) => { _text.text = e.data.message; }
			);
		}

		public void TintColor() {
			ArtMeshMatcher matcher = new ArtMeshMatcher();
			matcher.tintAll = true;
			TintArtMesh(
				_color,
				0.0f,
				matcher,
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; }
			);
		}

		public void AdjustAnalogGlitch(float f) {
			VTSPostProcessingUpdateOptions opts = new VTSPostProcessingUpdateOptions();
			opts.postProcessingOn = true;
			opts.setPostProcessingValues = true;
			PostProcessingValue value = new PostProcessingValue(EffectConfigs.AnalogGlitch_Strength, f);
			PostProcessingValue value2 = new PostProcessingValue(EffectConfigs.AnalogGlitch_ScanlineJitter, f);
			PostProcessingValue[] values = (new[] { value, value2 });
			SetPostProcessingEffectValues(opts, values,
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; });
		}

		public void ToggleHeadRoll() {
			this._headRolling = !this._headRolling;
		}

		public void ActivateExpression(string expressionName) {
			GetExpressionStateList(
				(r) => {
					_text.text = this.JsonUtility.ToJson(r);
					ExpressionData expression = new List<ExpressionData>(r.data.expressions).Find((e) => { return e.file.ToLower().Contains(expressionName.ToLower()); });
					if (expression != null) {
						SetExpressionState(expression.file, true,
							(x) => { _text.text = this.JsonUtility.ToJson(x); },
							(e2) => { _text.text = e2.data.message; });
					} else {
						throw new System.Exception("No Expression with " + expressionName + " in the file name was found.");
					}
				},
				(e) => { _text.text = e.data.message; }
			);
		}

		public void GetPhysicsData() {
			GetCurrentModelPhysics(
				(r) => { _text.text = this.JsonUtility.ToJson(r); },
				(e) => { _text.text = e.data.message; }
			);
		}

		public void GetArtMeshes() {
			this.RequestArtMeshSelection("", "", 2, new List<string>(),
			(s) => {
				this._text.text = this.JsonUtility.ToJson(s);
			},
			(e) => {
				this._text.text = this.JsonUtility.ToJson(e);
			});
		}

		public void SubTestEvent() {
			VTSTestEventConfigOptions config = new VTSTestEventConfigOptions("ECHO!");
			this.SubscribeToTestEvent(
				config,
				(s) => { _eventText.text = string.Format("{0} - {1}", s.data.counter, s.data.yourTestMessage); },
				DoNothingCallback,
				(e) => { _eventText.text = e.data.message; });
		}

		public void UnsubTestEvent() {
			this.UnsubscribeFromTestEvent(
				(s) => { _eventText.text = "[Event Output]"; },
				(e) => { _eventText.text = e.data.message; });
		}

		public void SubOutlineEvent() {
			VTSModelOutlineEventConfigOptions config = new VTSModelOutlineEventConfigOptions(true);
			this.SubscribeToModelOutlineEvent(
				config,
				(s) => { _eventText.text = string.Format("Model center: ({0}, {1})", s.data.convexHullCenter.x, s.data.convexHullCenter.y); },
				DoNothingCallback,
				(e) => { _eventText.text = e.data.message; });
		}

		public void UnsubOutlineEvent() {
			this.UnsubscribeFromModelOutlineEvent(
				(s) => { _eventText.text = "[Event Output]"; },
				(e) => { _eventText.text = e.data.message; });
		}

		public void SubAnimationEvent() {
			VTSModelAnimationEventConfigOptions config = new VTSModelAnimationEventConfigOptions();
			this.SubscribeToModelAnimationEvent(
				config,
				(s) => { _eventText.text = string.Format(s.data.animationEventType + " "); },
				(g) => { this.Logger.Log("Subscribed!"); },
				(e) => { _eventText.text = e.data.message; });
		}

		public void UnsubAnimationEvent() {
			this.UnsubscribeFromModelAnimationEvent(
				(s) => { _eventText.text = "[Event Output]"; },
				(e) => { _eventText.text = e.data.message; });
		}


		private void SyncValues(VTSParameterInjectionValue[] values) {
			InjectParameterValues(
				values,
				VTSInjectParameterMode.ADD,
				(r) => { },
				(e) => { this.Logger.LogError(e.data.message); }
			);
		}

		private void FixedUpdate() {

			if (this.IsAuthenticated && this._headRolling) {
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
