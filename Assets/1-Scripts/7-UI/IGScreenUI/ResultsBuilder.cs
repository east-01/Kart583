using System;
using System.Collections.Generic;
using UnityEngine;

/** This class will build the results rows out of the ResultRow prefabs. */
public class ResultsBuilder : MonoBehaviour
{

    public GameObject placementRowPrefab;
    public RectTransform resultsContainer;

    private List<GameObject> menuElements;

    void Awake() 
    {
        gameObject.SetActive(false);
    }

    /** Updates the results screen then shows it.
      * The kart array parameter is expected to be sorted. 
      * 
      * Honestly, idk why this method is so complicated. Shit just would not work easily. */
    public void ShowResults() 
    {

        int kartCount = GameplayManager.PlayerManager.kartObjects.Count;
        int i;

        List<KartManager> unsorted = new();
        List<KartManager> dnf = new(); 
        
        // Separate people that finished/didn't finish
        GameplayManager.PlayerManager.kartObjects.ForEach(ko => {
            KartManager km = KartBehavior.LocateManager(ko);
            if(km.GetPositionTracker().raceCompletion < 1)
                dnf.Add(km);
            else
                unsorted.Add(km);
        });

        List<KartManager> sorted = new();

        // Sort finished racers by time
        while(unsorted.Count > 0) {
            float smallestRaceTime = float.MaxValue;
            KartManager smallestKM = null;

            foreach(KartManager manager in unsorted) {
                PositionTracker pt = manager.GetPositionTracker();
                // Check if this is the lowest finish time
                if(pt.raceFinishTime < smallestRaceTime) {
                    smallestRaceTime = pt.raceFinishTime;
                    smallestKM = manager;
                }
            }

            if(smallestKM == null)
                throw new InvalidOperationException("Failed to select next fastest kart.");

            unsorted.Remove(smallestKM);
            sorted.Add(smallestKM);
        }

        // Sort unfinished racers by race completion
        while(dnf.Count > 0) {
            float highestRaceCompletion = float.MinValue;
            KartManager hrcKM = null;

            foreach(KartManager manager in dnf) {
                PositionTracker pt = manager.GetPositionTracker();
                // Check if this is the lowest finish time
                if(pt.raceCompletion > highestRaceCompletion) {
                    highestRaceCompletion = pt.raceCompletion;
                    hrcKM = manager;
                }
            }

            if(hrcKM == null)
                throw new InvalidOperationException("Failed to select next highest race completion.");

            dnf.Remove(hrcKM);
            sorted.Add(hrcKM);
        }

        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        for(i = 0; i < kartCount; i++) {
            KartManager manager = sorted[i];
            if(manager == null) 
                throw new InvalidOperationException("Manager cannot be null.");
                
            GameObject newObj = Instantiate(placementRowPrefab, resultsContainer);
            newObj.GetComponent<PlacementRow>().UpdateVisuals(manager, i+1);

            menuElements.Add(newObj);
        }

        gameObject.SetActive(true);

    }

    public bool ResultsShown { get { return gameObject.activeSelf; } }
}