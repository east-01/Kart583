using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/** Manages the kart's state by two ways: 
      1. Active state changes: The state is changed by player
           input/callback, usually leading to a request flag
		   being set.
	  2. Passive state changes: These state changes usually
		   happen when the player ISN'T performing input. */
public class KartStateManager : MonoBehaviour
{

	public float timeInState { get; private set; }

	[SerializeField] 
	private KartState _state;
	public KartState state { 
		get { return _state; } 
		set { 
			// Triggered state change, possibly throw an event
			_state = value; 
			timeInState = 0;
			GetComponent<KartController>().StateChanged(value);
		} 
	}

	private KartController kc;

    void Start()
    {
        
		kc = GetComponent<KartController>();

		state = KartState.DRIVING;
		
		/* Active state changes - Player inputs (make requests) */

    }

    void Update()
    {
     
		timeInState += Time.deltaTime;

		/* Active state changes - Recieve requests */

		/* Passive state changes */
		if(state == KartState.DRIFTING && kc.Grounded() && kc.GetSteeringWheelDirection() == 0 && timeInState >= 0.15f) { 
			state = KartState.DRIVING;
		}

    }

}

public enum KartState
{
	DRIVING, DRIFTING, REVERSING
}