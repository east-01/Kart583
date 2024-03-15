using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/** World items (items that are placed ingame after the player uses them) will
  *   be instantiated by the KartManager when the player uses an item, and then
  *   activated by the ActivateItem() call. Currently, the only parameter is
  *   direction input, but we can add more information if needed. */
public abstract class WorldItem : MonoBehaviour, GameplayManagerBehavior
{
    protected GameplayManager gameplayManager;
	  protected KartLevelManager kartLevelManager;

    /** Player/Bot gameobject that threw the item */
    public GameObject Owner { get; protected set; }
    /** How long left until the item despawns. */
    protected float lifeTime;
    /** Activate the item object, this will usually:
      *   - Start item animation
      *   - Move the item to position based off of input.
      *   - Set owner/lifeTime
      * Called by KartManager once item is used.
      * Owner gameobject is the player that threw the item, they need to be kept
      *   track of in order to reward whoever did the damage. */
    public abstract void ActivateItem(GameObject owner, Vector2 directionInput);
    /** The item was destroyed (either by lifetime decay or player/item hit), play
          destroy animation. */
    public abstract void ItemDestroyed();
    /** The item was hit, do damage accordingly. 
      * Called in OnTriggerEnter() in this class. */
    public abstract void ItemHit(GameObject hitPlayer);

	void Awake() 
	{
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
	}

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

    void Update() 
    {
        // Destroy item when lifetime runs out
        lifeTime -= Time.deltaTime;
        if(lifeTime <= 0) {
            ItemDestroyed();
            Destroy(gameObject);
            return;
        }
    }

    /** Check for collisions with players/other items */
    void OnTriggerEnter(Collider other) 
    {
		CheckForInteraction(other.gameObject);
    }

    public void OnCollisionEnter(Collision collision) 
	{
		CheckForInteraction(collision.gameObject);
    }

	/** Check for item interactions from a OnTriggerEnter or OnColliderEnter */
	public void CheckForInteraction(GameObject other) 
	{
		if(other.tag == "Kart") {
            // Hit a player, deal damage
            if(other.GetComponent<KartManager>() == null) throw new InvalidOperationException("A collider with a \"Kart\" tag hit an item but it didn't have a KartManager!");
            ItemHit(other);
        } else if(other.tag == "Item") {
            // Hit a different item, destroy both
            WorldItem wi = other.GetComponent<WorldItem>();
            if(wi == null) throw new InvalidOperationException("A collider with an \"Item\" tag hit an item but it didn't have a WorldItem script!");
            wi.ItemDestroyed();
            ItemDestroyed();
        }
	}

}  
