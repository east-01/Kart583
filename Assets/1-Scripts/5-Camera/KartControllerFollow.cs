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
	public float angleSpeed = 6.75f;

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

		KartController kc = subject;
		float fov = this.fov;
		if(kc != null && kc.isActiveAndEnabled && kc.TrackSpeed > 0 && kc.BoostInput && kc.BoostRatio > 0) {
			fov *= 1.3f;
		}

		displayFov = Mathf.Lerp(displayFov, fov, fovInterpolationFactor*Time.deltaTime);
		GetComponent<Camera>().fieldOfView = displayFov;

		Vector3 currentAngle = kc.RemoveUpComponent(subject.gameObject.transform.position-transform.position).normalized;
        Vector3 angle = Vector3.Lerp(currentAngle, kc.KartForward, angleSpeed*Time.deltaTime);

		Vector3 targetPos = kc.gameObject.transform.position - (angle.normalized * distance);
		targetPos.y = kc.gameObject.transform.position.y + height;

		transform.position = targetPos;
		transform.LookAt(kc.gameObject.transform.position);

    }

}
