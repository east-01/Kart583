using UnityEngine;
using System;

public class KartController : MonoBehaviour
{
	[Header("Speed")]
	public float maxSpeed = 20f;
    public float acceleration = 5f;
	public float passiveDeceleration = 3.5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public AnimationCurve kartTurnPower;
	public float steeringWheelTurnSpeed = 5f;

	private PlayerControls controls;
    private Rigidbody rb;
	private KartStateManager stateMgr;
	private KartState state { get { return stateMgr.state; } }

	/* !! Runtime variable */
	[Header("Runtime variables")]
	private Vector3 _velocity;
	private Vector3 velocity
	{
		get { return _velocity; }
		set { 
			_velocity = value; 
			if(value.magnitude > 0) velocityNormal = value.normalized;
		}
	}
	[SerializeField] private float velocityMagnitude;
	private Vector3 velocityNormal; // Keeps track of the normal of the velocity

	private float speed { get { return velocity.magnitude; } }
	private float speedRatio { get { return speed/maxSpeed; } }

	/** A [-1, 1] range float indicating the amount the steering wheel is turned and the direction. */
	private float steeringWheelDirection;
	
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
		velocityMagnitude = velocity.magnitude;

		/* DEBUG VISUALIZATION CODE */
		//Debug.DrawRay(transform.position, velocity * 10, Color.red, 1f);

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		if(turn.magnitude <= 0.1f) turn = Vector2.zero;
		Vector3 turn3 = new Vector3(turn.x, 0, turn.y);
		float throttle = controls.Gameplay.Throttle.ReadValue<float>();

		if(Mathf.Abs(turn.x) > 0) { 
			steeringWheelDirection += (Mathf.Sign(turn.x) != steeringWheelDirection ? 2f : 1) * steeringWheelTurnSpeed * turn.x * Time.deltaTime;
			steeringWheelDirection = Mathf.Clamp(steeringWheelDirection, -1, 1);
		} else { 
			steeringWheelDirection = Mathf.Lerp(steeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+speedRatio) * Time.deltaTime);
			if(Mathf.Abs(steeringWheelDirection) <= 0.01) steeringWheelDirection = 0;
		}

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
				float theta = kartTurnSpeed * steeringWheelDirection * Time.deltaTime;
				theta *= kartTurnPower.Evaluate(speedRatio);
				velocity = RotateVectorAroundAxis(velocity, Vector3.up, theta);
                velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

				// Make transform's forward follow velocity
				if(velocityNormal != Vector3.zero) transform.forward = velocityNormal;
				break;
			case KartState.DRIFTING:
				
				transform.forward = driftDirection + turn3;
				break;
			case KartState.REVERSING:
				break;
		}
    
		// Handle upwards velocity, we're letting Rigidbody use it's default gravity for downwards velocity.
		float yComp = Math.Max(velocity.y, 0); // We shouldn't have negative vertical velocity.
		if(yComp > 0) { 
			yComp -= 3*Time.deltaTime;
		}
		velocity = new Vector3(velocity.x, yComp, velocity.z);

        rb.MovePosition(rb.position + velocity*Time.deltaTime);
		if(rb.position.y < 0) {
			rb.MovePosition(new Vector3(0, 3, 0));	
			steeringWheelDirection = 0;
		}
		
	}

	/** The callback from KartStateManager indicating when we've changed state.
	 *  Thrown immediately before the state changes, newState is the state we're changing to. */
	public void StateChanged(KartState newState) 
	{ 
		driftAngle = Vector3.zero;
		print("state changed from " + state + " to " + newState);
		if(controls == null) return;
		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		switch(newState) { 
			case KartState.DRIVING:
				if(velocity.magnitude > 0) transform.forward = velocity;
				break;
			case KartState.DRIFTING:
				driftAngle = transform.forward + new Vector3(turn.x, 0, turn.y);
				velocity = new Vector3(velocity.x, 200, velocity.y);
				print("velocity.y="+velocity.y);
				break;
			case KartState.REVERSING:
				break;
			default:
				break;
		}
	}

	public Vector3 RotateVectorAroundAxis(Vector3 inputVector, Vector3 rotationAxis, float angleRadians)
    {
        rotationAxis = rotationAxis.normalized;
        Quaternion rotation = Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, rotationAxis);
        return rotation * inputVector;
    }

}
