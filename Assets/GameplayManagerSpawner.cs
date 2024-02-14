using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

public class GameplayManagerSpawner : MonoBehaviour
{
    public GameObject gameplayManagerPrefab;

    void Awake() 
    {
        print("registered event");
        InstanceFinder.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if(!asServer)
            return;
        if(gameplayManagerPrefab == null) {
            Debug.LogWarning("Gameplay manager prefab is null on GameplayManagerSpawner script on object " + gameObject.name);
            return;
        }

        GameObject gmo = Instantiate(gameplayManagerPrefab);
        InstanceFinder.ServerManager.Spawn(gmo);
        print("Spawned");
    }

}
