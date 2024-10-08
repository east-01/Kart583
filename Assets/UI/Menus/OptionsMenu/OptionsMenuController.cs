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
    
    public static readonly string GRAPHICS_OPTIONS_MENU_ID = "GraphicsOptions";
    public static readonly string VOLUME_OPTIONS_MENU_ID = "VolumeOptions";

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
        OpenSubMenu(GRAPHICS_OPTIONS_MENU_ID);
    }

    public void OpenOptionsSubMenu(string id) => OpenSubMenu(id);

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
    protected override void SendMenuBack()
    {
        parentMenu.SendMenuBackPublic();
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