using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class TailLight : MonoBehaviour
{
    [SerializeField] private KartModel kartModel;
    [SerializeField] private float dimValue;
    [SerializeField] private float activeValue;

    private new Light light;
    private float derivativeTimeElapsed;
    private readonly float derivativeTimeRequired = 0.25f;

    private void Awake() 
    {
        light = GetComponent<Light>();
    }

    void Update()
    {   
        if(kartModel == null)
            return;
        KartController kartController = kartModel.KartCtrl;
        if(kartController == null)
            return;

        if(kartController.EngineBase.TrackSpeedDerivative < 0)
            derivativeTimeElapsed += Time.deltaTime;
        else
            derivativeTimeElapsed = 0;

        bool activateTailLights = kartController.ThrottleInput < 0 || derivativeTimeElapsed > derivativeTimeRequired;
        light.range = activateTailLights ? activeValue : dimValue;
    }
}
