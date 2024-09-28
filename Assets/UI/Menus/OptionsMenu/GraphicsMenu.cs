using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsMenu : OptionsSubMenu
{
    public override void LoadOptions()
    {
        SetScreenMode((FullScreenMode)PlayerPrefs.GetInt("FullScreenMode", (int)FullScreenMode.Windowed));
    }

    public override void SaveOptions()
    {
        PlayerPrefs.SetInt("FullScreenMode", (int)Screen.fullScreenMode);
    }

    public void SetScreenMode(FullScreenMode fsm) => Screen.fullScreenMode = fsm;
    /// <summary> Called by dropdown menu, adapts the dropdown index to a true FullScreenMode enum </summary>
    public void SetConvertedScreenMode(int dropdownIndex) 
    {
        switch(dropdownIndex) {
            case 0:
                SetScreenMode(Application.platform.ToString().StartsWith("OSX") ? FullScreenMode.MaximizedWindow : FullScreenMode.ExclusiveFullScreen);
                break;
            case 1:
                SetScreenMode(FullScreenMode.FullScreenWindow);
                break;
            case 2:
                SetScreenMode(FullScreenMode.Windowed);
                break;
        }
    }
}
