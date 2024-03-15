using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapTeleporterWorldItem : WorldItem
{
    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        PositionTracker ownerPT = KartBehavior.LocateManager(owner).GetPositionTracker();
        float currentRaceCompletion = ownerPT.raceCompletion;

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
        Vector3 tempPosition = owner.transform.position;
        Quaternion tempRotation = owner.transform.rotation;
        int tempWaypoint = ownerPT.waypointIndex;

        owner.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        ownerPT.waypointIndex = targetPT.waypointIndex;

        target.transform.SetPositionAndRotation(tempPosition, tempRotation);
        targetPT.waypointIndex = tempWaypoint;

        Destroy(gameObject);
    }

    // We shouldn't have to handle these since the item immediately gets destroyed on spawn
    public override void ItemDestroyed() { throw new System.NotImplementedException(); }
    public override void ItemHit(GameObject hitPlayer) { throw new System.NotImplementedException(); }
}
