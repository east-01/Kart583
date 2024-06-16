using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuController : MenuController
{
    private void Update() 
    {

    }

    public void ResumePressed() 
    {
        SendMenuBack();
    }

    public void OptionsPressed() 
    {
        PlayerObject cachedFocus = focusedPlayer;
        Close();
        CoreManager.OptionsMenuController.SetParentMenuController(this);
        CoreManager.OptionsMenuController.Open(cachedFocus);
    }

    public void QuitPressed() 
    {
        CoreManager.LobbyCommunicator.StopCommunication();
        SceneController.Instance.LoadScene(new(SceneNames.MENU_TITLE), false);
    }

}
