using System;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;

/** This class is responsible for managing splitscreen behaviour
      as well as the full-screen HUD. 
      
    This class will negotiate the laying out of spliscreen views in
      the future, but for now will be responsible for showing results. */
public class ScreenManager : MonoBehaviour, GameplayManagerBehavior
{

	[SerializeField] 
	private GameObject continueTooltip;

	private GameplayManager gameplayManager;
	public ResultsBuilder ResultsBuilder;

	private PlayerControls controlsReference;
	private PlayerObject currentPlayer; // The player that has control over screen. Set by ConnectPlayerInput

	private void Awake() 
	{
		SceneDelegate.Instance.SubscribeForGameplayManager(this);
	}

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
    }

	void OnDisable() 
	{
		if(currentPlayer != null)
			currentPlayer.input.onActionTriggered -= ActionTriggered;
	}

	/** Connect a player object's input to the screen */
	public void ConnectPlayerInput(PlayerObject obj) 
	{
		if(currentPlayer != null) 
			throw new InvalidOperationException("Can't connect a new player object since one is already connected!");

		controlsReference = new PlayerControls();

		currentPlayer = obj;
		currentPlayer.input.onActionTriggered += ActionTriggered;
		currentPlayer.input.SwitchCurrentActionMap("UI");

		continueTooltip.GetComponent<ToolTip>().SetObservedInput(currentPlayer.input);
	}

	void ActionTriggered(InputAction.CallbackContext context) 
	{
		if(gameplayManager == null) {
			Debug.LogError("Tried to perfom InputAction on ScreenManager when gameplayManager is null!");
			return;
		}
		if(context.performed && context.action.name == controlsReference.UI.Submit.name) {
			if(gameplayManager.HasLobby) {
				SceneDelegate.Instance.MoveClientToLobby();
			} else {
				GameObject tmo = GameObject.Find("TransitionManager");
				tmo.GetComponent<TransitionManager>().LoadScene(SceneNames.MENU_MAP);
			}
        }
	}

}