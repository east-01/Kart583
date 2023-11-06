using UnityEngine;
using System;

public class KartController : MonoBehaviour
{
	public float maxSpeed = 5f;
    public float acceleration = 10f;
	public float passiveDeceleration = 1f;
    public float rotationSpeed = 100f;

	private PlayerControls controls;
    private Rigidbody rb;
	private KartStateManager stateMgr;
	private KartState state { get { return stateMgr.state; } }

	// Runtime variables
	[SerializeField]
    private float rotationInput;
	
	[SerializeField]
	private Vector2 velocity;
	private float speed { get { return velocity.magnitude; } }
	private float speedRatio { get { return speed/maxSpeed; } }
	
	private Vector2 driftDirection;

	public Vector3 driftAngle;

    private void Start()
    {
		if(controls != null) controls.Disable(); // Disable existing controls
		controls = new PlayerControls();
		controls.Enable();

        rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();
    }

    private void Update()
    {

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		float throttle = controls.Gameplay.Throttle.ReadValue<float>();

		switch(state) { 
			case KartState.DRIVING:	
				if(throttle > 0) {
					velocity = Vector2.ClampMagnitude(transform.forward * (throttle * acceleration + speed) * Time.deltaTime, maxSpeed);
				} else {
					// Shorten velocity vector by the passive deceleration rate. TODO: Add brakes
					velocity = velocity.normalized * (speed - passiveDeceleration*Time.deltaTime);
					// WARNING: This might cause the kart to stop if it bounced off the wall and rolled backwards
					if(speed < 0) velocity = Vector2.zero; 
				}

				rotationInput = turn.x * speedRatio;
				break;
			case KartState.DRIFTING:
				
				transform.forward = turn;
				
				break;
			case KartState.REVERSING:
				break;
		}
    
		Vector3 movement = transform.forward * velocity * maxSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);


		float rotation = rotationInput * rotationSpeed * TurnFunction(0.1f, 0.3f, 0.5f, speedRatio) * Time.deltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);			
		
	}

	/** The callback from KartStateManager indicating when we've changed state.
	 *  Thrown immediately before the state changes, newState is the state we're changing to. */
	public void StateChanged(KartState newState) { 
		switch(state) { 
			case KartState.DRIVING:
				transform.forward = velocity;
				break;
			case KartState.DRIFTING:



				break;
			case KartState.REVERSING:
				break;
			default:
				break;
		}
	}

	/** Gets turn power percentage from a speed percentage.
		See desmos: https://www.desmos.com/calculator/tmojijx91i 
		
		In interval [a, b] you will get max turn power percentage.
		'c' is the turn power percentage at max speed.
		Outside the interval you will get a sloped value. See desmos. */
	public float TurnFunction(float a, float b, float c, float x) { 
		if(a >= b) throw new ArgumentException("a (" + a + ") must be less than b (" + b + ")");
		x = Mathf.Clamp01(x);
		if(x < a) { 
			return x/a;
		} else if(x > b) { 
			return -(c*(x-1))/(1-b) + c;
		} else { 
			return 1;	
		}
	}

}
