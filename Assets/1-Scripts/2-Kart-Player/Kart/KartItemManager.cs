using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KartItemManager : KartBehavior, GameplayManagerBehavior
{

	private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

	private ItemSlotAnimator _itemSlotManager;
    public ItemSlotAnimator ItemSlotManager{
		get {
			// Attempt to locate a PlayerObjectInGame object for it's ItemSlotAnimator
			if(_itemSlotManager == null && kartManager.HasPOIGDelegate)
				_itemSlotManager = kartManager.POIGDelegate.PlayerHUDCanvas.ItemDisplay;
			return _itemSlotManager;
		}
		private set { _itemSlotManager = value; }
	}

	public Image heldItemImage;

	[SyncVar(OnChange = nameof(ItemsUpdated))]
	private Item slotItem;
	[SyncVar(OnChange = nameof(ItemsUpdated))]
	private Item heldItem;

	new protected void Awake() 
	{
		base.Awake();
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
	}

	void Start() 
	{
		heldItemImage.gameObject.SetActive(false);
	}

	public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

	public void PerformItemInput(bool pressed) 
	{		
		if(!base.IsServer) {
			ServerRpcPerformItemInput(pressed);
			return;
		}	

		if(pressed && (ItemSlotManager == null || !ItemSlotManager.IsAnimating()) && slotItem != Item.NONE && heldItem == Item.NONE) {

			heldItem = slotItem;
			slotItem = Item.NONE;

		} else if(!pressed && heldItem != Item.NONE) {

			GameObject worldItemPrefab = gameplayManager.ItemAtlas.RetrieveData(heldItem).worldItemPrefab;
			String err = null;
			if(worldItemPrefab == null || worldItemPrefab.GetComponent<WorldItem>() == null)	
				err = worldItemPrefab == null ? 
				"Item \"" + heldItem + "\" is missing a world item prefab!" : 
				"Item \"" + heldItem + "\" has a world item prefab, but that prefab is missing a WorldItem script";			

			// If an error occured we don't want to instantiate a new item.
			if(err != null) { Debug.Log(err); return; } 

			// Make request to spawn item
			gameplayManager.ItemManager.SpawnItem(new ItemSpawnData() {
				ownerUUID = kartManager.GetPlayerData().uuid,
				itemType = heldItem,
				stickDirection = kartCtrl.TurnInput
			});

			heldItem = Item.NONE;

		}
	}

	[ServerRpc]
	private void ServerRpcPerformItemInput(bool pressed) 
	{
		PerformItemInput(pressed);
	}

    /** Callback for when a player hits an item box. 
	    Return true if item successfully recieved, false if not. */
	public bool HitItemBox(GameObject itemBox) 
    { 
		if(gameplayManager == null) {
			Debug.LogWarning("Hit item box without a gameplayManager being loaded!");
			return false;
		}

		if(slotItem != Item.NONE) 
			return false;

		if(!base.IsServer)
			return false;

		// Eventually this code will change to better give items based off of position
        Item result = gameplayManager.ItemAtlas.RollRandom();

		slotItem = result;

		if(base.Owner.IsValid) 
			TargetRpcRecieveItem(base.Owner, result);
		else
			RecieveItem(result);

		return true;
	}

	[TargetRpc]
	public void TargetRpcRecieveItem(NetworkConnection client, Item result) 
	{
		RecieveItem(result);
	}

	/// <summary>
	/// Callback to perform item recieve animation and show item menu
	/// </summary>
	public void RecieveItem(Item result) 
	{
		if(base.Owner.IsValid && !base.IsOwner) {
			Debug.LogError("Can't recieve item on something we don't own");
			return;
		}
		if(ItemSlotManager != null)
			ItemSlotManager.AnimateItems(result);
	}

	/// <summary>
	/// Called when either item updates
	/// </summary>
	private void ItemsUpdated(Item prev, Item current, bool asServer) 
	{
		if(asServer)
			return;

		if(heldItem == Item.NONE) {
			// Clear held item
			heldItemImage.gameObject.SetActive(false);
		} else {
			heldItemImage.gameObject.SetActive(true);
			heldItemImage.sprite = gameplayManager.ItemAtlas.RetrieveData(heldItem).itemIcon;
		}

		if(slotItem == Item.NONE && ItemSlotManager != null) {
			ItemSlotManager.DisableChildren();
		}

	} 

	public bool HasSlotItem { get { return slotItem != Item.NONE; } }
	public bool HasHeldItem { get { return heldItem != Item.NONE; } }

}
