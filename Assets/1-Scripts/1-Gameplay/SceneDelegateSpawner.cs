using System;
using FishNet;
using FishNet.Connection;
using UnityEngine;

public class SceneDelegateSpawner : MonoBehaviour
{

    [SerializeField] GameObject _sceneDelegatePrefab;

    void Awake() 
    {
        InstanceFinder.SceneManager.OnQueueStart += SceneManager_OnQueueStart;
    }

    void Update() 
    {
        if(!InstanceFinder.ServerManager.Started)
            return;
        if(!InstanceFinder.IsServer)
            return;
        if(_sceneDelegatePrefab == null) {
            Debug.LogWarning("Scene delegate prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        if(SceneDelegate.Instance != null)
            return;

        GameObject go = Instantiate(_sceneDelegatePrefab);
        go.name = "SceneDelegate";
        InstanceFinder.ServerManager.Spawn(go);
        print("SPAWNED SCENEDELEGATE");
    }

    private void SceneManager_OnQueueStart()
    {
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        // if(!asServer)
        //     return;
        // if(_sceneDelegatePrefab == null) {
        //     Debug.LogWarning("Scene delegate prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
        //     return;
        // }
        // if(SceneDelegate.Instance != null)
        //     return;

        // GameObject go = Instantiate(_sceneDelegatePrefab);
        // go.name = "SceneDelegate";
        // InstanceFinder.ServerManager.Spawn(go);
    }

}
