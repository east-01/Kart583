using System.Collections;
using System.Collections.Generic;
using EMullen.PlayerMgmt;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class NameSelectController : PlayerPanelControllerSubMenu
{
    [SerializeField] private TMP_InputField nameInputField;

    protected override void Opened() 
    {
        // TODO: Add on screen keyboard for name select
        if(focusedPlayer.Input.currentControlScheme == "Gamepad") {
            PlayerData playerData = focusedPlayer.GetPlayerData();
            if(!playerData.HasData<PlayerDisplayData>())
                playerData.SetData<PlayerDisplayData>(new());
            PlayerDisplayData pdd = playerData.GetData<PlayerDisplayData>();
            pdd.name = "Player " + (focusedPlayer.Input.playerIndex+1);
            playerData.SetData(pdd);
            PlayerPanelController.UpdateBuildPhase();
        }
    }

    /* This method is called by the TextInputField, set in inspector */
    public void SubmitText() 
    {
        PlayerData playerData = focusedPlayer.GetPlayerData();
        if(!playerData.HasData<PlayerDisplayData>())
            playerData.SetData<PlayerDisplayData>(new());
        PlayerDisplayData pdd = playerData.GetData<PlayerDisplayData>();
        pdd.name = nameInputField.text;
        playerData.SetData(pdd);
        PlayerPanelController.UpdateBuildPhase();
    }
}
