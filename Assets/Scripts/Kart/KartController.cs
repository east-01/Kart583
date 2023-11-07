using UnityEngine;
using System;

public class KartController : MonoBehaviour
{
	public float maxSpeed = 5f;
    public float acceleration = 10f;
	public float passiveDeceleration = 1f;

	public float rotationSpeed = 5f;

	private PlayerControls controls;
    private Rigidbody rb;
	private KartStateManager stateMgr;
	private KartState state { get { return stateMgr.state; } }

	// Runtime variables	
	private Vector3 _velocity;
	private Vector3 velocity
	{
		get { return _velocity; }
		set { 
			_velocity = value; 
			if(value.magnitude > 0) velocityNormal = value.normalized;
		}
	}
	/** A variable keeping track of the normal of velocity */
	private Vector3 velocityNormal;

	[SerializeField]
	private float angularMomentum;

	private float speed { get { return velocity.magnitude; } }
	private float speedRatio { get { return speed/maxSpeed; } }
	
	private Vector3 driftDirection;
	public Vector3 driftAngle;

    private void Start()
    {
		if(controls != null) controls.Disable(); // Disable existing controls
		controls = new PlayerControls();
		controls.Enable();

        rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();

		velocityNormal = transform.forward.normalized;

    }

    private void Update()
    {

		//print("speed: " + speed + ", speedRatio: " + speedRatio);

		/* DEBUG VISUALIZATION CODE */
		//Debug.DrawRay(transform.position, velocity * 10, Color.red, 1f);

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		Vector3 turn3 = new Vector3(turn.x, 0, turn.y);
		float throttle = controls.Gameplay.Throttle.ReadValue<float>();

		switch(state) { 
			case KartState.DRIVING:	
				if(throttle > 0) {
					//velocity = Vector3.ClampMagnitude(velocity + velocity.normalized * throttle * acceleration * Time.deltaTime, maxSpeed);
					velocity += transform.forward.normalized * throttle * acceleration * Time.deltaTime;
                    velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
				} else {
					// Shorten velocity vector by the passive deceleration rate. TODO: Add brakes
					velocity = Vector3.Lerp(velocity, Vector3.zero, passiveDeceleration*Time.deltaTime);
					if(velocity.magnitude < 0.00001f) velocity = Vector3.zero;
				}

				// Rotation
				float theta = rotationSpeed * TurnFunction(0.1f, 0.3f, 0.5f, speedRatio) * turn.x * Time.deltaTime;
				print("theta: " + theta);
				if(Mathf.Abs(theta) > 0.0001f) {
					// Theta is still changing, do not decay momentum
					if(Mathf.Sign(theta) != Mathf.Sign(angularMomentum)) { 
						float ratio = angularMomentum/rotationSpeed;
						float mult = (ratio*ratio);
						print("multipluer: " + mult);
						theta *= mult;					
					}
					angularMomentum += theta;
					angularMomentum = Mathf.Clamp(angularMomentum, -rotationSpeed, rotationSpeed);
				} else { 
					// Theta is no longer changing, decay momentum
					angularMomentum = Mathf.Lerp(angularMomentum, 0, 1-speedRatio);
					//int sign = (int)Mathf.Sign(angularMomentum);
					//angularMomentum -= Time.deltaTime*sign;
					//if(Mathf.Sign(angularMomentum) != sign) angularMomentum = 0;
				}
				velocity = RotateVectorAroundAxis(velocity, Vector3.up, theta);

				transform.forward = velocityNormal;
				break;
			case KartState.DRIFTING:
				
				transform.forward = driftDirection + turn3;
				break;
			case KartState.REVERSING:
				break;
		}
    
        rb.MovePosition(rb.position + velocity);
		if(rb.position.y < 0) {
			rb.MovePosition(new Vector3(0, 3, 0));	
			angularMomentum = 0;
		}

		//float rotation = rotationInput * rotationSpeed * TurnFunction(0.1f, 0.3f, 0.5f, speedRatio) * Time.deltaTime;
  //      Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);
  //      rb.MoveRotation(rb.rotation * deltaRotation);			
		
	}

	/** The callback from KartStateManager indicating when we've changed state.
	 *  Thrown immediately before the state changes, newState is the state we're changing to. */
	public void StateChanged(KartState newState) { 
		driftAngle = Vector3.zero;

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		switch(newState) { 
			case KartState.DRIVING:
				if(velocity.magnitude > 0) transform.forward = velocity;
				break;
			case KartState.DRIFTING:
				driftAngle = transform.forward + new Vector3(turn.x, 0, turn.y);
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

	public Vector3 RotateVectorAroundAxis(Vector3 inputVector, Vector3 rotationAxis, float angleRadians)
    {
        // Ensure the rotation axis is normalized
        rotationAxis = rotationAxis.normalized;

        // Calculate the rotation using Quaternion
        Quaternion rotation = Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, rotationAxis);

        // Rotate the vector using the calculated rotation
        return rotation * inputVector;
    }

}
