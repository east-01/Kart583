using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The scene delegate is a global networked object that will handle clients being placed
///   into scenes. The current setup works like this:
/// Server-side scene delegate:
///   - Spawns scenes
///   - Recieves client requests to be placed into scenes
/// Client-side scene delegate:
///   - On start, makes a request to join a scene
/// </summary>
public class SceneDelegate : NetworkBehaviour
{

    public static SceneDelegate Instance;
    
    private List<Scene> ScenesLoaded = new();

    void Awake() 
    {
        if(Instance != null)
            throw new InvalidOperationException("Tried to create a new SceneDelegate when one already exists.");

        Instance = this;
    }

    void OnEnable() { InstanceFinder.SceneManager.OnLoadEnd += RegisterScenes; }
    void OnDisable() { InstanceFinder.SceneManager.OnLoadEnd -= RegisterScenes; }

    private void RegisterScenes(SceneLoadEndEventArgs args)
    {
        if(!args.QueueData.AsServer) 
            return;

        foreach(var scene in args.LoadedScenes) {
            ScenesLoaded.Add(scene);
            print("Registered scene " + scene.name);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Starting client, looking for scene to join
        ServerRpcJoinScene(base.LocalConnection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcJoinScene(NetworkConnection conn) 
    {
        // Not sure what scene to put them in yet. Lets just put them in the first one that was loaded.
        base.SceneManager.AddConnectionToScene(conn, ScenesLoaded[0]);
    }

}
