using System;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/** The PlayerPanelController is responsible for a single PlayerObject */
public class PlayerPanelController : MenuController
{
    public static readonly string NAME_SELECT_MENU_ID = "NameSelect";
    public static readonly string COLOR_SELECT_MENU_ID = "ColorSelect"; 
    public static readonly string KART_SELECT_MENU_ID = "KartSelect";
    public static readonly string READY_MENU_ID = "ReadyMenu";

    [SerializeField] private TMP_Text titleText;

    [SerializeField] private PlayerBuildPhase phase;
    private readonly Dictionary<PlayerBuildPhase, string> phaseSubMenu = new() {
        { PlayerBuildPhase.COLOR_SELECT, COLOR_SELECT_MENU_ID },
        { PlayerBuildPhase.NAME_SELECT, NAME_SELECT_MENU_ID },
        { PlayerBuildPhase.VEHICLE_SELECT, KART_SELECT_MENU_ID },
        { PlayerBuildPhase.WAITING_FOR_READY, READY_MENU_ID },
        { PlayerBuildPhase.READY, READY_MENU_ID }
    };

    private Color origPanelColor; // Stored so we can revert to it if the player revert's their color selection

    protected new void Awake() 
    {
        base.Awake();

        origPanelColor = GetComponent<Image>().color;
    }

    /** Shortcut for UpdateBuildPhase() & UpdateVisuals(). UpdateBuildPhase is called first. */
    public void UpdatePanel() { UpdateBuildPhase(); UpdateVisuals(); }

    /** Update the current selection so it reflects what stage we're at in player construction. 
        The order is: Name -> Color -> Kart -> Ready */
    public void UpdateBuildPhase() 
    {
        PlayerBuildPhase prePhase = phase;
        PlayerObject currentFocus = GetSubMenu(phaseSubMenu[phase]).FocusedPlayer;
        if(currentFocus == null)
            currentFocus = FocusedPlayer;

        // Check player data and enable the corresponding phase
        if(focusedPlayer.data.name.Length == 0) {
            phase = PlayerBuildPhase.NAME_SELECT;
        } else if(focusedPlayer.data.hexColor == null) {
            phase = PlayerBuildPhase.COLOR_SELECT;
        } else if(focusedPlayer.data.kartType == KartType.NONE) {
            phase = PlayerBuildPhase.VEHICLE_SELECT;
        } else if(!focusedPlayer.data.ready) {
            phase = PlayerBuildPhase.WAITING_FOR_READY;
        } else {
            phase = PlayerBuildPhase.READY;
        }
        OpenSubMenu(phaseSubMenu[phase], currentFocus);
    }

    public void RegressBuildPhase() 
    {
        if(focusedPlayer.data.ready) {
            focusedPlayer.data.ready = false;
        } else if(focusedPlayer.data.kartType != KartType.NONE) {
            focusedPlayer.data.kartType = KartType.NONE;
        } else if(focusedPlayer.data.hexColor != null) {
            focusedPlayer.data.hexColor = null;
        } else if(focusedPlayer.data.name.Length > 0) {
            focusedPlayer.data.name = "";
        } else if(focusedPlayer.data.name == "") {
            MenuPlayerController mpc = FindObjectOfType<MenuPlayerController>();
            mpc.RemovePanel(focusedPlayer, focusedPlayer.PlayerIndex != 0);
            return;
        }
        UpdatePanel();
    }

    /** Update the visuals to reflect what the player has selected in playerObj#data */
    public void UpdateVisuals() 
    {
        titleText.text = focusedPlayer.data.name;
        GetComponent<Image>().color = focusedPlayer.data.hexColor != null ? HexToColor(focusedPlayer.data.hexColor) : origPanelColor;
    }

    public new void Open(PlayerObject focusedPlayer = null) 
    { 
        base.Open(focusedPlayer);
        // Set player to unready in case it's set as ready
        this.focusedPlayer.data.ready = false;

        UpdatePanel();
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

    public PlayerBuildPhase PlayerBuildPhase => phase;

}

public class PlayerPanelControllerSubMenu : MenuController 
{
    private UIElementSounds uiElementSounds;

    public PlayerPanelController PlayerPanelController { get { return parentMenu as PlayerPanelController; } }

    protected new void Awake() 
    {
        base.Awake();
        uiElementSounds = GetComponentInParent<UIElementSounds>();
    }

    protected override void SendMenuBack()
    {
        PlayerPanelController.RegressBuildPhase();
        uiElementSounds.PlayBackSound();
        Close();
    }
}

public enum PlayerBuildPhase 
{
    NAME_SELECT, COLOR_SELECT, VEHICLE_SELECT, WAITING_FOR_READY, READY
}