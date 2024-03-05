using System;
using FishNet;
using FishNet.Connection;
using UnityEngine;

public class SceneDelegateSpawner : MonoBehaviour
{
    [SerializeField] GameObject _sceneDelegatePrefab;

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
        InstanceFinder.ServerManager.Spawn(go);

        go.name = "SceneDelegate";
        go.GetComponent<SceneDelegate>().CheckInitialGlobalScene();
    }
}
