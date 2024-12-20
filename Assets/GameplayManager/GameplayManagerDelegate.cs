using System.Collections;
using System.Collections.Generic;
using EMullen.SceneMgmt;
using FishNet.Managing.Scened;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Is responsible for finding GameplayManagers in a scene and distributing
///   said GameplayManagers to GameplayManagerBehaviours that request them.
/// </summary>
public class GameplayManagerDelegate : MonoBehaviour
{

    private List<GameplayManagerBehavior> waitingForGameplayManagers = new();
    private static Dictionary<SceneLookupData, GameplayManager> gameplayManagers;

    private void Update() 
    {
        if(waitingForGameplayManagers.Count == 0)
            return;
        // A copy of the list to iterate through so we can remove elements without throwing errors
        List<GameplayManagerBehavior> listCopy = new(waitingForGameplayManagers);
        listCopy.ForEach(gmb => LoadGameplayManager(gmb, out bool loadStatus));
    }

    public static GameplayManager GetGameplayManager(SceneLookupData lookupData) {

        // Create dictionary if its null
        gameplayManagers ??= new();

        // Locate and store the GameplayManager if it isn't stored.
        if(!gameplayManagers.ContainsKey(lookupData)) {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(lookupData.Name);
            GameplayManager gameplayManager = LocateGameplayManager(scene);
            if(gameplayManager == null) {
                Debug.LogError($"Can't GetGameplayManager for SceneLookupData \"{lookupData}\" it wasn't located.");
                return null;
            }

            gameplayManagers.Add(lookupData, gameplayManager);
        }

        return gameplayManagers[lookupData];
    }

    /// <summary>
    /// Look for a GameplayManager in a scene
    /// </summary>
    public static GameplayManager LocateGameplayManager(Scene scene) 
    {
        foreach(GameObject obj in scene.GetRootGameObjects()) {
            obj.TryGetComponent(out GameplayManager testGameplayManager);
            if (testGameplayManager != null)
                return testGameplayManager;
        }
        return null;
    }

    /// <summary>
    /// Checks for a GameplayManager in the same scene as the provided GameplayManagerBehavior script.
    /// If one is found, the GameplayManager will be stored in the gameplayManagers dictionary and
    ///   the GameplayManagerLoaded interface method gets called.
    /// Use out loadStatus to see if the GameplayManager was successfully loaded or not.
    /// </summary>
    public void LoadGameplayManager(GameplayManagerBehavior gameplayManagerBehavior, out bool loadStatus) 
    {
        loadStatus = false;
        if(gameplayManagerBehavior is not MonoBehaviour) {
            Debug.LogError("A GameplayManagerBehavior interface is on a script that isn't a Monobehavior!");
            return;
        }
        MonoBehaviour monoGMB = gameplayManagerBehavior as MonoBehaviour;
        if(monoGMB == null) {
            waitingForGameplayManagers.Remove(gameplayManagerBehavior);
            return;
        }
        Scene objectsScene = monoGMB.gameObject.scene;
        SceneLookupData lookupData = objectsScene.GetSceneLookupData();

        // Create dictionary if its null
        gameplayManagers ??= new();
        
        if(gameplayManagers.ContainsKey(lookupData)) {
            Debug.LogWarning("Already loaded gameplayManager but people are still requesting loads for it."); 
            return;
        }

        GameplayManager loadedGameplayManager = LocateGameplayManager(objectsScene);

        // Check if the locate call returned something
        if(loadedGameplayManager == null)
            return;

        // We successfully loaded a GameplayManager, add it to the dictionary
        gameplayManagers.Add(lookupData, loadedGameplayManager);
        gameplayManagerBehavior.GameplayManagerLoaded(loadedGameplayManager);

        // If the gameplay manager was waiting, remove it from the wait list
        if(waitingForGameplayManagers.Contains(gameplayManagerBehavior))
            waitingForGameplayManagers.Remove(gameplayManagerBehavior);

        loadStatus = true;
    }

    public void SubscribeForGameplayManager(GameplayManagerBehavior gameplayManagerBehavior) 
    {
        // If we can get the GameplayManager right away do it so we don't have to waste time waiting for the next Update() call.
        LoadGameplayManager(gameplayManagerBehavior, out bool loadStatus);
        if(loadStatus)
            return;

        waitingForGameplayManagers.Add(gameplayManagerBehavior);
    }

}
