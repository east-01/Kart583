using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Is responsible for finding GameplayManagers in a scene and distributing
///   said GameplayManagers to GameplayManagerBehaviours that request them.
/// </summary>
public class GameplayManagerDelegate : MonoBehaviour
{

    private List<GameplayManagerBehavior> waitingForGameplayManagers = new();

    private void Update() 
    {
        if(waitingForGameplayManagers.Count == 0)
            return;
        List<GameplayManagerBehavior> toRemove = new();
        foreach(GameplayManagerBehavior gmb in waitingForGameplayManagers) {
            if(GetGameplayManager(gmb))
                toRemove.Add(gmb);
        }
        toRemove.ForEach(gmb => waitingForGameplayManagers.Remove(gmb));
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
    /// If one is found, return true and the GameplayManagerLoaded interface method gets called.
    /// </summary>
    public bool GetGameplayManager(GameplayManagerBehavior gameplayManagerBehavior) 
    {
        if(gameplayManagerBehavior is not MonoBehaviour) {
            Debug.LogError("A GameplayManagerBehavior interface is on a script that isn't a Monobehavior!");
            return false;
        }
        Scene objectsScene = (gameplayManagerBehavior as MonoBehaviour).gameObject.scene;
        FishNet.Managing.Scened.SceneLookupData lookupData = new(objectsScene.handle, objectsScene.name);
        if(SceneDelegate.Instance == null)
            return false;
        if(!SceneDelegate.Instance.IsSceneRegistered(lookupData)) 
            return false;

        GameplayManager toReturn = SceneDelegate.Instance.GetSceneElements(lookupData).GameplayManager;

        if(toReturn == null)
            return false;
        
        gameplayManagerBehavior.GameplayManagerLoaded(toReturn);
        return true;
    }

    public void SubscribeForGameplayManager(GameplayManagerBehavior gameplayManagerBehavior) 
    {
        // If we can get the GameplayManager right away do it so we don't have to waste time waiting for the next Update() call.
        if(GetGameplayManager(gameplayManagerBehavior))
            return;
        waitingForGameplayManagers.Add(gameplayManagerBehavior);
    }

}
