// using System;
// using System.Collections.Generic;
// using FishNet.Managing.Scened;
// using FishNet.Object;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// /// <summary>
// /// A superclass for scripts who want to use the GameplayManager.
// /// </summary>
// public class NetworkGameplayManagerBehaviour : NetworkBehaviour
// {

//     private GameplayManager _gameplayManager;
//     protected GameplayManager gameplayManager { 
//         get {
//             if(_gameplayManager == null)
//                 if(!AttemptToLoadGameplayManager())
//                     Debug.LogWarning("Tried to access gameplayManager before it was loaded! Either move problem code to GameplayManagerLoaded or add IsGameplayManagerLoaded() guard clause to Update().");
//             return _gameplayManager;
//         } 
//         private set { _gameplayManager = value; } 
//     }
//     protected KartLevelManager kartLevelManager { get { return gameplayManager.KartLevelManager; } }

//     protected void Awake() 
//     {
//         print("NetworkGameplayBehavior woke up");
//         AttemptToLoadGameplayManager();
//         gameObject.AddComponent(typeof(GameplayManagerObserver));

//         //TODO: Add a watcher component to the gameobject that waits for the gameplaymanager to ready
//         // while the gameplaymanager doesn't exist, it disables GameplayManagerBehaviour components
//     }

//     /// <summary>
//     /// Default GameplayManagerLoaded call, to be overridden by child classes.
//     /// </summary>
//     protected void GameplayManagerLoaded() {

//     }

    

//     /// <summary>
//     /// Check if this instance has gameplayManager loaded. If 
//     /// </summary>
//     /// <param name="attemptToLoad"></param>
//     /// <returns></returns>
//     protected bool IsGameplayManagerLoaded(bool attemptToLoad) 
//     {
//         if(_gameplayManager != null) {
//             return true;
//         } else if(attemptToLoad) {
//             return AttemptToLoadGameplayManager();
//         } else {
//             return false;
//         }
//     }

// }
