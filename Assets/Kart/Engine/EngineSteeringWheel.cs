using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSteeringWheel : KartBehavior
{
    public static readonly float INPUT_DEADZONE = 0.1f;

#region Configuration fields
    [Header("Configuration fields"), SerializeField]
	private float steeringWheelTurnSpeed = 5f;
#endregion

#region Runtime fields
    /// <summary>A [-1, 1] range float indicating the amount the steering wheel is turned and the direction.</summary>
	public float SteeringWheelDirection { get; private set; }
    public float StraightSteeringWheelTime { get; private set; }
#endregion

#region Utility fields
	public bool SteeringWheelMatchesTurn => Mathf.Sign(SteeringWheelDirection) == Mathf.Sign(kartCtrl.TurnInput.x);
	public bool SteeringWheelMatchesDrift => Mathf.Sign(SteeringWheelDirection) == Mathf.Sign(EngineWheels.DriftDirection);
#endregion

    protected new void Awake() 
    {
        base.Awake();
        
    }

    private void Update() 
    {
        /* Steering wheel direction modification */
        Vector2 turnInput = kartCtrl.TurnInput;
		if(Mathf.Abs(turnInput.x) > 0) { 
			SteeringWheelDirection += (!SteeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * turnInput.x * Time.deltaTime;
			SteeringWheelDirection += (!SteeringWheelMatchesTurn ? 2f : 1f) * steeringWheelTurnSpeed * turnInput.x * Time.deltaTime;
			SteeringWheelDirection = Mathf.Clamp(SteeringWheelDirection, -1, 1);
		} else { 
			SteeringWheelDirection = Mathf.Lerp(SteeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+EngineBase.SpeedRatio) * Time.deltaTime);
			if(Mathf.Abs(SteeringWheelDirection) <= INPUT_DEADZONE) 
			SteeringWheelDirection = Mathf.Lerp(SteeringWheelDirection, 0, (steeringWheelTurnSpeed*2f) * (1+EngineBase.SpeedRatio) * Time.deltaTime);
			if(Mathf.Abs(SteeringWheelDirection) <= INPUT_DEADZONE) 
				SteeringWheelDirection = 0;
		}

        if(Mathf.Abs(SteeringWheelDirection) < INPUT_DEADZONE) {
            StraightSteeringWheelTime += Time.deltaTime;
        } else {
            StraightSteeringWheelTime = 0;
        }
    }

    private void FixedUpdate() 
    {  
       
    }

    ///<summary> s</summary>
    public void SetTurnInput(Vector2 turnInput) 
    {

    }
}
