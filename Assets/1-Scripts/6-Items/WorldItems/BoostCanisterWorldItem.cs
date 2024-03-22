using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoostCanisterWorldItem : WorldItem
{
    protected override void Internal_ActivateItem(ItemSpawnData spawnData)
    {
        KartController kc = OwnerKartManager.GetKartController();
        kc.boostDecayTime = 0;
        kc.boostAmount = kc.settings.maxBoost;

        Destroy(gameObject);
    }

    // We shouldn't have to handle these since the item immediately gets destroyed on spawn
    protected override void Internal_ItemDestroyed() { throw new System.NotImplementedException(); }
    protected override void Internal_ItemHit(string hitPlayerUUID) { throw new System.NotImplementedException(); }
}
