using System;
using EMullen.MenuController;
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
        CoreManager.OptionsMenuController.SetParentMenuController(this);
        CoreManager.OptionsMenuController.Open();
    }

    public void ClickedQuit() 
    {
        Application.Quit();
    }

    protected override void SendMenuBack() {}
}
