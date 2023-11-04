using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrictFollow : MonoBehaviour
{

	public Transform subject;
	public float distance = 5f;
	public float height = 3f;

    void Start()
    {

    }

    void Update()
    {
		Vector3 targetPos = subject.position - (subject.forward * distance);
		targetPos.y = subject.position.y + height;
		transform.position = targetPos;
		transform.LookAt(subject.position);
    }
}
