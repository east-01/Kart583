using UnityEngine;
using System;
using Unity.VisualScripting;

public class KartController : MonoBehaviour
{

	[Header("Controls")]
	public float controllerDeadzone = 0.1f;

	[Header("Speed")]
	public float maxSpeed = 20f;
    public float acceleration = 5f;
	public float passiveDeceleration = 3.5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public AnimationCurve kartTurnPower;
	public float steeringWheelTurnSpeed = 5f;

	/* !! Runtime variable */
	private PlayerControls controls;
    private Rigidbody rb;
	private KartStateManager stateMgr;
	private KartState state { 
		get { return stateMgr.state; } 
	}

	private Vector3 _velocity;
	private Vector3 velocity
	{
		get { return _velocity; }
		set { 
			_velocity = value; 
			if(value.magnitude > 0) velocityNormal = value.normalized;
		}
	}
	[Header("Runtime variables")]
	[SerializeField] private float velocityMagnitude;
	private Vector3 velocityNormal; // Keeps track of the normal of the velocity

	private float speed { get { return velocity.magnitude; } }
	private float speedRatio { get { return speed/maxSpeed; } }

	/** A [-1, 1] range float indicating the amount the steering wheel is turned and the direction. */
	private float steeringWheelDirection;
	
	private int driftDirection; // Indicates if we're in a left/right drift
	[SerializeField] private bool isGrounded;

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
		isGrounded = Grounded();

		/* DEBUG VISUALIZATION CODE */
		//Debug.DrawRay(transform.position, velocity * 10, Color.red, 1f);

		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		if(turn.magnitude <= controllerDeadzone) turn = Vector2.zero;
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
			case KartState.DRIFTING:
				if(throttle > 0) {
					velocity += transform.forward.normalized * throttle * acceleration * Time.deltaTime;
					if(speed > maxSpeed) {
						Vector3 vel = velocity;
						float y = vel.y;
						vel.y = 0;
						vel = Vector3.ClampMagnitude(vel, maxSpeed);
						vel.y = y;
						velocity = vel;
					}
				} else {
					// Shorten velocity vector by the passive deceleration rate. TODO: Add brakes
					velocity = Vector3.Lerp(velocity, Vector3.zero, passiveDeceleration*Time.deltaTime);
					if(velocity.magnitude < 0.001f) velocity = Vector3.zero;
				}

				// Rotation
				float theta = kartTurnSpeed * steeringWheelDirection * Time.deltaTime;
				theta *= kartTurnPower.Evaluate(speedRatio);
				if(state == KartState.DRIFTING) theta *= Mathf.Sign(driftDirection) == Mathf.Sign(steeringWheelDirection) ? 2 : 0.3f;
				if(!Grounded()) theta /= 3;

				velocity = RotateVectorAroundAxis(velocity, Vector3.up, theta);

				// Make transform's forward follow velocity
				if(velocityNormal != Vector3.zero && state == KartState.DRIVING) {
					Vector3 kartForward = velocityNormal;
					kartForward.y = 0;
					transform.forward = kartForward;
				} else if(state == KartState.DRIFTING) { 
					Vector3 kartForward = velocityNormal;
					kartForward.y = 0;
					kartForward = RotateVectorAroundAxis(kartForward, Vector3.up, 3*steeringWheelDirection);
					transform.forward = kartForward;
					
				}
				break;
			case KartState.REVERSING:
				break;
		}
    
		// Handle upwards velocity, we're letting Rigidbody use it's default gravity for downwards velocity.
		float yComp = Math.Max(velocity.y, 0); // We shouldn't have negative vertical velocity.
		if(yComp > 0) { 
			yComp += Physics.gravity.y*Time.deltaTime;
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
		if(controls == null) return;
		Vector2 turn = controls.Gameplay.Turn.ReadValue<Vector2>();
		switch(newState) { 
			case KartState.DRIVING:
				if(velocity.magnitude > 0) transform.forward = velocity;
				break;
			case KartState.DRIFTING:
				velocity = new Vector3(velocity.x, 3f, velocity.z);

				if(Mathf.Abs(turn.x) < controllerDeadzone) {
					GetComponent<KartStateManager>().state = KartState.DRIVING;
					return;
				}

				driftDirection = (int)Mathf.Sign(turn.x);
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

	public bool Grounded() { 
		float distance = 0.05f;
		Vector3 raycastOrigin = new Vector3(transform.position.x, GetComponent<BoxCollider>().bounds.min.y+0.001f, transform.position.z);
		return Physics.Raycast(raycastOrigin, Vector3.down, distance);
	}

}
