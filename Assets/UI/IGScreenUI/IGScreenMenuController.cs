using EMullen.MenuController;
using EMullen.Networking;
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

	private void Update() 
	{
		// bool shouldShowResultsMenu = PlayerManager.Instance.Players.ForEach().Any(x => x.)
		if(!LobbyCommunicator.Instance.InLobby)
			return;

		// TODO: This code only really needs to run when any of the local players' data changes.
		// TODO: shouldShowResults menu should be based off of all local players completing race so we can see pending players.
		bool shouldShowResultsMenu = LobbyCommunicator.Instance.LobbyData.Value.stateTypeString == nameof(PostRaceState);
		if(shouldShowResultsMenu && !ResultsMenuController.IsOpen) {
			OpenSubMenu(RESULTS_MENU_ID);
		} else if(!shouldShowResultsMenu && ResultsMenuController.IsOpen) {
			ResultsMenuController.Close();
		}
	}

	public void SetPauseOpen(bool open) 
	{
		if(open) {
			OpenSubMenu(PAUSE_MENU_ID, focusedPlayer);
		} else
			PauseMenuController.Close();
	}

    public ResultsMenuController ResultsMenuController => GetSubMenu(RESULTS_MENU_ID) as ResultsMenuController;
	public PauseMenuController PauseMenuController => GetSubMenu(PAUSE_MENU_ID) as PauseMenuController;

}