using UnityEngine;
using System;

public class OilWorldItem : WorldItem
{

    public override void ActivateItem(GameObject owner, Vector2 directionInput)
    {

        Owner = owner;
        lifeTime = 25f; // 30s of lifetime

        KartController kc = owner.GetComponent<KartController>();

        transform.position = owner.transform.position - kc.KartForward.normalized*3f - kc.up*0.3f;

        // TODO: Play activation animation and sound

    }

    public override void ItemDestroyed()
    {
        Destroy(gameObject);
    }

    public override void ItemHit(GameObject hitPlayer)
    {
        if(!KartManager.IsKartGameObject(hitPlayer))
            throw new InvalidOperationException("Called ItemHit with a hitPlayer parameter that isn't an acceptable player!");
        
        hitPlayer.GetComponent<KartController>().damageCooldown = 2f;

        ItemDestroyed();
    }
    
}