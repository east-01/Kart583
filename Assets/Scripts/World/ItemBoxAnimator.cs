using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBoxAnimator : MonoBehaviour
{

	public AnimationCurve heightAnimation;
	public float height = 1f;
	public float speed = 1f;
	public float animationLength = 2f;

	private Vector3 initialPosition;
	private float lifetime;

	private void Start()
	{
		initialPosition = transform.position;
		lifetime = 0;
	}

	void Update()
    {
		transform.position = initialPosition + Vector3.up*height*heightAnimation.Evaluate(AnimationProgress);
		lifetime += Time.deltaTime;		
    }

	private void OnTriggerEnter(Collider other)
	{
		
	}

	public float AnimationProgress { get { return (lifetime%animationLength)/animationLength; } }

}
