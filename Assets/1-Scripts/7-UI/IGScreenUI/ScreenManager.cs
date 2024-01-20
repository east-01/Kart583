using System;
using UnityEngine;
using UnityEngine.InputSystem;

/** This class is responsible for managing splitscreen behaviour
      as well as the full-screen HUD. 
      
    This class will negotiate the laying out of spliscreen views in
      the future, but for now will be responsible for showing results. */
public class ScreenManager : MonoBehaviour
{

	[SerializeField] GameObject continueTooltip;

	public ResultsBuilder ResultsBuilder;

	private PlayerControls controlsReference;
	private PlayerObject currentPlayer; // The player that has control over screen. Set by ConnectPlayerInput

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
		if(context.performed && context.action.name == controlsReference.UI.Submit.name) {
			GameObject tmo = GameObject.Find("TransitionManager");
			tmo.GetComponent<TransitionManager>().LoadScene("MapSelect");
        }
	}

}