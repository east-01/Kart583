using System.Collections;
using System.Collections.Generic;
using EMullen.PlayerMgmt;
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
        PlayerData playerData = focusedPlayer.GetPlayerData();
        if(!playerData.HasData<PlayerDisplayData>())
            playerData.SetData<PlayerDisplayData>(new());
        RaceData rd = playerData.GetData<RaceData>();
        rd.ready = true;
        playerData.SetData(rd);

        PlayerPanelController.UpdateBuildPhase();

        parentMenu.GetComponentInParent<MenuPlayerController>().CheckReady();
    }
}
