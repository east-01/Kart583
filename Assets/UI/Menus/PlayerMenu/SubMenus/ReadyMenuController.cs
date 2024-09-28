using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadyMenuController : PlayerPanelControllerSubMenu
{
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyText;

    protected override void Opened() 
    {
        readyButton.gameObject.SetActive(PlayerPanelController.PlayerBuildPhase == PlayerBuildPhase.WAITING_FOR_READY);
        readyText.gameObject.SetActive(PlayerPanelController.PlayerBuildPhase == PlayerBuildPhase.READY);
    }

    /** This method is called by the ready button */
    public void SetReady() 
    {
        if(focusedPlayer.PlayerIndex == 0)
            focusedPlayer.data.SaveToPlayerPrefs(PlayerData.PLAYER_1_DATA);

        focusedPlayer.data.ready = true;
        PlayerPanelController.UpdateBuildPhase();

        parentMenu.GetComponentInParent<MenuPlayerController>().CheckReady();
    }
}
