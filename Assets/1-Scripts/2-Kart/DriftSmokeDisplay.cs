using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftSmokeDisplay : MonoBehaviour
{
    
	private KartController kc;
	public ParticleSystem particleEmitter;

	void Start()
    {
		try { 
			kc = GetComponentInParent<KartController>();
		} catch { 
			Debug.LogError("Error, DriftSmokeDisplay failed to find a KartController. Disabling.");
			gameObject.transform.parent.gameObject.SetActive(false);
		}
    }

    void Update()
    {
		if(kc.driftParticles) { 
			particleEmitter.Play();	
		} else { 
			particleEmitter.Stop();				
		}
    }
}
