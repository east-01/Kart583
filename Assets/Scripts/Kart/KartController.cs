using UnityEngine;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using System.Collections.Generic;
// Kart Controller is NOT ALLOWED to use UnityEngine.InputSystem. See HumanDriver!

/**
 * Kart ControllerV3 by Ethan Mullen
 */
public class KartController : MonoBehaviour
{

	/* ----- Settings variables ----- */
	public Transform kartModel;

	[Header("Controls")]
	public float inputDeadzone = 0.1f;
	public float crashStateLength = 0.15f;
	public Vector3 up = new(0, 1, 0);

	[Header("Speed")]
	public float maxSpeed = 20f;
	public float maxBoostSpeed = 30f;
    public float acceleration = 5f;

	[Header("Turning")]
	public float kartTurnSpeed = 2f;
	public AnimationCurve kartTurnPower;
	public float steeringWheelTurnSpeed = 5f;
	public Vector2 turnMultiplierRangeDriftMatch = new(1.5f, 2f); // Turn boost for drift that matches joystick
	public Vector2 turnMultiplierRangeDriftDiffer = new(-0.2f, 0.35f); // Turn decrease for drift that doesn't match joystick

	[Header("Drift")]
	public float driftAge = 4f; // The time (in seconds) it takes for a drift to reach its age
	public float driftAngleMin = 0.225f;
	public float driftAngleMax = 0.4f;

	public float driftHopSpeedPercent = 0.1f; // The percent of max speed that the player is allowed to hop at
	public float driftEngageSpeedPercent = 0.25f; // The percent of max speed that the player is allowed to engage a drift at
	public float driftVerticalVelocity = 3f;

	[Header("Boost")]
	public float requiredBoostPercentage = 0.3f;
	public float maxBoost = 3f; // Boost will be time in seconds
	public float boostGain = 1f;
	public float passiveBoostDrain = 3f;
	public float activeBoostDrain = 1.75f;

	/* ----- Runtime variable ----- */
	private KartStateManager stateMgr;
	private Rigidbody rb;

	/* Input variables */
	[Header("Input"), SerializeField] private Vector2 turn;
	[SerializeField] private float throttle;
	[SerializeField] private bool boosting;


	public float steeringWheelDirection; // A [-1, 1] range float indicating the amount the steering wheel is turned and the direction.

	// Velocity/Speed
	public Vector3 kartForward;
	public int momentum;

	public int driftDirection; // Indicates if we're in a left/right drift
	private float driftTheta;
	private float driftThetaTarget;
	[Header("Runtime fields")]
	public bool driftParticles; // True if driftparticles should be showing
	private bool driftHopRequest;

	public float boostAmount;

	private bool grounded; // Stores last update's grounded status
	private float airtime;
	public bool collisionFlag;

    private void Start()
    {
		rb = GetComponent<Rigidbody>();
		stateMgr = GetComponent<KartStateManager>();

		if(kartModel == null) Debug.LogWarning("KartController on \"" + gameObject.name + "\" doesn't have a kartModel assigned."); 

		collisionFlag = false;

		// TODO: Make kart spawn at a spawn position and face a certain direction
		kartForward = new Vector3(1, 0, 0); 
		transform.forward = kartForward;
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
		this.driftParticles = airtime <= 0.05f && stateMgr.state == KartState.DRIFTING && driftDirection != 0;

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
		if(stateMgr.state == KartState.DRIFTING && airtime > 0.05f && Math.Abs(turn.x) >= inputDeadzone) 
			driftDirection = (int)Mathf.Sign(turn.x);

		driftThetaTarget = 0;
		if(stateMgr.state == KartState.DRIFTING && driftDirection != 0 && Mathf.Abs(turn.x) >= inputDeadzone) { 
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

		// Animate kartModel forward
		if(kartModel != null)
			kartModel.forward = RotateVectorAroundAxis(kartForward, up, driftTheta);

	}

	private float theta;
	private void FixedUpdate() 
	{

		Vector3 velNoY = rb.velocity;
		velNoY.y = 0;

		// Momentum gets updated first, then kartForward second
		momentum = Vector3.Dot(rb.velocity, transform.forward) >= 0 ? 1 : -1;
		if(velNoY.magnitude > 0.15f)
			kartForward = new Vector3(rb.velocity.x, 0, rb.velocity.z).normalized;
		else
			kartForward = transform.forward;

		// Update theta
		theta = kartTurnSpeed * steeringWheelDirection * kartTurnPower.Evaluate(SpeedRatio) * DriftTurnMultiplier * Time.fixedDeltaTime;
		if(!grounded) theta /= 3;
		if(momentum < 0) theta = -theta;

		// Handle collision or driving state
		if(collisionFlag) {
			
		} else {

			Vector3 throttleForce = (ActivelyBoosting ? 1f : throttle) * acceleration * transform.forward;
			if(Mathf.Abs(throttle) > inputDeadzone && TrackSpeed <= CurrentMaxSpeed)
				rb.AddForce(throttleForce, ForceMode.Acceleration);	

			Vector3 turnForce = theta*1000f*-Vector3.Cross(rb.velocity.normalized, up);
			rb.AddForce(turnForce, ForceMode.Acceleration);

			transform.forward = kartForward;

			if(driftHopRequest) {
				rb.AddForce(up * driftVerticalVelocity, ForceMode.VelocityChange);
				driftHopRequest = false;
			}

		}

	}

	public void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.isTrigger) return;
		bool wasGroundCollision = true;
		for(int i = 0; i < collision.contactCount; i++) { 
			if(Vector3.Dot(Vector3.up, collision.GetContact(i).normal) < 0.75) { 
				wasGroundCollision = false;
				break;
			}
		}

		if(!wasGroundCollision) collisionFlag = true;

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
								
				if(!CanDriftHop) {
					stateMgr.state = KartState.DRIVING;
					break;
				}

				driftHopRequest = true;
				if(!CanDriftEngage) stateMgr.state = KartState.DRIVING;

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

	/* Input */
	public Vector2 TurnInput {
		get { return turn; }
		set {
			turn = value;
			if(turn.magnitude <= inputDeadzone) this.turn = Vector2.zero;
		}
	}

	public float ThrottleInput {
		get { return throttle; }
		set {
			this.throttle = value;
		}
	}

	public bool DriftInput { 
		get { return stateMgr.state == KartState.DRIFTING; } 
		set {
			if(value && stateMgr.state == KartState.DRIVING && Grounded() && !ActivelyBoosting)
				stateMgr.state = KartState.DRIFTING;
			else if(!value && stateMgr.state == KartState.DRIFTING)
				stateMgr.state = KartState.DRIVING;
		}
	}

	public bool BoostInput {
		get { return boosting; }
		set {
			if(value && !boosting && (boostAmount/maxBoost) >= requiredBoostPercentage) { 
				boosting = true;
				stateMgr.state = KartState.DRIVING;
			} else if(!value) { 
				if(boosting) boostAmount = 0;
				boosting = false;
			}
		}
	}

	/** The velocity magnitude tangential to the up vector */
	public float TrackSpeed { 
		get { 
			if(rb == null) rb = GetComponent<Rigidbody>(); 
			return new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude; 
		} 
	}

	public float CurrentMaxSpeed { get { return momentum == 1 ? (ActivelyBoosting ? maxBoostSpeed : maxSpeed) : maxSpeed/4f; } }
	public float SpeedRatio { get { return TrackSpeed/CurrentMaxSpeed; } }

	public bool SteeringWheelMatchesTurn { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(turn.x); } }
	public bool SteeringWheelMatchesDrift { get { return Mathf.Sign(steeringWheelDirection) == Mathf.Sign(driftDirection); } }

	public bool CanDriftHop { get { return SpeedRatio >= driftHopSpeedPercent && momentum == 1; } }
	public bool CanDriftEngage { get { return SpeedRatio >= driftEngageSpeedPercent; } }

	public bool ActivelyBoosting { get { return boosting && boostAmount > 0;} }
	public float BoostRatio { get { return boostAmount/maxBoost; } }
	
	public float DriftTurnMultiplier { 
		get { 
			if(stateMgr.state == KartState.DRIFTING) {
				float driftAgeRatio = stateMgr.timeInState/driftAge;
				return SteeringWheelMatchesDrift ? 
					Mathf.Lerp(turnMultiplierRangeDriftMatch.x, turnMultiplierRangeDriftMatch.y, 1-driftAgeRatio) :
					Mathf.Lerp(turnMultiplierRangeDriftDiffer.x, turnMultiplierRangeDriftDiffer.y, 1-driftAgeRatio);
			} else 
				return 1f;
		} 
	}

	private void DrawRays() { 
		// Debug.DrawLine(transform.position, transform.position + kartForward*3f, Color.blue, Time.deltaTime);
	}

}