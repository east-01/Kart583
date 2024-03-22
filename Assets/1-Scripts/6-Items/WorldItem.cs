using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using FishNet.Object;
using JetBrains.Annotations;

/** World items (items that are placed ingame after the player uses them) will
  *   be instantiated by the KartManager when the player uses an item, and then
  *   activated by the ActivateItem() call. Currently, the only parameter is
  *   direction input, but we can add more information if needed. */
public abstract class WorldItem : NetworkBehaviour, GameplayManagerBehavior
{
    protected GameplayManager gameplayManager;
	protected KartLevelManager kartLevelManager;

	private KartManager ownerKartManager;
    /** Player/Bot gameobject that threw the item */
    public KartManager OwnerKartManager { 
		get {
			if(!hasPopulatedOwner) {
				Debug.LogError($"Attempted to grab owner before owner was populated in WorldItem on \"{gameObject.name}\". Make sure to call PopulateOwner() in ActivateItem()");
				return null;
			} else
				return ownerKartManager;
		} 
		protected set {
			ownerKartManager = value; 
			hasPopulatedOwner = ownerKartManager != null;
		}
	}
	private bool hasPopulatedOwner = false;
    /** How long left until the item despawns. */
    protected float lifeTime;

	/// <summary>
	/// Data that was attempted to spawn but gameplayManager was null.
	/// Will be re-called once this WorldItem recieves a gameplayManager.
	/// </summary>
	private ItemSpawnData? queuedData = null;

	/// <summary>
	/// Entrypoint for Item spawning, called by the ItemManager once it spawns the WorldItem prefab on the server.
	/// </summary>
	[ObserversRpc(RunLocally = true)]
	public void ObserversRpcActivateItem(ItemSpawnData spawnData) 
	{
		ActivateItem(spawnData);
	}

	/// <summary>
	/// Calls Internal_ActivateItem which is the unique implementation of item activation
	///   for each WorldItem. If this WorldItem doesn't have a gameplayManager yet, it
	///   will queue the spawnData in queuedData which will be re-called once GameplayManagerLoaded fires.
	/// </summary>
	public void ActivateItem(ItemSpawnData spawnData) 
	{
		if(gameplayManager == null) {
			queuedData = spawnData;
			return;
		}

		OwnerKartManager = gameplayManager.PlayerManager.SearchForKartManager(spawnData.ownerUUID);
        if(!hasPopulatedOwner) {
            Debug.LogError($"ActivateItem failed to find owner from UUID \"{spawnData.ownerUUID}\"");
            return;
        }

		Internal_ActivateItem(spawnData);
	}

	/// <summary>
	/// Activate the item object, this will usually:
	///   - Start item animation
	///   - Move the item to position based off of input.
	///   - Set owner/lifeTime
	/// Called by KartManager once item is used.
	/// Owner gameobject is the player that threw the item, they need to be kept
	///   track of in order to reward whoever did the damage. 
	/// </summary>
    protected abstract void Internal_ActivateItem(ItemSpawnData spawnData);

	[ObserversRpc(RunLocally = true)]
	public void ItemDestroyed() 
	{
		Internal_ItemDestroyed();
	}

	/// <summary>
	/// The item was destroyed (either by lifetime decay or player/item hit), play
	///   destroy animation.
	/// </summary>
    protected abstract void Internal_ItemDestroyed();

	[ObserversRpc(RunLocally = true)]
	public void ItemHit(string hitPlayerUUID) 
	{
		Internal_ItemHit(hitPlayerUUID);
	}

	/// <summary>
	/// The item was hit, do damage accordingly.
	/// Similar to ActivateItem()
	/// </summary>
    protected abstract void Internal_ItemHit(string hitPlayerUUID);

    void Awake() 
    {
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;

		if(queuedData.HasValue) {
			ActivateItem(queuedData.Value);
			queuedData = null;
		}
    }

    void Update() 
    {
		if(gameplayManager == null)
			return;

        // Destroy item when lifetime runs out
        lifeTime -= Time.deltaTime;
        if(lifeTime <= 0) {
			Destroy();
            return;
        }
    }

	private void Destroy() 
	{
		ItemDestroyed();

		if(!base.IsServer) {
			ServerRpcDestroy();
			return;
		}

		base.ServerManager.Despawn(GetComponent<NetworkObject>());
	}

	[ServerRpc(RequireOwnership = false)]
	private void ServerRpcDestroy() 
	{
		Destroy();
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
		if(!base.IsServer)
			return;
		if(other.tag == "Kart") {
            // Hit a player, deal damage
			KartManager otherKM = KartBehavior.LocateManager(other);
			if(otherKM == null) {
				Debug.LogError("A collider with a \"Kart\" tag hit an item but it didn't have a KartManager!");
				return;
			}
            ItemHit(otherKM.GetPlayerData().uuid);
			Destroy();
        } else if(other.tag == "Item") {
            // Hit a different item, destroy both
            WorldItem wi = other.GetComponent<WorldItem>();
            if(wi == null) throw new InvalidOperationException("A collider with an \"Item\" tag hit an item but it didn't have a WorldItem script!");
            wi.Destroy();
            Destroy();
        }
	}

}  
