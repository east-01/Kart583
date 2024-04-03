using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps track of the loaded scenes on this instance.
/// </summary>
[RequireComponent(typeof(SceneDelegate))]
public class LoadedScenes : NetworkBehaviour
{

    private SceneDelegate sceneDelegate;
    [SerializeField]
    private Dictionary<SceneLookupData, Scene> loadedScenes;

    private void OnEnable() 
    { 
        this.sceneDelegate = GetComponent<SceneDelegate>();

        base.SceneManager.OnLoadEnd += FishSceneManager_SceneLoaded; 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += UnitySceneManager_SceneLoaded;
    }

    private void OnDisable() { 
        if(base.SceneManager != null)
            base.SceneManager.OnLoadEnd -= FishSceneManager_SceneLoaded; 
    }

    private void UnitySceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode) 
    {
        // We don't care about the servers UnityEngine SceneManager. That's for RegisterScenes.
        if(!base.IsClient)
            return;
        if(scene != null && loadTarget != null && scene.name != loadTarget.Name) {
            Debug.LogWarning("Scene load didn't match load target.");
            return;
        }

        // Scene loaded, add to loadedScenes Dictionary. Also done in RegisterScenes for server
        loadedScenes.Add(new(scene.handle, scene.name), scene);

        NetworkConnection client = base.LocalConnection;
        SceneDelegate.SceneDelegateDebug("LoadedScenes#UnitySceneManager_SceneLoaded: Validated client loaded, scene. Disconnecting them from their other scenes.");
        foreach(Scene otherScene in client.Scenes) {
            if(otherScene != scene) {
                SceneDelegate.SceneDelegateDebug($"LoadedScenes#SceneManager_SceneLoaded: Clearing other scene {otherScene.name}/{otherScene.handle} from client");
                base.SceneManager.RemoveConnectionsFromScene(new NetworkConnection[] { client }, otherScene);
            }
        }

        ServerRpcClientLoadedScene(base.LocalConnection, loadTarget);

        SceneDelegate.SceneDelegateDebug($"SceneDelegate#UnitySceneManager_SceneLoaded: Client loaded scene \"{scene.name}\"");
    }

    private void FishSceneManager_SceneLoaded(SceneLoadEndEventArgs args)
    {

        LevelAtlas la = _atlasPrefab.GetComponent<LevelAtlas>();
        foreach(Scene scene in args.LoadedScenes) {
            SceneDelegate.SceneDelegateDebug($"{(base.IsServer ? "Server" : "Client")} loaded scene " + scene.name + ", handle: " + scene.handle);

            // Find which lobby is expecing this scene
            SceneLookupData sceneLookupData = new(scene.handle, scene.name);
            SceneLookupData sceneLookupDataNoHandle = new(0, scene.name); // Check for handleless lookup
            if(!expectingScene.ContainsKey(sceneLookupData) && !expectingScene.ContainsKey(sceneLookupDataNoHandle)) {
                if(scene.name != SceneNames.MENU_SERVER)
                    Debug.LogWarning($"Scene \"{scene.name}\" was loaded without any lobby expecting it."); // Only send this warning message for scenes other than the server dash
                continue;
            }

            GameLobby expectingLobby = null;
            if(expectingScene.ContainsKey(sceneLookupData))
                expectingLobby = expectingScene[sceneLookupData];
            else if(expectingScene.ContainsKey(sceneLookupDataNoHandle))
                expectingLobby = expectingScene[sceneLookupDataNoHandle];
            else
                throw new InvalidOperationException("Shouldn't be able to reach this");

            expectingLobby.RegisterLoadedScene(sceneLookupData, scene);

            // Scene loaded, add it to loadedScenes dictionary, also done in SceneManager_SceneLoaded
            loadedScenes.Add(sceneLookupData, scene);
        }

        // Disable event systems
        if(base.IsServer) {
            int disabledEventSystems = 0;
            foreach(EventSystem system in FindObjectsOfType<EventSystem>()) {
                system.enabled = false;
                disabledEventSystems++;
            }
            print($"Disabled {disabledEventSystems} event system(s).");
        }

        if(args.SkippedSceneNames.Length > 0)
            SceneDelegate.SceneDelegateDebug($"RegisterScenes skipped {args.SkippedSceneNames.Length} scene(s).");
    }

    public Scene GetScene(SceneLookupData sceneLookupData, bool allowNameOnlyLookup) 
    {
        // TODO: Grab from SceneDelegate class
        return null;
    }

}

