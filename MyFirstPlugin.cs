using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;
using VTS;

using UnityEngine;
public class MyFirstPlugin : VTSPlugin
{
    // Start is called before the first frame update
    void Start()
    {
        // Everything you need to get started!
        Initialize(
            new WebSocketSharpImpl(),
            new JsonUtilityImpl(),
            new TokenStorageImpl(),
            () => {Debug.Log("Connected!");},
            () => {Debug.LogWarning("Disconnected!");},
            () => {Debug.LogError("Error!");});
    }
}
