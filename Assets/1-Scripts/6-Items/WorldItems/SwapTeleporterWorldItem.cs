using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapTeleporterWorldItem : WorldItem
{
    protected override void Internal_ActivateItem(ItemSpawnData spawnData)
    {
        PositionTracker ownerPT = OwnerKartManager.GetPositionTracker();
        float currentRaceCompletion = ownerPT.RaceCompletion;

        /* Pick target */
        // Store the kartmanager and it's weight
        // Determine weight based off of distance from player
        Dictionary<KartManager, float> dict = new();
        float totalWeight = 0;
        foreach(GameObject obj in gameplayManager.PlayerManager.kartObjects) {
            KartManager km = KartBehavior.LocateManager(obj);
            PositionTracker pt = km.GetPositionTracker();
            // pt.racepos == 1 makes it so that we can teleport to anyone if we're in first
            if(pt.GetRaceCompletion() > currentRaceCompletion || pt.racePos == 1) {
                float weight = 1 - (pt.GetRaceCompletion() - currentRaceCompletion);
                totalWeight += weight;
                dict.Add(km, weight);
            }
        }

        // PICK KART BASED OFF OF WEIGHT
        float randomValue = UnityEngine.Random.value * totalWeight;
        float cumulativeWeight = 0;
        KartManager target = null;
        foreach (KartManager km in dict.Keys) {
            cumulativeWeight += dict[km];
            if (randomValue <= cumulativeWeight) {
                target = km;
            }
        }

        if(target == null) throw new InvalidOperationException("Target cannot be null!");

        PositionTracker targetPT = target.GetPositionTracker();

        // Store the transform values of object1
        Vector3 tempPosition = OwnerKartManager.gameObject.transform.position;
        Quaternion tempRotation = OwnerKartManager.gameObject.transform.rotation;
        int tempWaypoint = ownerPT.waypointIndex;

        OwnerKartManager.gameObject.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        ownerPT.waypointIndex = targetPT.waypointIndex;

        target.transform.SetPositionAndRotation(tempPosition, tempRotation);
        targetPT.waypointIndex = tempWaypoint;

        Destroy(gameObject);
    }

    // We shouldn't have to handle these since the item immediately gets destroyed on spawn
    protected override void Internal_ItemDestroyed() { throw new System.NotImplementedException(); }
    protected override void Internal_ItemHit(string hitPlayerUUID) { throw new System.NotImplementedException(); }
}
