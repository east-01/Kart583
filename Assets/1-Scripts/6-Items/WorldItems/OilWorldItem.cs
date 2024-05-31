using UnityEngine;
using System;

public class OilWorldItem : WorldItem
{

    protected override void Internal_ActivateItem(ItemSpawnData spawnData)
    {        
        lifeTime = 25f; // 30s of lifetime

        KartController kc = OwnerKartManager.GetKartController();
        transform.position = OwnerKartManager.gameObject.transform.position - kc.KartForward.normalized*3f - kc.up*0.3f;

        // TODO: Play activation animation and sound
        CoreManager.AudioManager.PlayOneShotSound(AudioFile.FX_ITEM_OIL_SPILL, 1f, transform);
    }

    protected override void Internal_ItemDestroyed()
    {
        
    }

    protected override void Internal_ItemHit(string hitPlayerUUID)
    {
        KartManager hitKM = gameplayManager.PlayerManager.SearchForKartManager(hitPlayerUUID);
        if(hitKM == null) {
            Debug.LogError($"Internal_ItemHit could not locate KartManager from uuid \"{hitPlayerUUID}\"");
            return;
        }
        
        hitKM.GetKartController().damageCooldown = 2f;

        CoreManager.AudioManager.PlayOneShotSound(AudioFile.KART_DAMAGED, 1f, transform);
        CoreManager.AudioManager.PlayOneShotSound(AudioFile.FX_ITEM_OIL_SPILL, 1f, transform);

        Internal_ItemDestroyed();
    }
    
}