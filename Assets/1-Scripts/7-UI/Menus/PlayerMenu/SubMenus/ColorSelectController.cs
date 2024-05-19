using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorSelectController : PlayerPanelControllerSubMenu
{
    /** This method is called by each color select button, fields set in editor. */
    public void SetColor(string hexColor) 
    {
        focusedPlayer.data.hexColor = hexColor;
        PlayerPanelController.UpdatePanel();
    }
}
