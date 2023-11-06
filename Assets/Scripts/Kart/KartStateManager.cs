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
		private set { 
			// Triggered state change, possibly throw an event
			GetComponent<KartController>().StateChanged();
			_state = value; 
		} 
	}

	private PlayerControls controls;

    void Start()
    {
        
		if(controls != null) controls.Disable(); // Disable existing controls
		controls = new PlayerControls();
		controls.Enable();
		
		/* Active state changes - Player inputs (make requests) */
		state = KartState.DRIVING;

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