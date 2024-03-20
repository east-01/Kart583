using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/** This class will build the results rows out of the ResultRow prefabs. */
public class ResultsBuilder : MonoBehaviour, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    public GameObject placementRowPrefab;
    public RectTransform resultsContainer;

    private List<GameObject> menuElements;
    public bool waitingForPlacements = false;

    void Awake() 
    {        
        SceneDelegate.Instance.SubscribeForGameplayManager(this);
    
        gameObject.SetActive(false);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

    int ticked;
    void Update() {
        if(gameplayManager == null)
            return;

        if(waitingForPlacements && gameplayManager.RaceManager.GetPlacements().Count == gameplayManager.PlayerManager.KartCount) {
            waitingForPlacements = false;
            ShowResults();
        }
    }

    public void ShowResults() 
    {
        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        SyncDictionary<string, RacePlacementData> placements = gameplayManager.RaceManager.GetPlacements();
        for(int position = 0; position < gameplayManager.PlayerManager.KartCount; position++) {
            string playerUUID = null;
            // Find playerUUID from position
            foreach(string testUUID in placements.Keys) {
                if(placements[testUUID].position == position) {
                    playerUUID = testUUID;
                    break;
                }
            }

            if(playerUUID == null) {
                Debug.LogError($"ResultsBuilder failed to find UUID from position " + position);
                continue;
            }

            KartManager manager = gameplayManager.PlayerManager.SearchForKartManager(playerUUID);
            if(manager == null) {
                Debug.LogError($"Couldn't locate KartManager from uuid \"{playerUUID}\"");
                continue;
            }

            RacePlacementData racePlacementData = placements[playerUUID];                
            GameObject newObj = Instantiate(placementRowPrefab, resultsContainer);
            newObj.GetComponent<PlacementRow>().UpdateVisuals(manager, racePlacementData);
            menuElements.Add(newObj);
        }
    }

    public bool ResultsShown { get { return gameObject.activeSelf; } }
}
