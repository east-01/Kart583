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
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MENU_TITLE, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

}
