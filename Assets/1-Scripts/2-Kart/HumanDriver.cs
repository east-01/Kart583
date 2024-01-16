using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/** Human Driver script is the layer that connects the Input System
      to the KartController. */
public class HumanDriver : KartBehavior
{

    private PlayerInput input;

    void Update() 
    {
        if(posTracker.raceCompletion >= 1) {
            kartManager.UseBotDriver();
        }
    }

    void OnEnable() 
    {
        if(input != null) input.onActionTriggered += ActionTriggered; // Re-connect the actions if we lose them
    }

    void OnDisable() 
    {
        input.onActionTriggered += ActionTriggered;
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
            default:
                break;
        }
    }

}
