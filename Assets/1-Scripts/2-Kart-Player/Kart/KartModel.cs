using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Manages all visual aspects of the kart model */
public class KartModel : MonoBehaviour
{
    private KartController kartCtrl;

    public bool drawTireRadiusWireFrames = false;
    public float frontTireRadius = 0.5f;
    public List<GameObject> frontTires;
    public float rearTireRadius = 0.5f;
    public List<GameObject> rearTires;
    public List<ParticleSystem> driftParticles;
    private bool showingDriftParticles;

    void Start() 
    {
        showingDriftParticles = false;
        driftParticles.ForEach(ps => ps.Stop());
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

        float arcLength = kartCtrl.TrackSpeed*Time.deltaTime;

        // Tire rotation
        frontTires.ForEach(frontTire => {
            float theta = arcLength/frontTireRadius;
            frontTire.transform.RotateAround(frontTire.transform.position, frontTire.transform.right, theta*kartCtrl.momentum);
        });
        rearTires.ForEach(rearTire => {
            float theta = arcLength/rearTireRadius;
            rearTire.transform.RotateAround(rearTire.transform.position, rearTire.transform.right, theta*kartCtrl.momentum);
        });
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
