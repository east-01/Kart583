using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** The PlayerPanelController is responsible for a single PlayerObject */
public class PlayerPanelController : MonoBehaviour
{

    [SerializeField] TMP_Text titleText;
    [SerializeField] List<GameObject> toolTips;
    
    [SerializeField] GameObject colorSelect;
    [SerializeField] Button colorFirstSelection;
    [SerializeField] GameObject kartSelect;
    // [SerializeField] Button kartFirstSelection; 
    [SerializeField] Button readyButton;
    [SerializeField] GameObject readyText;

    private PlayerObject playerObj;
    private PlayerControls controlsReference;

    private Color origPanelColor; // Stored so we can revert to it if the player revert's their color selection

    private void Awake() 
    {
        // DO NOT ENABLE, WE USE THE PLAYERINPUT COMPONENT HERE
        // This is so we can have consistent name references in ActionTriggered
        controlsReference = new PlayerControls();

        origPanelColor = GetComponent<Image>().color;
    }

    private void OnDisable() 
    {
        playerObj.input.onActionTriggered -= ActionTriggered; // Event registered in SetPlayerObject
    }

    public void ActionTriggered(InputAction.CallbackContext context) {
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name) {
            // We have to reverse what is done in UpdatePosition() to see what needs to be taken off first.
            // This will be the opposite order as specified in UpdatePosition()
            if(playerObj.data.ready) {
                playerObj.data.ready = false;
            // } else if(## kart selected ##) {
            //     set kart to null
            } else if(playerObj.data.hexColor != null) {
                playerObj.data.hexColor = null;
            }
            UpdatePanel();
        }
    }

    /** Shortcut for UpdatePosition() & UpdateVisuals(). UpdatePosition is called first. */
    public void UpdatePanel() { UpdatePosition(); UpdateVisuals(); }

    /** Update the current selection so it reflects what stage we're at in player construction. 
        The order is: Color -> Kart -> Ready */
    public void UpdatePosition() 
    {

        // Disable everything so we can enable only what we want
        List<GameObject> everything = new() {colorSelect, kartSelect, readyButton.gameObject, readyText};
        everything.ForEach(go => go.SetActive(false));

        // Enable correct thing based on what data we have
        if(playerObj.data.hexColor == null) {
            colorSelect.SetActive(true);
            colorFirstSelection.Select();
        // } else if(##kart selected##) {
        //     kartSelect.SetActive(true);
        //     kartFirstSelection.Select();
        } else if(!playerObj.data.ready) {
            readyButton.gameObject.SetActive(true);
            readyButton.Select();
        } else {
            readyText.SetActive(true);
        }
    }

    /** Update the visuals to reflect what the player has selected in playerObj#data */
    public void UpdateVisuals() 
    {
        titleText.text = playerObj.data.name;
        GetComponent<Image>().color = playerObj.data.hexColor != null ? HexToColor(playerObj.data.hexColor) : origPanelColor;
    }

    public void SetPlayerObject(PlayerObject obj) 
    { 
        playerObj = obj; 
        playerObj.input.onActionTriggered += ActionTriggered;  

        toolTips.ForEach(tt => tt.GetComponent<ToolTip>().SetObservedInput(obj.input));
    }

    public void SetColor(string hexColor) 
    {
        playerObj.data.hexColor = hexColor;
        UpdatePanel();
    }

    public void SetReady() 
    {
        playerObj.data.ready = true;
        UpdatePanel();
        GetComponentInParent<PlayerMenuController>().CheckReady();
    }

    public Color HexToColor(string hex)
    {
        // Remove the '#' character if present
        hex = hex.Replace("#", "");

        // Parse the hex value into a 32-bit integer
        uint hexValue = uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);

        // Extract individual color channels
        byte r = (byte)((hexValue >> 16) & 255);
        byte g = (byte)((hexValue >> 8) & 255);
        byte b = (byte)(hexValue & 255);

        // Create and return the Color object
        return new Color32(r, g, b, 255);
    }

}
