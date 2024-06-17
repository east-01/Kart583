using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    public Transform heldItemTransform;

    public bool drawHitbox = false;

    public float[] verticalOffsets = new float[2];

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

        // Vertical position
        float modelHeight = GetComponentInChildren<MeshFilter>().mesh.bounds.size.z;
        verticalOffsets[0] = (kartCtrl.Grounded() ? -kartCtrl.distanceFromGround : 0) + modelHeight/2f;

        float t = kartCtrl.driftEngageTime/kartCtrl.driftEngageDuration;
        verticalOffsets[1] = verticalOffsets[0] + kartCtrl.driftHopHeight*(-4*(t*t)+4*t);

        Vector3 localPos = transform.localPosition;
        if(kartCtrl.driftEngageTime > 0) {
            localPos.y = verticalOffsets[1];
        } else {
            localPos.y = verticalOffsets[0];
        }
        transform.localPosition = localPos;

        if(Input.GetKeyDown(KeyCode.J)) {
            ToggleHitBox();   
        }

        lastTrackedPosition = transform.position;
    }

    void OnDrawGizmos() 
    {
        if(drawTireRadiusWireFrames) {
            frontTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, frontTireRadius));
            rearTires.ForEach(t => Gizmos.DrawWireSphere(t.transform.position, rearTireRadius));
        }

        if(drawHitbox) {
            BoxCollider boxCollider = kartCtrl.GetComponent<BoxCollider>();
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + boxCollider.center, boxCollider.size);
        }
    }

    public void SetKartController(KartController kartController) { this.kartCtrl = kartController; }

    public void ToggleHitBox() {
        if(kartCtrl == null) {
            Debug.LogError("Can't show hitbox, kart controller is null");
            return;
        }
        BLog.Highlight("Drawing hitbox: " + drawHitbox);
        drawHitbox = !drawHitbox;
        GetComponentInChildren<MeshRenderer>().enabled = drawHitbox;
    }

}
