using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrictFollow : MonoBehaviour
{

	public Transform subject;
	public float distance = 5f;
	public float height = 3f;

    void Update()
    {
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
