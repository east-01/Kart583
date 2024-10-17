using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.MenuController;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Is a host for all OptionsSubMenus, which get pulled up from "tabs" (buttons)
///   in the 
/// </summary>
public class OptionsMenuController : MenuController
{
    
    public static OptionsMenuController Instance { get; private set; }

    public static readonly string GRAPHICS_OPTIONS_MENU_ID = "GraphicsOptions";
    public static readonly string VOLUME_OPTIONS_MENU_ID = "VolumeOptions";

    protected new void Awake() 
    {
        if(Instance != null) {
            Debug.LogError($"Singleton problem for OptionsMenuController deleting gameObject \"{gameObject.name}\"");
            Destroy(gameObject);
            return;
        }

        base.Awake();

        Instance = this;
        LoadOptions();
    }

    protected new void OnDestroy() 
    {
        base.OnDestroy();
        SaveOptions();
    }

#region Save/Load
    public void LoadOptions() 
    {
        subMenus.ForEach(sm => (GetSubMenu(sm.id) as OptionsSubMenu).LoadOptions());
    }
    
    public void SaveOptions() 
    {
        if(!AreSubmenusCached)
            return;
        subMenus.ForEach(sm => (GetSubMenu(sm.id) as OptionsSubMenu).SaveOptions());
        PlayerPrefs.Save();
    }
    #endregion

    protected override void Opened()
    {
        base.Opened();
        GetSubMenu(GRAPHICS_OPTIONS_MENU_ID).Open(FocusedPlayer);
    }

    /// <summary>
    /// Used by tab buttons to open each sub menu
    /// </summary>
    public void OpenOptionsSubMenu(string id) => GetSubMenu(id).Open(FocusedPlayer);
}

public abstract class OptionsSubMenu : MenuController {
    [SerializeField] private Button tabButton;
    public abstract void SaveOptions();
    public abstract void LoadOptions();
    protected override void Opened()
    {
        base.Opened();
        tabButton.enabled = false;
    }
    protected override void Closed()
    {
        base.Closed();
        tabButton.enabled = true;
    }
    public override void SendMenuBack()
    {
        ParentMenu.SendMenuBack();
    }
}

[Serializable]
public struct ChannelData {
    /// <summary>
    /// The label for the mixer channel, must correspond with an exposed variable in the mixer
    /// </summary>
    public string label;
    public Slider slider;
}