using System;
using System.Collections.Generic;
using FishNet;
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

    void Update() {
        if(gameplayManager == null)
            return;

        if(waitingForPlacements && gameplayManager.RaceManager.GetPlacements().Count > 0) {
            waitingForPlacements = false;
            ShowResults();
        }
    }

    /** Updates the results screen then shows it.
      * The kart array parameter is expected to be sorted. 
      * 
      * Honestly, idk why this method is so complicated. Shit just would not work easily. */
    public void ShowResults() 
    {
        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        int i = 0;
        foreach(PlayerData data in gameplayManager.RaceManager.GetPlacements()) {
            KartManager manager = gameplayManager.PlayerManager.SearchForKartManager(data);
            if(manager == null) 
                throw new InvalidOperationException("Manager cannot be null.");
                
            GameObject newObj = Instantiate(placementRowPrefab, resultsContainer);
            newObj.GetComponent<PlacementRow>().UpdateVisuals(manager, i+1);

            menuElements.Add(newObj);
            i++;
        }
    }

    public bool ResultsShown { get { return gameObject.activeSelf; } }
}
