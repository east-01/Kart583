using System;
using AClockworkBerry;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
public class CoreManager : MonoBehaviour
{

    public static CoreManager Instance;

    [Header("Prefabs"), SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject sceneDelegatePrefab;
    [SerializeField] private GameObject playerObjectManagerPrefab;
    [SerializeField] private GameObject screenLoggerPrefab;

    [Header("Settings")] public bool isMultiplayer;

    private void Awake() 
    {
        if(Instance != null) {
            Debug.Log("CoreManager already spawned. Deleting self.");
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        CheckNetworkManager();
        CheckPlayerObjectManager();
        CheckScreenLogger();

    }

    private void Update() 
    {
        CheckNetworkManager();
        CheckSceneDelegate();
    }

    private void CheckNetworkManager() 
    {
        if(NetworkManager.Instances.Count > 0)
            return;
        
        Instantiate(networkManagerPrefab);
    }

    private void CheckSceneDelegate() 
    {
        if(sceneDelegatePrefab == null) {
            Debug.LogWarning("Scene delegate prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        if(SceneDelegate.Instance != null)
            return;
        // Check if there's a NetworkManager in place and the server is started
        if(NetworkManager.Instances.Count <= 0 || InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started || !InstanceFinder.IsServer)
            return;

        GameObject go = Instantiate(sceneDelegatePrefab);
        InstanceFinder.ServerManager.Spawn(go);

        go.name = "SceneDelegate";
        go.GetComponent<SceneDelegate>().CheckInitialGlobalScene();
    }

    private void CheckPlayerObjectManager() 
    {
        if(PlayerObjectManager.Instance != null)
            return;

        Instantiate(playerObjectManagerPrefab);
    }

    private void CheckScreenLogger() 
    {
        if(ScreenLogger.Instance != null)
            return;

        Instantiate(screenLoggerPrefab);
    }

}
