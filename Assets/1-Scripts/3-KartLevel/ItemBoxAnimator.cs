using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBoxAnimator : MonoBehaviour
{

	public AnimationCurve heightAnimation;
	public float height = 1f;
	public float speed = 1f;
	public float animationLength = 2f;
	public float cooldownTime = 1.5f;

	public GameObject powerupTexture;
	public GameObject itemLight;
	public ParticleSystem particles;

	private MeshRenderer meshRenderer;
	private Vector3 initialPosition;
	private float lifetime;
	private float cooldownCounter = 0;

	private void Start()
	{
		initialPosition = transform.position;
		lifetime = 0;

		meshRenderer = GetComponent<MeshRenderer>();
	}

	void Update()
    {
		if(cooldownCounter > 0) {
			cooldownCounter -= Time.deltaTime;
			if(cooldownCounter <= 0) Show(true);
		}

		transform.position = initialPosition + Vector3.up*height*heightAnimation.Evaluate(AnimationProgress);
		lifetime += Time.deltaTime;		

		float theta = 2*Mathf.PI*AnimationProgress;
		Vector3 rotationAxis = new Vector3(Mathf.Cos(theta), Mathf.PI/4, Mathf.Sin(theta)).normalized;
		transform.rotation = Quaternion.LookRotation(rotationAxis, Vector3.up);

    }

	private void OnTriggerEnter(Collider other)
	{
		if(cooldownTime > 0) { 
			KartManager pm = other.GetComponent<KartManager>();
			bool awardedItem = pm != null && pm.GetKartItemManager().HitItemBox(other.gameObject);

			Show(false);
			
			if(!awardedItem) cooldownTime /= 2f; // Halve the cooldown time if we didn't award an item.
		}
	}

	public void Show(bool show) 
	{ 
		cooldownCounter = show ? 0 : cooldownTime;
		if(!show) particles.Play();

		meshRenderer.enabled = show;
		powerupTexture.SetActive(show);
		itemLight.SetActive(show);
	}

	public float AnimationProgress { get { return (lifetime%animationLength)/animationLength; } }

}
