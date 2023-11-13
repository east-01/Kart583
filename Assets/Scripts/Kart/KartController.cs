using UnityEngine;
using System;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices.WindowsRuntime;

/**
 * Kart Controller by Ethan Mullen
 * 
 */
public class KartController : MonoBehaviour
{

	/* ### Settings variables ### */
	[Header("Controls")]
	public float controllerDeadzone = 0.1f;

	[Header("Speed")]
	public float maxSpeed = 20f;
	public float maxBoostSpeed = 30f;
    public float acceleration = 5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public AnimationCurve kartTurnPower;
	public float steeringWheelTurnSpeed = 5f;

	public Vector2 driftTurnDirectionMatchRange = new(1.5f, 2f);
	public Vector2 driftTurnDirectionDifferRange = new(-0.2f, 0.35f);

	[Header("Drift")]
	/** The time (in seconds) that it takes for a drift to reach its age.
	 *  Drift performance worsens as it reaches its age. */
	public float driftAge = 4f;
	public float driftAngleMin = 0.225f;
	public float driftAngleMax = 0.4f;

	public float driftEngageSpeed { get { return 0.25f * maxSpeed; } }
	public float driftVerticalVelocity = 3f;

	[Header("Boost")]
	public AnimationCurve ageWeight;
	public float maxBoost = 3f; // Boost will be time in seconds
	public float passiveBoostDrain = 3f;

	/* ### Runtime variable ### */
	private KartStateManager stateMgr;
	private Rigidbody rb;
	private KartState state { get { return stateMgr.state; } }

	/* Input variables */
	private Vector2 turn;
	private float throttle;

	// Velocity/Speed
	public Vector3 kartForward { 
		get { 
			foreach(Vector3 forwardVector in new Vector3[] { rb != null ? rb.velocity : Vector3.zero, transform.forward }) { 
				if(Math.Abs(forwardVector.x) > kartForwardCutoff || Math.Abs(forwardVector.z) > kartForwardCutoff)  // Using '== 0' here causes glitching
					return forwardVector.normalized;
			}
			return Vector3.forward;
		}	
	}
	private float kartForwardCutoff = 0.1f;

	/** The velocity magnitude tangential to the up vector */
	private float trackSpeed { get { return new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude; } }
	private float speedRatio { get { return trackSpeed/maxSpeed; } }

	public bool steeringWheelMatchesTurn { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(turn.x); } }
	/** A [-1, 1] range float indicating the amount the steering wheel is turned and the direction. */
	public float steeringWheelDirection { get; private set; }
	public bool steeringWheelMatchesDrift { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(driftDirection); } }

	public int driftDirection { get; private set; } // Indicates if we're in a left/right drift
	private float driftTheta;
	private float driftThetaTarget;

	private float boostAmount;
	public bool boosting { get; private set; }
	
	private bool grounded; // Stores last update's grounded status
	private float airtime;

    private void Start()
    {
		rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();
    }

    private void Update()
    {
		bool grounded = Grounded();
		if(grounded) { 
			airtime = 0;
		} else { 
			if(this.grounded) airtime = 0;
			airtime += Time.deltaTime;	
		}
		this.grounded = grounded;

		// Steering wheel direction modification
		if(Mathf.Abs(turn.x) > 0) { 
			steeringWheelDirection += (!steeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * turn.x * Time.deltaTime;
			steeringWheelDirection = Mathf.Clamp(steeringWheelDirection, -1, 1);
		} else { 
			steeringWheelDirection = Mathf.Lerp(steeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+speedRatio) * Time.deltaTime);
			if(Mathf.Abs(steeringWheelDirection) <= controllerDeadzone) 
				steeringWheelDirection = 0;
		}
		
		// If we're drifting and airborne, change drift direction to match joystick
		// Use airtime to ensure we maintain drift direction during small falls.
		if(state == KartState.DRIFTING && airtime > 0.05f) 
			driftDirection = (int)Mathf.Sign(turn.x);

		// Modify boost
		if(boostAmount > 0) boostAmount -= Time.deltaTime * (boosting ? 1f : passiveBoostDrain);	
		if(boostAmount < 0) boostAmount = 0;

		// Make transform's forward follow velocity
		Vector3 kartForward = this.kartForward; // Default kartForward for state DRIVING
		kartForward.y = 0;

		if(state == KartState.DRIFTING) { 
			driftThetaTarget = Mathf.Sign(driftDirection)*driftAngleMin;
			if(steeringWheelMatchesDrift) 
				driftThetaTarget += steeringWheelDirection*(driftAngleMax-driftAngleMin);
		}			
		driftTheta = Mathf.Lerp(driftTheta, driftThetaTarget, 20*Time.deltaTime);

		kartForward = RotateVectorAroundAxis(kartForward, Vector3.up, driftTheta);
		if(kartForward.normalized.magnitude > 0) transform.forward = kartForward.normalized;
	}

	private void FixedUpdate() 
	{
		HandleVelocity();
	}

	//https://www.reddit.com/r/Unity3D/comments/psukm1/know_the_difference_between_forcemodes_a_little/
	/** Takes the state of input and state manager and adds forces to the Rigidbody */
	public void HandleVelocity() 
	{ 
		driftThetaTarget = 0;

		// Forward force application
		if(throttle > 0) {
			Vector3 throttleForce = kartForward * throttle * acceleration;
			if((rb.velocity + throttleForce*Time.fixedDeltaTime).magnitude <= maxSpeed) { 
				rb.AddForce(throttleForce, ForceMode.Acceleration);			
			} else { 
				Vector3 targetVelocity = (rb.velocity + throttleForce*Time.fixedDeltaTime).normalized*maxSpeed;
				rb.AddForce(targetVelocity-rb.velocity, ForceMode.VelocityChange);
			}
		}

		// Turning
		float theta = kartTurnSpeed * steeringWheelDirection * Time.fixedDeltaTime;
		theta *= kartTurnPower.Evaluate(speedRatio);
		if(state == KartState.DRIFTING) {
			float driftAgeRatio = stateMgr.timeInState/driftAge;
			theta *= steeringWheelMatchesDrift ? 
				Mathf.Lerp(driftTurnDirectionMatchRange.x, driftTurnDirectionMatchRange.y, 1-driftAgeRatio) :
				Mathf.Lerp(driftTurnDirectionDifferRange.x, driftTurnDirectionDifferRange.y, 1-driftAgeRatio);
		}
		if(!grounded) theta /= 3;

		if(theta != 0) {
			/* Turn method #2: Rotate the velocity vector around the axis by theta, then add the difference
					to the velocity. This should rotate the vector without changing velocity. */
			rb.AddForce(RotateVectorAroundAxis(rb.velocity, Vector3.up, theta)-rb.velocity, ForceMode.VelocityChange);
		}
	}

	/** The callback from KartStateManager indicating when we've changed state.
	 *  Thrown immediately before the state changes, newState is the state we're changing to. */
	public void StateChanged(KartState newState) 
	{ 
		if(newState != KartState.DRIFTING) driftDirection = 0;

		switch(newState) { 
			case KartState.DRIVING:
				if(rb.velocity.magnitude > 0) transform.forward = rb.velocity;
				break;
			case KartState.DRIFTING:
				float speedBefore = trackSpeed;
				rb.AddForce(Vector3.up * driftVerticalVelocity, ForceMode.VelocityChange);
				if(speedBefore <= driftEngageSpeed) stateMgr.state = KartState.DRIVING;
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

	public bool Grounded() 
	{ 
		float distance = 0.05f;
		Vector3 raycastOrigin = new(transform.position.x, GetComponent<BoxCollider>().bounds.min.y+0.001f, transform.position.z);
		return Physics.Raycast(raycastOrigin, Vector3.down, distance);
	}

	/* Input Events */
	public void OnTurn(InputAction.CallbackContext context) 
	{ 
		turn = context.ReadValue<Vector2>();
		if(turn.magnitude <= controllerDeadzone) turn = Vector2.zero;
	}

	public void OnThrottle(InputAction.CallbackContext context) 
	{ 
		throttle = context.ReadValue<float>();
	}

	public void OnBoost(InputAction.CallbackContext context) { 
		if(context.performed && !boosting && boostAmount > 0) { 
			boosting = true;
		} else if(context.canceled) { 
			if(boosting) boostAmount = 0;
			boosting = false;
		}
	}

}
