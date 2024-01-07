using UnityEngine;

public class BotItemManager : KartBehavior {

    public Vector2 itemReleaseTimes = new Vector2(1f, 10f); // Min and max time (in seconds) it takes bot to drop item, randomly selected in range

    [SerializeField] private float itemReleaseTime; // Timer counting down seconds until bot releases item

    void Update() 
    {

        if(kartManager.HasHeldItem && itemReleaseTime > 0f) {
            itemReleaseTime -= Time.deltaTime;
            if(itemReleaseTime <= 0) {
                kartManager.PerformItemInput(false);
            }
        }

        if(kartManager.HasSlotItem && !kartManager.HasHeldItem && itemReleaseTime < 0.001f) {
            kartManager.PerformItemInput(true);
            itemReleaseTime = UnityEngine.Random.Range(itemReleaseTimes.x, itemReleaseTimes.y);
        }

    }

}