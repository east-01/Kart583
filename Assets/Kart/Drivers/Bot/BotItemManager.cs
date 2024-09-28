using UnityEngine;

public class BotItemManager : KartBehavior {

    public Vector2 itemReleaseTimes = new Vector2(1f, 10f); // Min and max time (in seconds) it takes bot to drop item, randomly selected in range

    [SerializeField] private float itemReleaseTime; // Timer counting down seconds until bot releases item

    void Update() 
    {

        if(kartItemManager.HasHeldItem && itemReleaseTime > 0f) {
            itemReleaseTime -= Time.deltaTime;
            if(itemReleaseTime <= 0) {
                kartItemManager.PerformItemInput(false);
            }
        }

        if(kartItemManager.HasSlotItem && !kartItemManager.HasHeldItem && itemReleaseTime < 0.001f) {
            kartItemManager.PerformItemInput(true);
            itemReleaseTime = UnityEngine.Random.Range(itemReleaseTimes.x, itemReleaseTimes.y);
        }

    }

}