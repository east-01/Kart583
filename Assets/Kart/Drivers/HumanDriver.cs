using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/** Human Driver script is the layer that connects the Input System
      to the KartController. */
public class HumanDriver : KartBehavior, GameplayManagerBehavior
{

    private GameplayManager gameplayManager;
    private KartLevelManager kartLevelManager;

    private PlayerInput input;

    new protected void Awake() 
    {
        base.Awake();
        CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
    }

    private void OnDisable() 
    {
        input.onActionTriggered -= ActionTriggered;
    }

    public void GameplayManagerLoaded(GameplayManager gameplayManager)
    {
        this.gameplayManager = gameplayManager;
        this.kartLevelManager = gameplayManager.KartLevelManager;
    }

    private void Update() 
    {
        if(posTracker.RaceCompletion >= 1 && kartManager.IsHuman) {
            kartManager.UseBotDriver(kartManager.OwnerUID);
        }
    }

    public void ConnectPlayerInput(PlayerInput input) 
    {
        this.input = input;
        this.input.onActionTriggered += ActionTriggered;
        this.input.SwitchCurrentActionMap("Gameplay");
    }

    public void ActionTriggered(InputAction.CallbackContext context) 
    {
        if(!context.performed && !context.canceled) return;
        switch(context.action.name) {
            case "Turn":
                kartCtrl.TurnInput = context.ReadValue<Vector2>();
                break;
            case "Throttle":
                kartCtrl.ThrottleInput = context.ReadValue<float>();
                break;
            case "Reverse":
                kartCtrl.ThrottleInput = -context.ReadValue<float>();
                break;
            case "Drift":
                kartCtrl.DriftInput = context.performed;
                break;
            case "Boost":
                kartCtrl.BoostInput = context.performed;
                break;
            case "Item":
                kartItemManager.PerformItemInput(context.performed);
                break;
            case "Pause":
                if(!context.performed)
                    kartLevelManager.RaceCamera.igScreenMenuController.SetPauseOpen(true);
                break;
            default:
                break;
        }
    }

}
