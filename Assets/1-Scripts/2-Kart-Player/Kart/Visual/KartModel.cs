using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/** Manages all visual aspects of the kart model */
public class KartModel : MonoBehaviour
{
    public KartController KartCtrl { get; private set; }

#region Editor fields
    [Header("Transforms"), SerializeField] private Transform carComponents;
    [SerializeField] private Transform model;

    [Header("Tires")] public bool drawTireRadiusWireFrames = false;
    public float frontTireRadius = 0.5f;
    public List<GameObject> frontTires;
    public List<GameObject> frontTireTurners;
    public float rearTireRadius = 0.5f;
    public List<GameObject> rearTires;
    public float tireTurnAngle = Mathf.PI/4f;
    /// <summary> An offset to raise the model by this amount (it is the offset between the center of the tires and the bottom of the model) </summary>
    public float tireYOffset = 0.2f; 
    
    [Header("Particles")] public List<ParticleSystem> driftParticles;
    public List<ParticleSystem> boostParticles;

    [Header("Items")] public Transform heldItemTransform;

	[Header("Other animations"), SerializeField] private float verticalOffsetInterpolationFactor = 5f;
    public float driftHopHeight = 0.7f;
    public float damageStateSpinSpeed = 5f;
    public float bodyRoll = 1.75f;
#endregion

#region Runtime fields
    private Vector3 lastTrackedPosition;

    private bool showingDriftParticles;
    private bool showingBoostParticles;

    private float verticalOffsetTarget;

    private float damageAngle; // Angle from damage rotation
	private float driftAngle;
	private float driftAngleTarget;
#endregion

#region Utility fields
    private List<Vector3> wheelPositions;
    public List<Vector3> WheelPositions { get {
        if(wheelPositions == null) {
            wheelPositions = new();
            frontTires.ForEach(tire => wheelPositions.Add(tire.transform.position));
            rearTires.ForEach(tire => wheelPositions.Add(tire.transform.position));
        }
        return wheelPositions;
    } }
#endregion

#region EngineComponent shortcuts
    private EngineBase EngineBase => KartCtrl.EngineBase;
    private EngineSteeringWheel EngineSteeringWheel => KartCtrl.EngineSteeringWheel;
    private EngineWheels EngineWheels => KartCtrl.EngineWheels;
    private EngineBoost EngineBoost => KartCtrl.EngineBoost;
#endregion

    private void Start() 
    {
        showingDriftParticles = false;
        driftParticles.ForEach(ps => ps.Stop());

        showingBoostParticles = false;
        boostParticles.ForEach(ps => ps.Stop());
    }

    private void Update() 
    {
        if(KartCtrl == null) return;

        UpdateParticles();
        UpdateTires();
        UpdateModelPosition();

        lastTrackedPosition = transform.position;
    }

    private void UpdateParticles() 
    {
        // Drift particles
        bool shouldShowDriftParticles = EngineWheels.IsDriftEngaged && EngineWheels.DriftDirection != 0;
        if(shouldShowDriftParticles && !showingDriftParticles) { 
            showingDriftParticles = true;
			driftParticles.ForEach(ps => ps.Play());	
		} else if(!shouldShowDriftParticles && showingDriftParticles) { 
            showingDriftParticles = false;
            driftParticles.ForEach(ps => ps.Stop());
		}

        // Boost particles
        if(EngineBoost.ActivelyBoosting && !showingBoostParticles) { 
            showingBoostParticles = true;
			boostParticles.ForEach(ps => ps.Play());	
		} else if(!EngineBoost.ActivelyBoosting && showingBoostParticles) { 
            showingBoostParticles = false;
            boostParticles.ForEach(ps => ps.Stop());
		}
    }

    private void UpdateTires() 
    {
        float turnTheta = EngineWheels.TurnForce*tireTurnAngle;
        frontTireTurners.ForEach(frontTireTurner => {
            Vector3 lea = frontTireTurner.transform.localEulerAngles;
            lea.y = Mathf.Rad2Deg*turnTheta;
            frontTireTurner.transform.localEulerAngles = lea;
        } );

        float frontRotationTheta = Vector3.Distance(lastTrackedPosition, transform.position)/(2*Mathf.PI*frontTireRadius);
        frontTires.ForEach(frontTire => 
            frontTire.transform.RotateAround(frontTire.transform.position, frontTire.transform.right, (frontRotationTheta/Time.deltaTime)*EngineWheels.Momentum)
        );

        float rearRotationTheta = Vector3.Distance(lastTrackedPosition, transform.position)/(2*Mathf.PI*rearTireRadius);
        rearTires.ForEach(rearTire =>
            rearTire.transform.RotateAround(rearTire.transform.position, rearTire.transform.right, (rearRotationTheta/Time.deltaTime)*EngineWheels.Momentum)
        );
    }

    private void UpdateModelPosition() 
    {
        float kartHitbox = KartCtrl.GetComponent<BoxCollider>().size.y;
        float distanceFromGround = EngineWheels.Grounded ? EngineWheels.DistanceFromGround : 0;
        float groundedHeight = -kartHitbox/2f - distanceFromGround + frontTireRadius/2f + tireYOffset;

        if(EngineWheels.DriftTimeElapsed > 0) {
            float t = Mathf.Clamp(EngineWheels.DriftTimeElapsed, 0, EngineWheels.DriftEngageDuration)/EngineWheels.DriftEngageDuration;
            verticalOffsetTarget = groundedHeight + this.driftHopHeight*(-4*(t*t)+4*t);
        } else {
            verticalOffsetTarget = groundedHeight;
        }

        Vector3 localPos = transform.localPosition;
        localPos.y = Mathf.Lerp(localPos.y, verticalOffsetTarget, Time.deltaTime*verticalOffsetInterpolationFactor);
        transform.localPosition = localPos;

        // Rotation - Horizontal
        float driftAngleMin = EngineWheels.DriftAngleExtents.x;
        float driftAngleMax = EngineWheels.DriftAngleExtents.y;
        driftAngleTarget = 0;
		if(EngineWheels.Drifting && EngineWheels.DriftDirection != 0 && Mathf.Abs(KartCtrl.TurnInput.x) >= 0.1f) { 
			driftAngleTarget = Mathf.Sign(EngineWheels.DriftDirection)*driftAngleMin;
			if(EngineSteeringWheel.SteeringWheelMatchesDrift) 
				driftAngleTarget += EngineSteeringWheel.SteeringWheelDirection*(driftAngleMax-driftAngleMin);
		}			
		driftAngle = Mathf.Lerp(driftAngle, driftAngleTarget, 20*Time.deltaTime);
		if(Mathf.Abs(driftAngle) < 0.01f) 
            driftAngle = 0;

        if(EngineBase.EngineStallTime > 0) {
            damageAngle += Time.deltaTime*damageStateSpinSpeed;
            if(damageAngle > Mathf.PI*2) 
                damageAngle -= Mathf.PI*2;
        } else 
            damageAngle = 0;

        transform.forward = KartCtrl.RotateVectorAroundAxis(KartCtrl.KartForward, transform.up, driftAngle + damageAngle);

        // Rotation - Body roll
        float turnTiltAngle = bodyRoll*Mathf.Max(0.2f, KartCtrl.EngineBase.SpeedRatio)*KartCtrl.EngineWheels.TurnForce;

        Vector3 eulerAngles = model.localEulerAngles;
        eulerAngles.x = -turnTiltAngle-90;
        eulerAngles.y = 90;
        eulerAngles.z = -90;
        model.localEulerAngles = eulerAngles;

    }

    private void OnDrawGizmos() 
    {
        if(drawTireRadiusWireFrames) {
            frontTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, frontTireRadius));
            rearTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, rearTireRadius));
        }
    }

    public void SetKartController(KartController kartController) { this.KartCtrl = kartController; }

}