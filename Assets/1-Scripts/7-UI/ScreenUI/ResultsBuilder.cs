using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

/** This class will build the results rows out of the ResultRow prefabs. */
public class ResultsBuilder : MonoBehaviour
{

    public float paddingV = 5;

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
        List<KartManager> dnf = new(); // The people who don't have raceCompletion >= 1
        GameplayManager.PlayerManager.kartObjects.ForEach(ko => {
            KartManager km = KartBehavior.LocateManager(ko);
            if(km.GetPositionTracker().raceCompletion < 1)
                dnf.Add(km);
            else
                unsorted.Add(km);
        });
        List<KartManager> sorted = new();

        // Sort everyone by race time
        for(i = 0; i < unsorted.Count; i++) {
            float smallestRaceTime = float.MaxValue;
            KartManager smallestKM = null;

            foreach(KartManager manager in unsorted) {
                PositionTracker pt = manager.GetPositionTracker();
                // Check if this is the lowest finish time
                print(pt.raceFinishTime + " compared to " + smallestRaceTime);
                if(pt.raceFinishTime < smallestRaceTime) {
                    smallestRaceTime = pt.raceFinishTime;
                    smallestKM = manager;
                }
            }

            if(smallestKM == null)
                throw new InvalidOperationException("Failed to select next fastest kart!");

            unsorted.Remove(smallestKM);
            print("unsorted objs left: " + unsorted.Count);
            sorted.Add(smallestKM);
        }

        KartManager[] finalPositions = new KartManager[kartCount];
        i = 0;
        sorted.ForEach(km => { finalPositions[i] = km; i++; });
        dnf.ForEach(km => { finalPositions[i] = km; i++; });

        // DEBUG CODE
        StringBuilder sb = new();
        foreach(KartManager manager in finalPositions) {
            if(manager == null) {
                sb.Append("null, ");
                continue;
            }
            sb.Append(manager.GetPositionTracker().raceFinishTime + ", ");
        }
        print("sorted racers: " + sb.ToString());
        // END DEBUG

        // Delete old menu elements
        menuElements?.ForEach(e => Destroy(e));
        menuElements = new List<GameObject>();

        float rowW = placementRowPrefab.GetComponent<RectTransform>().rect.width;
        float rowH = placementRowPrefab.GetComponent<RectTransform>().rect.height;

        float pageHeightPx = kartCount*rowH + (kartCount-1)*rowH;
        resultsContainer.sizeDelta = new Vector2(rowW, pageHeightPx);
        resultsContainer.anchoredPosition = Vector2.zero;

        for(i = 0; i < kartCount; i++) {
            KartManager manager = finalPositions[i];
            print(i + " " + manager);
            if(manager == null) break;
            GameObject newObj = Instantiate(placementRowPrefab, resultsContainer);
            newObj.GetComponent<PlacementRow>().UpdateVisuals(manager);
            newObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, i*rowH + Math.Max(0, i-1)*paddingV);

            menuElements.Add(newObj);

            i++;
        }

        gameObject.SetActive(true);

    }
}
