using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/** Human Driver script is the layer that connects the Input System
      to the KartController. */
public class HumanDriver : MonoBehaviour
{

    private KartController kc;

    private void Awake() 
    {
        kc = GetComponent<KartController>();
    }

    public void OnTurn(InputAction.CallbackContext context) 
    {
        kc.SetTurnInput(context.ReadValue<Vector2>());
    }

    public void OnThrottle(InputAction.CallbackContext context) 
    {
        kc.SetThrottleInput(context.ReadValue<float>());
    }

    public void OnReverse(InputAction.CallbackContext context) 
    {
        kc.SetReverseInput(context.ReadValue<float>());
    }

	public void OnDrift(InputAction.CallbackContext context) { 
        if(!context.performed && !context.canceled) return;        
        kc.SetDriftInput(context.performed);
	}

    public void OnBoost(InputAction.CallbackContext context) 
    {
        if(!context.performed && !context.canceled) return;
        kc.SetBoostInput(context.performed);
    }

}
