using UnityEngine;
using System;

public class OilWorldItem : WorldItem
{

    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        Owner = owner;
        lifeTime = 30f; // 30s of lifetime

        KartController kc = owner.GetComponent<KartController>();

        transform.position = owner.transform.position - kc.KartForward.normalized*1.5f;

        // TODO: Play activation animation and sound

    }

    public override void ItemDestroyed()
    {
        Destroy(gameObject);
    }

    public override void ItemHit(GameObject hitPlayer)
    {
        ItemDestroyed();
    }
}