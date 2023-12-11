using UnityEngine;
using System;

public class OilWorldItem : WorldItem
{

    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        Owner = owner;
        lifeTime = 30f; // 30s of lifetime

        KartController kc = owner.GetComponent<KartController>();

        transform.position = owner.transform.position - kc.KartForward.normalized*3f - kc.up*0.3f;

        // TODO: Play activation animation and sound

    }

    public override void ItemDestroyed()
    {
        print("TODO: Item oil destroyed");
        Destroy(gameObject);
    }

    public override void ItemHit(GameObject hitPlayer)
    {
        print("TODO: Player hit oil spill");
        ItemDestroyed();
    }
    
}