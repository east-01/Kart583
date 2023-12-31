using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrictFollow : MonoBehaviour
{

	public Transform subject;
	public float distance = 5f;
	public float height = 3f;

	public float fov = 65;
	public float displayFov { get; private set; }
	public float fovInterpolationFactor = 3f;

    void Update()
    {

		float fov = this.fov;
		KartController kc = subject.GetComponent<KartController>();
		if(kc != null && kc.isActiveAndEnabled && kc.TrackSpeed > 0 && kc.IsBoosting() && kc.GetBoostAmount() > 0) {
			fov *= 1.3f;
		}

		displayFov = Mathf.Lerp(displayFov, fov, fovInterpolationFactor*Time.deltaTime);
		GetComponent<Camera>().fieldOfView = displayFov;

		transform.position = GetTargetPosition();
		transform.LookAt(subject.position);

    }

	public Vector3 GetTargetPosition() 
	{ 
		Vector3 angle = subject.forward;
		KartController kc = subject.GetComponent<KartController>();
		if(kc != null && kc.kartForward.magnitude > 0) angle = kc.kartForward;
		angle.y = 0;

		Vector3 targetPos = subject.position - (angle.normalized * distance);
		targetPos.y = subject.position.y + height;

		return targetPos;
	}

}
