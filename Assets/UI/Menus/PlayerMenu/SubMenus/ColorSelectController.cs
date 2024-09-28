using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.UI;

public class ColorSelectController : PlayerPanelControllerSubMenu
{
    protected override void Opened() 
    {
        MultiplayerEventSystem mes = parentMenu.GetComponent<MultiplayerEventSystem>();
        mes.SetSelectedGameObject(firstSelect.gameObject);
    }

    /** This method is called by each color select button, fields set in editor. */
    public void SetColor(string hexColor) 
    {
        focusedPlayer.data.hexColor = hexColor;
        PlayerPanelController.SetPanelColor(hexColor);
        PlayerPanelController.UpdateBuildPhase();
    }
}
