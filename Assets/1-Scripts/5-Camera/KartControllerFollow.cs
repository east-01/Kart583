using System.Collections;
using System.Collections.Generic;
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
	public Vector3 targetAngle;

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

		Vector3 currentAngle = kc.RemoveUpComponent(subject.gameObject.transform.position-transform.position).normalized;
		Vector3 targetAngle = Vector3.zero;

		if(kc.TrackSpeed > 0.1f) {
			float ratio = Mathf.Clamp01(kc.timeSinceLastCollision/5f);
			targetAngle = kc.RemoveUpComponent(Vector3.Lerp(kc.GetComponent<Rigidbody>().velocity, kc.KartForward, ratio*ratio).normalized);
		} else {
			targetAngle = kc.KartForward;
		}

        Vector3 angle = Vector3.Lerp(currentAngle, targetAngle, 0.3f);
		Vector3 targetPos = kc.gameObject.transform.position - (angle.normalized * distance);
		targetPos.y = kc.gameObject.transform.position.y + height;

		transform.position = targetPos;
		transform.LookAt(kc.gameObject.transform.position);

    }

}
