using System.Collections;
using System.Collections.Generic;
using EMullen.PlayerMgmt;
using UnityEngine;
using UnityEngine.InputSystem.UI;

public class ColorSelectController : PlayerPanelControllerSubMenu
{
    protected override void Opened() 
    {
        MultiplayerEventSystem mes = ParentMenu.GetComponent<MultiplayerEventSystem>();
        mes.SetSelectedGameObject(firstSelect.gameObject);
    }

    /** This method is called by each color select button, fields set in editor. */
    public void SetColor(string hexColor) 
    {
        PlayerData playerData = FocusedPlayer.GetPlayerData();
        if(!playerData.HasData<PlayerDisplayData>())
            playerData.SetData<PlayerDisplayData>(new());
        PlayerDisplayData pdd = playerData.GetData<PlayerDisplayData>();
        pdd.hexColor = hexColor;
        playerData.SetData(pdd);

        PlayerPanelController.SetPanelColor(hexColor);
        PlayerPanelController.UpdateBuildPhase();
    }
}
