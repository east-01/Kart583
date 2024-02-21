using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** The PlayerPanelController is responsible for a single PlayerObject */
public class PlayerPanelController : MonoBehaviour
{
    [SerializeField] float phaseChangeCooldown = 1f;

    [SerializeField] TMP_Text titleText;
    [SerializeField] List<GameObject> toolTips;
    
    [SerializeField] GameObject colorSelect;
    [SerializeField] Button colorFirstSelection;
    [SerializeField] GameObject kartSelect;
    [SerializeField] Button readyButton;
    [SerializeField] GameObject readyText;

    private PlayerObject playerObj;
    private PlayerControls controlsReference;

    private PlayerBuildPhase phase;
    private float lastPhaseChangeTime;
    private Color origPanelColor; // Stored so we can revert to it if the player revert's their color selection

    private void Awake() 
    {
        // DO NOT ENABLE, WE USE THE PLAYERINPUT COMPONENT HERE
        // This is so we can have consistent name references in ActionTriggered
        controlsReference = new PlayerControls();

        phase = PlayerBuildPhase.COLOR_SELECT;
        origPanelColor = GetComponent<Image>().color;
    }

    private void OnDisable() 
    {
        playerObj.input.onActionTriggered -= ActionTriggered; // Event registered in SetPlayerObject
    }

    public void ActionTriggered(InputAction.CallbackContext context) {
        if(context.performed && context.action.name == controlsReference.UI.Cancel.name) {
            // We have to reverse what is done in UpdateBuildPhase() to see what needs to be taken off first.
            // This will be the opposite order as specified in UpdateBuildPhase()
            if(playerObj.data.ready) {
                playerObj.data.ready = false;
            } else if(playerObj.data.kartType != KartType.NONE) {
                playerObj.data.kartType = KartType.NONE;
            } else if(playerObj.data.hexColor != null) {
                playerObj.data.hexColor = null;
            }
            UpdatePanel();
        }

        if(phase == PlayerBuildPhase.VEHICLE_SELECT) {
            kartSelect.GetComponent<KartSelectController>().HandleInputAction(context);
        }
    }

    /** Shortcut for UpdateBuildPhase() & UpdateVisuals(). UpdateBuildPhase is called first. */
    public void UpdatePanel() { UpdateBuildPhase(); UpdateVisuals(); }

    /** Update the current selection so it reflects what stage we're at in player construction. 
        The order is: Color -> Kart -> Ready */
    public void UpdateBuildPhase() 
    {
        // Disable everything so we can enable only what we want
        List<GameObject> everything = new() {colorSelect, kartSelect, readyButton.gameObject, readyText};
        everything.ForEach(go => go.SetActive(false));

        // Enable correct thing based on what data we have
        if(playerObj.data.hexColor == null) {
            phase = PlayerBuildPhase.COLOR_SELECT;
            colorSelect.SetActive(true);
            colorFirstSelection.Select();
        } else if(playerObj.data.kartType == KartType.NONE) {
            phase = PlayerBuildPhase.VEHICLE_SELECT;
            kartSelect.SetActive(true);
        } else if(!playerObj.data.ready) {
            phase = PlayerBuildPhase.WAITING_FOR_READY;
            readyButton.gameObject.SetActive(true);
            readyButton.Select();
        } else {
            phase = PlayerBuildPhase.READY;
            readyText.SetActive(true);
        }

        lastPhaseChangeTime = Time.time;
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

    /** This method is called by each color select button, fields set in editor. */
    public void SetColor(string hexColor) 
    {
        if(Time.time <= lastPhaseChangeTime + phaseChangeCooldown) return;

        playerObj.data.hexColor = hexColor;
        UpdatePanel();
    }

    /** This method is called by the KartSelectController */
    public void SetKartName(KartType kartName) {
        if(Time.time <= lastPhaseChangeTime + phaseChangeCooldown) return;

        playerObj.data.kartType = kartName;
        UpdatePanel();
    }

    /** This method is called by the ready button */
    public void SetReady() 
    {
        if(Time.time <= lastPhaseChangeTime + phaseChangeCooldown) return;

        playerObj.data.ready = true;
        UpdatePanel();

        GetComponentInParent<MenuPlayerController>().CheckReady();
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

    public PlayerObject PlayerObject { get { return playerObj; } }

}

public enum PlayerBuildPhase 
{
    COLOR_SELECT, VEHICLE_SELECT, WAITING_FOR_READY, READY
}
