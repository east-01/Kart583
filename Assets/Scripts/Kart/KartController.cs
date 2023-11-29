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
	public float inputDeadzone = 0.1f;
	public float crashStateLength = 0.15f;

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

	/** The percent of max speed that the player is allowed to hop at */
	public float driftHopSpeedPercent = 0.1f;
	/** The percent of max speed that the player is allowed to engage a drift at */
	public float driftEngageSpeedPercent = 0.25f;
	public float driftVerticalVelocity = 3f;

	[Header("Boost")]
	public float requiredBoostPercentage = 0.3f;
	public float maxBoost = 3f; // Boost will be time in seconds
	public float boostGain = 1f;
	public float passiveBoostDrain = 3f;
	public float activeBoostDrain = 1.75f;

	/* ### Runtime variable ### */
	private KartStateManager stateMgr;
	private Rigidbody rb;
	private KartState state { get { return stateMgr.state; } }

	/* Input variables */
	private Vector2 turn;
	private float throttle;

	/** A [-1, 1] range float indicating the amount the steering wheel is turned and the direction. */
	private float steeringWheelDirection;

	// Velocity/Speed
	/** Kartforward keeps track of which direction the transform.forward vector should face.
	    Initially, it will be set to Vector3.forward or (TODO) the direction that the
		  spawnpoint dictates.
		kartForward is used in velocity calculations. */
	public Vector3 kartForward;
	/** Momentum is updated along side kartForward, showing if we're rolling forward or backwards. */
	private int momentum;

	public int driftDirection { get; private set; } // Indicates if we're in a left/right drift
	private float driftTheta;
	private float driftThetaTarget;
	[Header("Runtime fields")]
	public bool driftParticles; // True if driftparticles should be showing

	private float boostAmount;
	private bool boosting;
	private bool lastActivelyBoosting;

	private bool grounded; // Stores last update's grounded status
	private float airtime;
	private float timeSinceLastCollision;

    private void Start()
    {
		rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();

		kartForward = Vector3.forward;
    }

    private void Update()
    {
		DrawRays();

		bool grounded = Grounded();
		if(grounded) { 
			airtime = 0;
		} else { 
			if(this.grounded) airtime = 0;
			airtime += Time.deltaTime;	
		}
		this.grounded = grounded;
		this.driftParticles = airtime <= 0.05f && state == KartState.DRIFTING && driftDirection != 0;

		// Steering wheel direction modification
		if(Mathf.Abs(turn.x) > 0) { 
			steeringWheelDirection += (!SteeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * turn.x * Time.deltaTime;
			steeringWheelDirection = Mathf.Clamp(steeringWheelDirection, -1, 1);
		} else { 
			steeringWheelDirection = Mathf.Lerp(steeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+SpeedRatio) * Time.deltaTime);
			if(Mathf.Abs(steeringWheelDirection) <= inputDeadzone) 
				steeringWheelDirection = 0;
		}
		
		// If we're drifting and airborne, change drift direction to match joystick
		// Use airtime to ensure we maintain drift direction during small falls.
		if(state == KartState.DRIFTING && airtime > 0.05f && Math.Abs(turn.x) >= inputDeadzone) 
			driftDirection = (int)Mathf.Sign(turn.x);

		driftThetaTarget = 0;
		if(state == KartState.DRIFTING && driftDirection != 0 && Mathf.Abs(turn.x) >= inputDeadzone) { 
			driftThetaTarget = Mathf.Sign(driftDirection)*driftAngleMin;
			if(SteeringWheelMatchesDrift) 
				driftThetaTarget += steeringWheelDirection*(driftAngleMax-driftAngleMin);
		}			
		driftTheta = Mathf.Lerp(driftTheta, driftThetaTarget, 20*Time.deltaTime);
		if(Mathf.Abs(driftTheta) < 0.01f) driftTheta = 0;

		// Boosting
		if(ActivelyBoosting) {
			boostAmount = Mathf.Max(boostAmount - activeBoostDrain*Time.deltaTime, 0); 
		} else if(driftParticles && SteeringWheelMatchesDrift) {
			boostAmount += boostGain*Time.deltaTime;
			if(boostAmount > maxBoost) boostAmount = maxBoost;
		} else { 
			boostAmount = Mathf.Max(boostAmount - passiveBoostDrain*Time.deltaTime, 0);									
		}

		// Variable kartForward can never be 0, we get stuck.
		if(kartForward.magnitude == 0) kartForward = transform.forward;

	}

	private void FixedUpdate() 
	{

		/* Update kartForward and momentum- make transform follow kartForward */
		// Momentum gets updated first, then kartForward second
		momentum = Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1;
		Vector3 kartForwardCandidate = rb.velocity;
		kartForwardCandidate.y = 0;
		if(CanChangeForward && kartForwardCandidate.magnitude > 0) { 
			kartForward = rb.velocity.normalized*momentum;	
		}
		if(timeSinceLastCollision < crashStateLength) { 
			kartForward = transform.forward;
		}
		
		Vector3 transformForward = kartForward; // Default kartForward for state DRIVING
		transformForward.y = 0;
		transformForward = RotateVectorAroundAxis(transformForward, Vector3.up, driftTheta);
		if(CanChangeForward && transformForward.magnitude > 0 && timeSinceLastCollision > crashStateLength) 
			transform.forward = transformForward.normalized; // Use TrackSpeed > 1.15f to stop randomly turning at slow speeds

		/** Crash state */
		if(timeSinceLastCollision + Time.fixedDeltaTime >= crashStateLength && timeSinceLastCollision < crashStateLength) { 
			// Reached end of crash state, make kart forward match velocity
			kartForward = transform.forward;
			Vector3 targetVelocity = kartForward*momentum*rb.velocity.magnitude;
			rb.AddForce(targetVelocity-rb.velocity, ForceMode.VelocityChange);
		}
		this.timeSinceLastCollision += Time.fixedDeltaTime;

		HandleVelocity();

		lastActivelyBoosting = ActivelyBoosting;

	}

	//https://www.reddit.com/r/Unity3D/comments/psukm1/know_the_difference_between_forcemodes_a_little/
	/** Takes the state of input and state manager and adds forces to the Rigidbody */
	public void HandleVelocity() 
	{ 
		driftThetaTarget = 0;

		float throttle = this.throttle;
		if(ActivelyBoosting) throttle = 1f;

		/* Forward force application */
		Vector3 throttleForce = kartForward * throttle * acceleration;
		// Represents the velocity after throttleForce has been added using AddForce(ForceMode.Acceleration)
		// Only used for VelocityChange calculations
		Vector3 addedVelocity = kartForward + throttleForce*Time.fixedDeltaTime;

		if(ActivelyBoosting && !lastActivelyBoosting) {
			throttleForce *= 10;
			// Make velocity == maxVelocity
			rb.AddForce((addedVelocity.normalized*CurrentMaxSpeed)-rb.velocity, ForceMode.VelocityChange);
		}

		if(Mathf.Abs(throttle) > inputDeadzone) {
			if(TrackSpeed <= CurrentMaxSpeed) { 
				rb.AddForce(throttleForce, ForceMode.Acceleration);	
			} else { 
				rb.AddForce((addedVelocity.normalized*momentum*CurrentMaxSpeed)-rb.velocity, ForceMode.VelocityChange);
			}
		}

		/* Turning */
		float theta = kartTurnSpeed * steeringWheelDirection * Time.fixedDeltaTime;
		theta *= kartTurnPower.Evaluate(SpeedRatio);
		if(state == KartState.DRIFTING) {
			float driftAgeRatio = stateMgr.timeInState/driftAge;
			theta *= SteeringWheelMatchesDrift ? 
				Mathf.Lerp(driftTurnDirectionMatchRange.x, driftTurnDirectionMatchRange.y, 1-driftAgeRatio) :
				Mathf.Lerp(driftTurnDirectionDifferRange.x, driftTurnDirectionDifferRange.y, 1-driftAgeRatio);
		}
		if(!grounded) theta /= 3;
		if(momentum < 0) theta = -theta;

		if(theta != 0) {
			/* Turn method #2: Rotate the velocity vector around the axis by theta, then add the difference
					to the velocity. This should rotate the vector without changing velocity magnitude. */
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
				break;
			case KartState.DRIFTING:
				
				float speedPercent = TrackSpeed/CurrentMaxSpeed;
				
				if(speedPercent < driftHopSpeedPercent || momentum != 1) {
					stateMgr.state = KartState.DRIVING;
					break;
				}

				rb.AddForce(Vector3.up * driftVerticalVelocity, ForceMode.VelocityChange);
				if(speedPercent < driftEngageSpeedPercent) stateMgr.state = KartState.DRIVING;

				break;
			case KartState.REVERSING:
				break;
			default:
				break;
		}
	}

	public void OnCollisionEnter(Collision collision)
	{
		bool wasGroundCollision = true;
		for(int i = 0; i < collision.contactCount; i++) { 
			if(Vector3.Dot(Vector3.up, collision.GetContact(i).normal) < 0.75) { 
				wasGroundCollision = false;
				break;
			}
		}

		if(!wasGroundCollision) timeSinceLastCollision = 0f;

	}

	private Vector3 RemoveComponent(Vector3 vector, Vector3 normal)
    {
        // Calculate the projection of vector onto normal
        float projection = Vector3.Dot(vector, normal);
        Vector3 projectionVector = projection * normal;

        // Subtract the projection from the original vector
        Vector3 result = vector - projectionVector;

        return result;
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
		if(turn.magnitude <= inputDeadzone) turn = Vector2.zero;
	}

	public void OnThrottle(InputAction.CallbackContext context) 
	{ 
		throttle = context.ReadValue<float>();
	}

	public void OnReverse(InputAction.CallbackContext context) { 
		throttle = -context.ReadValue<float>();	
	}

	public void OnBoost(InputAction.CallbackContext context) { 
		if(context.performed && !boosting && (boostAmount/maxBoost) >= requiredBoostPercentage) { 
			boosting = true;
			stateMgr.state = KartState.DRIVING;
		} else if(context.canceled) { 
			if(boosting) boostAmount = 0;
			boosting = false;
		}
	}

	/** The velocity magnitude tangential to the up vector */
	public float TrackSpeed { get { 
		if(rb == null) rb = GetComponent<Rigidbody>(); 
		return new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude; 
	} }
	public float CurrentMaxSpeed { get { return ActivelyBoosting ? maxBoostSpeed : maxSpeed; } }
	public float SpeedRatio { get { return TrackSpeed/CurrentMaxSpeed; } }

	public float GetSteeringWheelDirection() { return steeringWheelDirection; }
	public bool SteeringWheelMatchesTurn { get { return Mathf.Sign(GetSteeringWheelDirection()) == Mathf.Sign(turn.x); } }
	public bool SteeringWheelMatchesDrift { get { return Mathf.Sign(GetSteeringWheelDirection()) == Mathf.Sign(driftDirection); } }
	/** Calculations related to forward/backward break at slow speeds, only do them above this speed. */
	public bool CanChangeForward { get { return TrackSpeed > 1.25f; } }

	public float GetBoostAmount() { return boostAmount; }
	public bool IsBoosting() { return boosting; }
	public bool ActivelyBoosting { get { return boosting && boostAmount > 0;} }
	public float BoostRatio { get { return boostAmount/maxBoost; } }

	private void DrawRays() { 
		Debug.DrawLine(transform.position, transform.position + kartForward*3f, Color.blue, Time.deltaTime);
	}

}
