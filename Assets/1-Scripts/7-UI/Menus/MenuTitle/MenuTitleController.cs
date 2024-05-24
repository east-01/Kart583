using System;
using UnityEngine;
using UnityEngine.UI;

public class MenuTitleController : MenuController
{
    public TitleShipFlight titleShip;

    public void ClickedStart(bool isMultiplayer) 
    {
        CoreManager.Instance.isMultiplayer = isMultiplayer;
        CoreManager.TransitionManager.LoadScene(SceneNames.MENU_PLAYER);
    }

    public void ClickedOptions() 
    {
        print("TODO: Create options menu");
        // SceneManager.LoadScene("OptionsMenu");
    }

    public void ClickedQuit() 
    {
        Application.Quit();
    }
}
