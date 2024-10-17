using System.Collections;
using System.Collections.Generic;
using EMullen.MenuController;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
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
        LocalPlayer cachedFocus = FocusedPlayer;
        Close();

        OptionsMenuController.Instance.ParentMenu = this;
        OptionsMenuController.Instance.Open(cachedFocus);
    }

    public void QuitPressed() 
    {
        LobbyCommunicator.Instance.StopCommunication();
        SceneController.Instance.LoadScene(new(SceneNames.MENU_TITLE), false);
    }

}
