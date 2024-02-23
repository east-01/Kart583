using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
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
    
    private List<Scene> ScenesLoaded = new(); // Unused for now, see RegisterScenes

    void Awake() 
    {
        if(Instance != null)
            throw new InvalidOperationException("Tried to create a new SceneDelegate when one already exists.");

        Instance = this;
    }

    void OnEnable() { InstanceFinder.SceneManager.OnLoadEnd += RegisterScenes; }
    void OnDisable() { InstanceFinder.SceneManager.OnLoadEnd -= RegisterScenes; }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ServerRpcJoinScene(base.LocalConnection);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcJoinScene(NetworkConnection conn) 
    {
        // For now we're loading the players into the default loaded scene
        // More info here: https://fish-networking.gitbook.io/docs/manual/guides/scene-management/scene-visibility#initial-scene-load
        Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        base.SceneManager.AddConnectionToScene(conn, activeScene);
    }

    /// <summary>
    /// Unused code at the moment, will be used when we get more into depth of menus/lobbies
    /// </summary>
    /// <param name="args"></param>
    private void RegisterScenes(SceneLoadEndEventArgs args)
    {
        if(!args.QueueData.AsServer) 
            return;

        foreach(var scene in args.LoadedScenes) {
            ScenesLoaded.Add(scene);
            print("Registered scene " + scene.name);
        }
    }

}
