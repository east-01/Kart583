using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBoxAnimator : MonoBehaviour
{

	public AnimationCurve heightAnimation;
	public float height = 1f;
	public float speed = 1f;

	private Vector3 initialPosition;

	private void Awake()
	{
		initialPosition = transform.position;
	}

	void Update()
    {
     
		transform.position =- Vector3.up*heightAnimation.Evaluate();
		
    }
}
