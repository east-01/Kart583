using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;

public class KartControllerFollow : MonoBehaviour
{

	public KartController subject;

	/* ----- Settings fields ---- */
	public float distance = 5f;
	public float height = 3f;
	public float fov = 65;

	/* ----- Runtime fields ----- */
	public Vector3 targetPosition;
	public float displayFov { get; private set; }
	public float fovInterpolationFactor = 3f;

	void Start() 
	{
		displayFov = fov;
	}

    void Update()
    {

		float fov = this.fov;
		KartController kc = subject.GetComponent<KartController>();
		if(kc != null && kc.isActiveAndEnabled && kc.TrackSpeed > 0 && kc.BoostInput && kc.BoostRatio > 0) {
			fov *= 1.3f;
		}

		displayFov = Mathf.Lerp(displayFov, fov, fovInterpolationFactor*Time.deltaTime);
		GetComponent<Camera>().fieldOfView = displayFov;

		targetPosition = GetTargetPosition();

		if(targetPosition.magnitude > 0) {
			transform.position = GetTargetPosition();
			transform.LookAt(kc.gameObject.transform.position);
		}

    }

	public Vector3 GetTargetPosition() 
	{ 
		KartController kc = subject.GetComponent<KartController>();
		float ratio = Mathf.Clamp01(kc.timeSinceLastCollision/5f);
		Vector3 angle = Vector3.zero;
		if(kc.TrackSpeed > 0.1f) {
			angle = Vector3.Lerp(kc.KartForward, kc.GetComponent<Rigidbody>().velocity, ratio*ratio).normalized;
		} else {
			angle = kc.KartForward;
		}

		angle.y = 0;
		if(angle.magnitude == 0) return Vector3.zero;

		int momentum = kc.momentum;
		if(momentum == 0) momentum = 1;

		Vector3 targetPos = kc.gameObject.transform.position - momentum*(angle.normalized * distance);
		targetPos.y = kc.gameObject.transform.position.y + height;

		return targetPos;
	}

}
