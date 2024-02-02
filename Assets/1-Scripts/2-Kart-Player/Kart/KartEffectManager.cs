using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartEffectManager : KartBehavior
{

    public GameObject bumpParticlePrefab;

    public void SpawnBumpEffect(Vector3 position) 
    {
        GameObject particles = Instantiate(bumpParticlePrefab);
        particles.transform.position = position;
    }

}
