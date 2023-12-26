using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriftSmokeDisplay : KartBehavior
{
    
	public ParticleSystem particleEmitter;

    void Update()
    {
		if(kartCtrl.driftParticles) { 
			particleEmitter.Play();	
		} else { 
			particleEmitter.Stop();				
		}
    }
}
