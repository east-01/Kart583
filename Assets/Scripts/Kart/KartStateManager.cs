using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Manages the kart's state by two ways: 
      1. Active state changes: The state is changed by player
           input/callback, usually leading to a request flag
		   being set.
	  2. Passive state changes: These state changes usually
		   happen when the player ISN'T performing input. */
public class KartStateManager : MonoBehaviour
{

	private KartState _state;
	[SerializeField] 
	public KartState state { 
		get { return _state; } 
		set { 
			// Triggered state change, possibly throw an event
			_state = value; 
			GetComponent<KartController>().StateChanged(value);
		} 
	}

	private PlayerControls controls;

    void Start()
    {
        
		if(controls != null) controls.Disable(); // Disable existing controls
		controls = new PlayerControls();
		controls.Enable();

		state = KartState.DRIVING;
		
		/* Active state changes - Player inputs (make requests) */
		controls.Gameplay.Drift.performed += action => { 
			if(state == KartState.DRIVING) 
			{
				state = KartState.DRIFTING;
				//GetComponent<KartController>().driftAngle = transform.forward;
			}
		};

		controls.Gameplay.Drift.canceled += action => { 
			if(state == KartState.DRIFTING)
			{
				state = KartState.DRIVING;
			}	
		};

    }

    void FixedUpdate()
    {
        
		/* Active state changes - Recieve requests */

		/* Passive state changes */

    }

}

public enum KartState
{
	DRIVING, DRIFTING, REVERSING
}