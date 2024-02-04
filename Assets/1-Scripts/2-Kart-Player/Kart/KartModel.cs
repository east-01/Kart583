using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Manages all visual aspects of the kart model */
public class KartModel : MonoBehaviour
{
    private KartController kartCtrl;

    [Header("Tires")] public bool drawTireRadiusWireFrames = false;
    public float frontTireRadius = 0.5f;
    public List<GameObject> frontTires;
    public List<GameObject> frontTireTurners;
    public float rearTireRadius = 0.5f;
    public List<GameObject> rearTires;
    
    [Header("Particles")] public List<ParticleSystem> driftParticles;
    private bool showingDriftParticles;

    public List<ParticleSystem> boostParticles;
    private bool showingBoostParticles;

    private Vector3 lastTrackedPosition;

    void Start() 
    {
        showingDriftParticles = false;
        driftParticles.ForEach(ps => ps.Stop());

        showingBoostParticles = false;
        boostParticles.ForEach(ps => ps.Stop());
    }

    void Update() 
    {
        if(kartCtrl == null) return;

        // Drift particles
        if(kartCtrl.driftParticles && !showingDriftParticles) { 
            showingDriftParticles = true;
			driftParticles.ForEach(ps => ps.Play());	
		} else if(!kartCtrl.driftParticles && showingDriftParticles) { 
            showingDriftParticles = false;
            driftParticles.ForEach(ps => ps.Stop());
		}

        // Boost particles
        if(kartCtrl.ActivelyBoosting && !showingBoostParticles) { 
            showingBoostParticles = true;
			boostParticles.ForEach(ps => ps.Play());	
		} else if(!kartCtrl.ActivelyBoosting && showingBoostParticles) { 
            showingBoostParticles = false;
            boostParticles.ForEach(ps => ps.Stop());
		}

        // Tire rotation
        float turnTheta = kartCtrl.steeringWheelDirection*(Mathf.PI/4f);
        frontTireTurners.ForEach(frontTireTurner =>
            frontTireTurner.transform.localRotation = Quaternion.AngleAxis(Mathf.Rad2Deg*turnTheta, Vector3.forward)
        );

        float frontRotationTheta = Vector3.Distance(lastTrackedPosition, transform.position)/(2*Mathf.PI*frontTireRadius);
        frontTires.ForEach(frontTire =>
            frontTire.transform.RotateAround(frontTire.transform.position, frontTire.transform.right, (frontRotationTheta/Time.deltaTime)*kartCtrl.momentum)
        );

        float rearRotationTheta = Vector3.Distance(lastTrackedPosition, transform.position)/(2*Mathf.PI*rearTireRadius);
        rearTires.ForEach(rearTire =>
            rearTire.transform.RotateAround(rearTire.transform.position, rearTire.transform.right, (rearRotationTheta/Time.deltaTime)*kartCtrl.momentum)
        );

        lastTrackedPosition = transform.position;
    }

    void OnDrawGizmos() 
    {
        if(drawTireRadiusWireFrames) {
            frontTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, frontTireRadius));
            rearTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, rearTireRadius));
        }
    }

    public void SetKartController(KartController kartController) { this.kartCtrl = kartController; }

}
