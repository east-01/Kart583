using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostCanisterWorldItem : WorldItem
{
    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        KartController kc = KartBehavior.LocateManager(owner).GetKartController();
        kc.boostDecayTime = 0;
        kc.boostAmount = kc.maxBoost;

        Destroy(gameObject);
    }

    // We shouldn't have to handle these since the item immediately gets destroyed on spawn
    public override void ItemDestroyed() { throw new System.NotImplementedException(); }
    public override void ItemHit(GameObject hitPlayer) { throw new System.NotImplementedException(); }
}
