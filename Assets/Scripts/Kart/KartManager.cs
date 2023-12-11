using System;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** This class will be responsible for one player.
    Manages items currently */
public class KartManager : MonoBehaviour 
{

	// ##################################
	// TODO: Make kart manager like GameplayManager in that it collects and provides access
	//   to essential kart functions
	// ##################################

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
		if(context.performed && (itemSlotManager == null || !itemSlotManager.IsAnimating()) && slotItem != null && heldItem == null) {

			heldItem = slotItem.Value;
			slotItem = null;

			if(itemSlotManager != null) itemSlotManager.DisableChildren();
			heldItemImage.gameObject.SetActive(true);
			heldItemImage.sprite = GameplayManager.ItemAtlas.RetrieveData(heldItem.Value).itemIcon;

		} else if(context.canceled && heldItem.HasValue) {

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

			Instantiate(worldItemPrefab).GetComponent<WorldItem>().ActivateItem(gameObject, GetComponent<KartController>().TurnInput);

		}
	}

	public static bool IsKartGameObject(GameObject obj) 
	{
		return obj.GetComponent<KartManager>() != null;
	}
}
