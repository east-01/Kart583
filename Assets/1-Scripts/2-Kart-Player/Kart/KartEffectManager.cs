using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartEffectManager : KartBehavior
{

    public List<ParticleSystem> driftParticles;
    private bool showingDriftParticles;
    public GameObject bumpParticlePrefab;

    void Start() 
    {
        showingDriftParticles = false;
        driftParticles.ForEach(ps => ps.Stop());
    }

    void Update() 
    {
        if(kartCtrl.driftParticles && !showingDriftParticles) { 
            showingDriftParticles = true;
			driftParticles.ForEach(ps => ps.Play());	
		} else if(!kartCtrl.driftParticles && showingDriftParticles) { 
            showingDriftParticles = false;
            driftParticles.ForEach(ps => ps.Stop());
		}
    }

    public void SpawnBumpEffect(Vector3 position) 
    {
        GameObject particles = Instantiate(bumpParticlePrefab);
        particles.transform.position = position;
    }

}
