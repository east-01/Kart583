using UnityEngine;
using System;
// Kart Controller is NOT ALLOWED to use UnityEngine.InputSystem. See HumanDriver!

/// <summary>
/// Kart ControllerVX by Ethan Mullen
///
///	It's mainly responsible for recieving input to the kart (i.e. throttle and turning)
///   and delegating those inputs to specific EngineComponents. Each engine component
///   will have a specific job to perform on the kart to make it drive.
/// </summary>
[RequireComponent(typeof(EngineBase))]
[RequireComponent(typeof(EngineSteeringWheel))]
[RequireComponent(typeof(EngineWheels))]
[RequireComponent(typeof(EngineBoost))]
[RequireComponent(typeof(KartVectorDrawer))]
public class KartController : KartBehavior, GameplayManagerBehavior
{
	/* 
	KartController VX to do list:
	 - (*) Particle effects for when you land
	 - (**) Reversing while going forward takes forever to slow down
	*/

	private GameplayManager gameplayManager;

	public KartModel kartModel;	
	public Transform kartModelTransform;
	public KartSettings settings;

	public GameplayManager GameplayManager => gameplayManager;

	new protected void Awake() 
	{
		base.Awake();
		CoreManager.GameplayManagerDelegate.SubscribeForGameplayManager(this);
	}

	public void GameplayManagerLoaded(GameplayManager gameplayManager) 
	{
		this.gameplayManager = gameplayManager;
	}

	public void OnCollisionEnter(Collision collision)
	{
		if(collision.collider.isTrigger) return;

		// Kart on kart collision has bounce effect
		if(collision.gameObject.CompareTag("Kart")) {
			Vector3 collPoint = collision.GetContact(0).point;
			
			kartVisualsManager.SpawnBumpEffect(collPoint);

			rb.velocity += RemoveUpComponent(transform.position-collPoint)*15f + EngineBase.Up*2f;
			collision.rigidbody.velocity += RemoveUpComponent(collision.transform.position-collPoint)*15f + EngineBase.Up*2f;
		}

	}

#region Vector calculations
	/// <summary>
	/// Remove the b vector component from vector a.
	/// Example: If vector a is at a 45 degree angle and vector b is the up vector, this
	///   function will return the horizontal part of vector a.
	/// </summary>
	private Vector3 RemoveComponent(Vector3 a, Vector3 b)
    {
        // Calculate the projection of vector onto normal
        float projection = Vector3.Dot(a, b);
        Vector3 projectionVector = projection * b;
        // Subtract the projection from the original vector
        Vector3 result = a - projectionVector;
        return result;
    }

	/// <summary>Get the component of the input vector that is orthogonal with the karts up vector.</summary>
	public Vector3 RemoveUpComponent(Vector3 input) {
		return RemoveComponent(input, EngineBase.Up);
	}

	/// <summary>Get the component of the input vector that is aligned with the karts up vector.</summary>
	public Vector3 IsolateUpComponent(Vector3 input) {
		return EngineBase.Up*Vector3.Dot(input, EngineBase.Up);
	}

	public Vector3 RotateVectorAroundAxis(Vector3 inputVector, Vector3 rotationAxis, float angleRadians)
    {
        rotationAxis = rotationAxis.normalized;
        Quaternion rotation = Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, rotationAxis);
		Vector3 rotatedVector = rotation * inputVector;

		// Correct potential numerical precision issues for vertical axis rotations
		if (Mathf.Approximately(rotationAxis.y, 1.0f) || Mathf.Approximately(rotationAxis.y, -1.0f))
			rotatedVector.y = inputVector.y;

		return rotatedVector;
    }
#endregion

#region Input
	/* Input */
	[Header("Raw input fields"), SerializeField] private Vector2 rawTurnInput;
	public Vector2 TurnInput {
		get { return CanMove ? rawTurnInput : Vector2.zero; }
		set { rawTurnInput = value; }
	}

	[SerializeField] private float rawThrottleInput;
	public float ThrottleInput {
		get { return CanMove ? (EngineBoost.ActivelyBoosting ? 1f : rawThrottleInput) : 0; }
		set { rawThrottleInput = value; }
	}

	[SerializeField] private bool rawDriftInput;
	public bool RawDriftInput => rawDriftInput;
	public bool DriftInput { 
		get { return CanMove && rawDriftInput; } 
		set {
			rawDriftInput = value;
			if(value && EngineWheels.CanDriftEngage)
				EngineWheels.SetDrifting(value);
			else if(!value && EngineWheels.Drifting)
				EngineWheels.SetDrifting(value);
		}
	}

	[SerializeField] private bool rawBoostInput;
	public bool BoostInput {
		get { return CanMove && rawBoostInput; }
		set {
			rawBoostInput = value;
			if(value && !EngineBoost.Boosting && EngineBoost.CanEngageBoost) { 
				EngineBoost.SetBoosting(value);
				if(EngineWheels.Drifting)
					EngineWheels.SetDrifting(false);
			} else if(!value) { 
				EngineBoost.SetBoosting(value);
			}
		}
	}
#endregion

	public Vector3 KartForward { get { return RemoveUpComponent(transform.forward.normalized); } }

	public new bool CanMove { get { 
		if(gameplayManager == null)
			return false;
		return gameplayManager.RaceManager.CanMove && EngineBase.EngineStallTime <= 0; 
	} }
	
}

[Serializable]
public struct KartSettings 
{
	public float maxSpeed;
	public float maxBoost;
	public float maxBoostSpeed;
	public float acceleration;
	public float turnSpeed;
}