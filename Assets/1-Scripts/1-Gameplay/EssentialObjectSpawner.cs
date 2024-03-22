using System;
using FishNet;
using FishNet.Connection;
using UnityEngine;

public class EssentialObjectSpawner : MonoBehaviour
{
    [SerializeField] 
    private GameObject sceneDelegatePrefab;
    [SerializeField]
    private GameObject playerObjectManagerPrefab;

    void Awake() 
    {
        if(PlayerObjectManager.Instance == null)
            Instantiate(playerObjectManagerPrefab);
    }

    void Update() 
    {
        CheckSceneDelegate();
    }

    void CheckSceneDelegate() 
    {
        if(InstanceFinder.ServerManager == null)
            return;
        if(!InstanceFinder.ServerManager.Started)
            return;
        if(!InstanceFinder.IsServer)
            return;
        if(sceneDelegatePrefab == null) {
            Debug.LogWarning("Scene delegate prefab is null on SceneDelegateSpawner script on object " + gameObject.name);
            return;
        }
        if(SceneDelegate.Instance != null)
            return;

        GameObject go = Instantiate(sceneDelegatePrefab);
        InstanceFinder.ServerManager.Spawn(go);

        go.name = "SceneDelegate";
        go.GetComponent<SceneDelegate>().CheckInitialGlobalScene();
    }

    void CheckPlayerObject() 
    {

    }

}
