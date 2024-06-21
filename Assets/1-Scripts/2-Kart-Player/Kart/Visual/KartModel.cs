using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/** Manages all visual aspects of the kart model */
public class KartModel : MonoBehaviour
{
    private KartController kartCtrl;

#region Editor fields
    /* Editor fields */
    [Header("Tires")] public bool drawTireRadiusWireFrames = false;
    public float frontTireRadius = 0.5f;
    public List<GameObject> frontTires;
    public List<GameObject> frontTireTurners;
    public float rearTireRadius = 0.5f;
    public List<GameObject> rearTires;
    
    [Header("Particles")] public List<ParticleSystem> driftParticles;
    public List<ParticleSystem> boostParticles;

    [Header("Items")] public Transform heldItemTransform;

	[Header("Other animations")] public float driftHopHeight = 0.7f;
    public float damageStateSpinSpeed = 5f;
#endregion

#region Runtime fields
    /* Runtime fields */
    private Vector3 lastTrackedPosition;

    private bool showingDriftParticles;
    private bool showingBoostParticles;

    private float damageAngle; // Angle from damage rotation
	private float driftAngle;
	private float driftAngleTarget;
#endregion

#region EngineComponent shortcuts
    private EngineBase EngineBase => kartCtrl.EngineBase;
    private EngineSteeringWheel EngineSteeringWheel => kartCtrl.EngineSteeringWheel;
    private EngineWheels EngineWheels => kartCtrl.EngineWheels;
    private EngineBoost EngineBoost => kartCtrl.EngineBoost;
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
        if(kartCtrl == null) return;

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

        // BLog.Highlight($"should: {EngineBoost.ActivelyBoosting} is: {showingBoostParticles}");
        // BLog.Highlight($"can move: {kartCtrl.CanMove} boost: {EngineBoost.BoostAmount} boosting: {EngineBoost.Boosting}");
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
        float turnTheta = EngineSteeringWheel.SteeringWheelDirection*(Mathf.PI/4f);
        frontTireTurners.ForEach(frontTireTurner =>
            frontTireTurner.transform.localRotation = Quaternion.AngleAxis(Mathf.Rad2Deg*turnTheta, Vector3.forward)
        );

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
        // Vertical position
        float modelHeight = GetComponentInChildren<MeshFilter>().mesh.bounds.size.z;
        float groundedHeight = (EngineWheels.Grounded ? -EngineWheels.DistanceFromGround : 0) + modelHeight/2f;

        Vector3 localPos = transform.localPosition;
        if(EngineWheels.DriftTimeElapsed > 0) {
            float t = Mathf.Clamp(EngineWheels.DriftTimeElapsed, 0, EngineWheels.DriftEngageDuration)/EngineWheels.DriftEngageDuration;
            localPos.y = groundedHeight + this.driftHopHeight*(-4*(t*t)+4*t);
        } else {
            localPos.y = groundedHeight;
        }
        transform.localPosition = localPos;

        // Rotation
        float driftAngleMin = EngineWheels.DriftAngleExtents.x;
        float driftAngleMax = EngineWheels.DriftAngleExtents.y;
        driftAngleTarget = 0;
		if(EngineWheels.Drifting && EngineWheels.DriftDirection != 0 && Mathf.Abs(kartCtrl.TurnInput.x) >= 0.1f) { 
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

        transform.forward = kartCtrl.RotateVectorAroundAxis(kartCtrl.KartForward, transform.up, driftAngle + damageAngle);
    }

    private void OnDrawGizmos() 
    {
        if(drawTireRadiusWireFrames) {
            frontTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, frontTireRadius));
            rearTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, rearTireRadius));
        }
    }

    public void SetKartController(KartController kartController) { this.kartCtrl = kartController; }

}