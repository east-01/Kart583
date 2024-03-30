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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
