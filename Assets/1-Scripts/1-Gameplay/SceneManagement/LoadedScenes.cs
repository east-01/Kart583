using System.Collections.Generic;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps track of the loaded scenes on this instance.
/// </summary>
public class LoadedScenes : MonoBehaviour
{

    [SerializeField]
    private Dictionary<SceneLookupData, Scene> loadedScenes;

    private void OnEnable() 
    {
        // TODO: Subscribe from client/server scene load events
    }

    private void OnDisable() 
    {
        // TODO: Unsubscribe from client/server scene load events
    }

    public Scene GetScene(SceneLookupData sceneLookupData, bool allowNameOnlyLookup) 
    {
        // TODO: Grab from SceneDelegate class
    }

}

