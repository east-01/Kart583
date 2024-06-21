using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Engine component: Tires, driven by EngineBase
/// Reponsible for
///  - Simulating RPM
///  - Generating torque for tires
/// </summary>
public class EngineBase : KartBehavior
{

#region Configuration fields
    // [Header("Configuration fields")]
#endregion

#region Runtime fields
    /// <summary>The up vector that the kart follows. Will always be normalized.</summary>
    public new Vector3 Up { get; private set; } = new(0, 1, 0);
    /// <summary>Time engine is stalled, stalls usually come from damage</summary>
    public new float EngineStallTime { get; private set; } = 0;
    public StallType StallType { get; private set; } = StallType.NONE;
#endregion

#region Utility fields
	public new Vector3 TrackVelocity => kartCtrl.RemoveUpComponent(rb.velocity); 
	/// <summary>The velocity magnitude tangential to the up vector</summary>
	public new float TrackSpeed => TrackVelocity.magnitude;
	public new float CurrentMaxSpeed => EngineWheels.Momentum == 1 ? (EngineBoost.ActivelyBoosting ? Settings.maxBoostSpeed : Settings.maxSpeed) : Settings.maxSpeed/4f;
	public new float SpeedRatio => EngineBase.TrackSpeed/CurrentMaxSpeed;
#endregion

#region Events
    public delegate void EngineStallHandler(float stallTime, StallType stallType);
    public EngineStallHandler EngineStallEvent;
#endregion

    protected new void Awake() 
    {
        base.Awake();
        
    }

    private void Update() 
    {
        if(EngineStallTime > 0)
			EngineStallTime = Math.Max(EngineStallTime-Time.deltaTime, 0);

    }

    private void FixedUpdate() 
    {  
        /* Up force: It should always be that transform.Up == up */
		// Code found here https://gamedev.stackexchange.com/questions/194641/how-to-set-transform-up-without-locking-the-y-axis
		Quaternion zToUp = Quaternion.LookRotation(Up, -transform.forward);
		Quaternion yToz = Quaternion.Euler(90, 0, 0);
		transform.rotation = zToUp * yToz;

		/* Apply gravity */
		if(!Grounded) 
			rb.AddForce(-Up.normalized*Physics.gravity.magnitude);

		/* Check if player is stuck in ground*/
		if(Grounded && EngineWheels.DistanceFromGround < EngineWheels.RideHeight-0.015f && EngineWheels.DistanceFromGround != -1)
			transform.position = EngineWheels.GroundHit.point + Up*EngineWheels.RideHeight;

		/* Interpolate up vector back to default if we're not grounded */
		if(!Grounded)
			Up = Vector3.Lerp(Up, Vector3.up, Mathf.Clamp01(EngineWheels.Airtime/0.5f));
    }

    public void SetUpVector(Vector3 upVector) => Up = upVector;
    public void ApplyStall(float stallTime, StallType type) {
        if(StallType > type)
            return;
        EngineStallTime = stallTime;
        StallType = type;
        EngineStallEvent?.Invoke(stallTime, type);
    }

}

// Order elements by strength, later entries get higher priority
public enum StallType {
    NONE,
    SMALL,
    LARGE
}