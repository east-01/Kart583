using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/** Human Driver script is the layer that connects the Input System
      to the KartController. */
public class HumanDriver : KartBehavior
{

    void Update() 
    {
        if(posTracker.raceCompletion >= 1) {
            botPath.enabled = true;
            botDriver.enabled = true;
            humanDriver.enabled = false;
        }
    }

    public void OnTurn(InputAction.CallbackContext context) 
    {
        kartCtrl.TurnInput = context.ReadValue<Vector2>();
    }

    public void OnThrottle(InputAction.CallbackContext context) 
    {
        kartCtrl.ThrottleInput = context.ReadValue<float>();
    }

    public void OnReverse(InputAction.CallbackContext context) 
    {
        kartCtrl.ThrottleInput = -context.ReadValue<float>();
    }

	public void OnDrift(InputAction.CallbackContext context) { 
        if(!context.performed && !context.canceled) return;        
        kartCtrl.DriftInput = context.performed;
	}

    public void OnBoost(InputAction.CallbackContext context) 
    {
        if(!context.performed && !context.canceled) return;
        kartCtrl.BoostInput = context.performed;
    }

}
