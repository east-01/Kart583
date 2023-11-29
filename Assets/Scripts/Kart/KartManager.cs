using System;
using UnityEngine;
using UnityEngine.InputSystem;

/** This class will be responsible for one player.
    Manages items currently */
public class KartManager : MonoBehaviour 
{

	public ItemSlotAnimator itemSlotManager;
	public HeldItem heldItemScript;

	private Item? slotItem;
	private Item? heldItem;

	/** Callback for when a player hits an item box. 
	    Return true if item successfully recieved, false if not. */
	public bool HitItemBox(GameObject itemBox) 
    { 
		if(slotItem != null) return false;

        Array values = Enum.GetValues(typeof(Item));
        int randomIndex = UnityEngine.Random.Range(0, values.Length);
        Item result = (Item)values.GetValue(randomIndex);

		this.slotItem = result;

		itemSlotManager.AnimateItems(result);
		return true;
	}

	public void OnItem(InputAction.CallbackContext context) {
		if(context.performed && !itemSlotManager.IsAnimating() && slotItem != null && heldItem == null) {

			heldItem = slotItem;
			slotItem = null;

			itemSlotManager.DisableChildren();
			heldItemScript.Show((Item)heldItem);

		} else if(context.canceled && heldItem != null) {
			print("TODO: Perform action for item \"" + heldItem + "\"");
			heldItem = null;

			heldItemScript.Hide(true);
		}
	}

}
