using System;
using System.Collections.Generic;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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

    /** Update the current selection so it reflects what stage we're at in player construction. 
        The order is: Name -> Color -> Kart -> Ready */
    public void UpdateBuildPhase() 
    {
        // Get the sub menus current focus, if its null (this happens when the menu is first opened) set the current focus as this menus focus
        PlayerObject currentFocus = FocusedPlayerIncludingChildren;
        if(currentFocus == null)
            currentFocus = FocusedPlayer;

        // Check player data and enable the corresponding phase
        if(currentFocus.data.name.Length == 0) {
            BLog.Highlight("A");
            phase = PlayerBuildPhase.NAME_SELECT;
        } else if(currentFocus.data.hexColor == null) {
            BLog.Highlight("B");
            phase = PlayerBuildPhase.COLOR_SELECT;
        } else if(currentFocus.data.kartType == KartType.NONE) {
            BLog.Highlight("C");
            phase = PlayerBuildPhase.VEHICLE_SELECT;
        } else if(!currentFocus.data.ready) {
            BLog.Highlight("D");
            phase = PlayerBuildPhase.WAITING_FOR_READY;
        } else {
            BLog.Highlight("E");
            phase = PlayerBuildPhase.READY;
        }
        OpenSubMenu(phaseSubMenu[phase], currentFocus);
    }

    public void RegressBuildPhase() 
    {
        PlayerObject focus = FocusedPlayerIncludingChildren;

        void RemoveSelf() 
        {
            MenuPlayerController mpc = FindObjectOfType<MenuPlayerController>();
            mpc.RemovePanel(focus, focus.PlayerIndex != 0);
        }

        if(focus.data.ready) {
            BLog.Highlight("1");
            focus.data.ready = false;
        } else if(focus.data.kartType != KartType.NONE) {
            BLog.Highlight("2");
            focus.data.kartType = KartType.NONE;
        } else if(focus.data.hexColor != null) {
            BLog.Highlight("3");
            focus.data.hexColor = null;
        } else if(focus.data.name.Length > 0) {
            BLog.Highlight("4");
            if(focus.input.currentControlScheme == "Gamepad") {
                RemoveSelf();
                return;
            }

            focus.data.name = "";
        } else if(focus.data.name == "") {
            BLog.Highlight("5");
            RemoveSelf();
            return;
        }
        BLog.Highlight("6");
        UpdateBuildPhase();
    }

    /** Update the visuals to reflect what the player has selected in playerObj#data */
    public void UpdateVisuals() 
    {  
        if(focusedPlayer == null)
            return;
    }

    public new void Open(PlayerObject focusedPlayer = null) 
    { 
        base.Open(focusedPlayer);

        // Reset ready state so we don't automatically ready up the player when they open
        this.focusedPlayer.data.ready = false;

        // Visuals
        titleText.text = this.focusedPlayer.data.name;
        SetPanelColor(focusedPlayer.data.hexColor);

        UpdateBuildPhase();
    }

    public void SetPanelColor(string color) 
    {
        GetComponent<Image>().color = color == null || color.Length == 0 ? origPanelColor : HexToColor(color);
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
    protected UIElementSounds uiElementSounds;

    public PlayerPanelController PlayerPanelController { get { return parentMenu as PlayerPanelController; } }

    protected new void Awake() 
    {
        base.Awake();
        uiElementSounds = GetComponentInParent<UIElementSounds>();
    }

    protected override void SendMenuBack()
    {
        BLog.Highlight("ppcsm override");
        PlayerPanelController.RegressBuildPhase();
        uiElementSounds.PlayBackSound();
        if(PlayerPanelController.PlayerBuildPhase != PlayerBuildPhase.WAITING_FOR_READY)
            Close();
    }
}

public enum PlayerBuildPhase 
{
    NAME_SELECT, COLOR_SELECT, VEHICLE_SELECT, WAITING_FOR_READY, READY
}