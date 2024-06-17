using UnityEngine;

/// <summary>
/// The InGameMenuController doesn't really have anything going on in it's default state.
/// It's just a delegate for the various menus that will pop up over the entire screen
///   once in game (i.e. pause menu and results menu).
/// </summary>
public class IGScreenMenuController : MenuController
{

	public static readonly string RESULTS_MENU_ID = "ResultsMenu";
	public static readonly string PAUSE_MENU_ID = "PauseMenu";

	public void SetPauseOpen(bool open) 
	{
		if(open) {
			OpenSubMenu(PAUSE_MENU_ID, focusedPlayer);
		} else
			PauseMenuController.Close();
	}

	public ResultsMenuController ResultsMenuController { get {
		return GetSubMenu(RESULTS_MENU_ID) as ResultsMenuController;
	} }

	public PauseMenuController PauseMenuController { get {
		return GetSubMenu(PAUSE_MENU_ID) as PauseMenuController;
	} }

}