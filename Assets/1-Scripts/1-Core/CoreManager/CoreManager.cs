using System;
using AClockworkBerry;
using FishNet;
using FishNet.Managing;
using UnityEngine;

/// <summary>
/// The CoreManager should be placed in all scenes. It will spawn other essential managers.
/// </summary>
public class CoreManager : MonoBehaviour
{

    public static CoreManager Instance;
    public static TransitionManager TransitionManager { get { return Instance.transitionManager; } }

    [Header("Prefabs"), SerializeField] private GameObject networkManagerPrefab;
    [SerializeField] private GameObject sceneDelegatePrefab;
    [SerializeField] private GameObject playerObjectManagerPrefab;
    [SerializeField] private GameObject screenLoggerPrefab;

    [Header("Settings")] public bool isMultiplayer;

    private TransitionManager transitionManager;

    private bool notifiedOfRelease = false;

    private void Awake() 
    {
        if(Instance != null) {
            Destroy(gameObject);
            return;
        } else {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if(!notifiedOfRelease && !GameVersion.IsDevelopment) {
            Debug.Log($"<color=aqua>Running release build {GameVersion.Version}</color>");
            notifiedOfRelease = true;
        }

        transitionManager = GetComponent<TransitionManager>();

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

    /// <summary>
    /// Check if the running instance is a server instance. More reliable than InstanceFinder because 
    ///   it will handle cases where a NetworkManager doesn't exist.
    /// </summary>
    public bool IsServer { get {
        if(NetworkManager.Instances.Count == 0) return false;
        return InstanceFinder.IsServer;
    } }

}
