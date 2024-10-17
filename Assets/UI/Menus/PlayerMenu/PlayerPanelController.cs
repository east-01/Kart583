using System;
using System.Collections.Generic;
using System.Data;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using Unity.VisualScripting;
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
        LocalPlayer currentFocus = FocusedPlayerIncludingChildren;
        if(currentFocus == null)
            currentFocus = FocusedPlayer;

        if(!PlayerDataRegistry.Instance.Contains(currentFocus.UID)) {
            Debug.LogError("Can't update build phase, the focused player's uid isn't in the PlayerDataRegistry.");
            return;
        }

        PlayerData data = PlayerDataRegistry.Instance.GetPlayerData(currentFocus.UID);
        PlayerDisplayData displayData = data.GetData<PlayerDisplayData>();
        RaceData raceData = data.GetData<RaceData>();

        // Check player data and enable the corresponding phase
        if(displayData.name.Length == 0) {
            phase = PlayerBuildPhase.NAME_SELECT;
        } else if(displayData.hexColor == null) {
            phase = PlayerBuildPhase.COLOR_SELECT;
        } else if(raceData.kartType == KartType.NONE) {
            phase = PlayerBuildPhase.VEHICLE_SELECT;
        } else if(!raceData.ready) {
            phase = PlayerBuildPhase.WAITING_FOR_READY;
        } else {
            phase = PlayerBuildPhase.READY;
        }
        GetSubMenu(phaseSubMenu[phase]).Open(currentFocus);
    }

    public void RegressBuildPhase() 
    {
        LocalPlayer focus = FocusedPlayerIncludingChildren;

        if(!PlayerDataRegistry.Instance.Contains(focus.UID)) {
            Debug.LogError("Can't regress build phase, the focused player's uid isn't in the PlayerDataRegistry.");
            return;
        }

        PlayerData data = PlayerDataRegistry.Instance.GetPlayerData(focus.UID);
        PlayerDisplayData displayData = data.GetData<PlayerDisplayData>();
        RaceData raceData = data.GetData<RaceData>();

        void RemoveSelf() 
        {
            MenuPlayerController mpc = FindObjectOfType<MenuPlayerController>();
            mpc.RemovePanel(focus, focus.Input.playerIndex != 0);
        }

        if(raceData.ready) {
            raceData.ready = false;
        } else if(raceData.kartType != KartType.NONE) {
            raceData.kartType = KartType.NONE;
        } else if(displayData.hexColor != null) {
            displayData.hexColor = null;
        } else if(displayData.name.Length > 0) {
            if(focus.Input.currentControlScheme == "Gamepad") {
                RemoveSelf();
                return;
            }

            displayData.name = "";
        } else if(displayData.name == "") {
            RemoveSelf();
            return;
        }
        UpdateBuildPhase();
    }

    /** Update the visuals to reflect what the player has selected in playerObj#data */
    public void UpdateVisuals() 
    {  
        if(FocusedPlayerIncludingChildren == null) {
            Debug.LogError("Can't UpdateVisuals, FocusedPlayer is null.");
            return;
        }

        PlayerData data = FocusedPlayerIncludingChildren.GetPlayerData();

        // Reset ready state so we don't automatically ready up the player when they open the panel
        RaceData rd = data.GetData<RaceData>();
        rd.ready = false;
        data.SetData(rd);

        // Visuals
        PlayerDisplayData pdd = data.GetData<PlayerDisplayData>();
        titleText.text = pdd.name;
        SetPanelColor(pdd.hexColor);
        BLog.Highlight($"Set name as \"{pdd.name}\" and panel color as \"{pdd.hexColor}\"");
    }

    public new void Open(LocalPlayer focusedPlayer = null) 
    { 
        if(focusedPlayer == null) {
            Debug.LogError("Can't open PlayerPanelController with a null focus.");
            return;
        }

        // Prepare player's data, this has to happen before base.Open
        PlayerData data = focusedPlayer.GetPlayerData();
        if(!data.HasData<RaceData>())
            data.SetData<RaceData>(new());
        
        if(!data.HasData<PlayerDisplayData>())
            data.SetData<PlayerDisplayData>(new());

        base.Open(focusedPlayer);

        if(FocusedPlayer == null) {
            Debug.LogError("Failed to open PlayerPanel, the FocusedPlayer didn't attach.");
            return;
        }

        if(!FocusedPlayer.HasPlayerData()) {
            Debug.LogError("Can't open PlayerPanelController they have no playerdata.");
            return;
        }

        UpdateVisuals();
        UpdateBuildPhase();
    }

    protected override void Opened() 
    {
        base.Opened();
        // UpdateBuildPhase();
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

    public PlayerPanelController PlayerPanelController { get { return ParentMenu as PlayerPanelController; } }

    protected new void Awake() 
    {
        base.Awake();
        uiElementSounds = GetComponentInParent<UIElementSounds>();
    }

    public override void SendMenuBack()
    {
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