using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KartItemManager : KartBehavior
{

    public ItemSlotAnimator itemSlotManager;
	public Image heldItemImage;

	private Item? slotItem;
	private Item? heldItem;

	void Start() 
	{
		heldItemImage.gameObject.SetActive(false);
	}

    /** Callback for when a player hits an item box. 
	    Return true if item successfully recieved, false if not. */
	public bool HitItemBox(GameObject itemBox) 
    { 
		if(slotItem != null) return false;

		// Eventually this code will change to better give items based off of position
        Item result = GameplayManager.ItemAtlas.RollRandom();

		this.slotItem = result;
		
		if(itemSlotManager != null) itemSlotManager.AnimateItems(result);
		return true;
	}

	public void OnItem(InputAction.CallbackContext context) {
		if(!context.performed && !context.canceled) return;
		PerformItemInput(context.performed);
	}

	public void PerformItemInput(bool pressed) 
	{
		if(pressed && (itemSlotManager == null || !itemSlotManager.IsAnimating()) && slotItem != null && heldItem == null) {

			heldItem = slotItem.Value;
			slotItem = null;

			if(itemSlotManager != null) itemSlotManager.DisableChildren();
			heldItemImage.gameObject.SetActive(true);
			heldItemImage.sprite = GameplayManager.ItemAtlas.RetrieveData(heldItem.Value).itemIcon;

		} else if(!pressed && heldItem.HasValue) {

			GameObject worldItemPrefab = GameplayManager.ItemAtlas.RetrieveData(heldItem.Value).worldItem;
			String err = null;
			if(worldItemPrefab == null || worldItemPrefab.GetComponent<WorldItem>() == null)	
				err = worldItemPrefab == null ? 
				"Item \"" + heldItem + "\" is missing a world item prefab!" : 
				"Item \"" + heldItem + "\" has a world item prefab, but that prefab is missing a WorldItem script";			

			// Clear held item
			heldItem = null;
			heldItemImage.gameObject.SetActive(false);

			// If an error occured we don't want to instantiate a new item.
			if(err != null) { Debug.Log(err); return; } 

			Instantiate(worldItemPrefab).GetComponent<WorldItem>().ActivateItem(gameObject, kartCtrl.TurnInput);

		}
	}

	public bool HasSlotItem { get { return slotItem.HasValue; } }
	public bool HasHeldItem { get { return heldItem.HasValue; } }

}
