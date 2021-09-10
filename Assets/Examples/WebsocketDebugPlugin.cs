using UnityEngine;
using VTS;
using VTS.Models;
using VTS.Networking.Impl;
using VTS.Models.Impl;

public class WebsocketDebugPlugin : VTSPlugin
{
    private bool _authenticated = false;
    private void Awake(){
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Initialize(new WebSocketImpl(), new JsonUtilityImpl(), new TokenStorageImpl(), 
        () => { 
            this._authenticated = true; 
        }, 
        () => {
            this._authenticated = false; 
        },
        () => {});
    }
    public void SyncValues(VTSParameterInjectionValue[] values)
	{
		InjectParameterValues(
			values,
			(r) => { },
			(e) => { print(e.data.message); }
		);
	}

    private void FixedUpdate()
	{
        if(this._authenticated){
            var x = Mathf.Sin(Time.realtimeSinceStartup);
            var y = Mathf.Cos(Time.realtimeSinceStartup);
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
