using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** This class will be a superclass to all going on the Kart. 
    It will check everything on the kart object and collect it.
    It's useful to have this because we can collect all of the separate
      components on the kart together here, then reference them from one thing. */
public class KartBehavior : MonoBehaviour
{

    // Control
    protected Rigidbody rb;
    protected Collider coll;

    protected KartManager kartManager;
    protected KartController kartCtrl;
    protected KartStateManager kartStateManager;
    protected PositionTracker posTracker;

    // Brain
    protected BotDriver botDriver;
    protected BotItemManager botItemManager;
    protected BotPath botPath;
    protected HumanDriver humanDriver;

    void Awake() 
    {

        // Find manager
        kartManager = GetComponent<KartManager>();
        if(kartManager == null) {
            kartManager = GetComponentInParent<KartManager>();
            if(kartManager == null) throw new InvalidOperationException("KartBehaviour failed to find the KartManager.");
        }

        // Objects on the same as manager
        rb = kartManager.GetComponent<Rigidbody>();
        coll = kartManager.GetComponent<Collider>();

        kartCtrl = kartManager.GetComponent<KartController>();
        kartStateManager = kartManager.GetComponent<KartStateManager>();
        posTracker = kartManager.GetComponent<PositionTracker>();

        // Objects on children of manager
        botDriver = kartManager.GetComponentInChildren<BotDriver>();
        botItemManager = kartManager.GetComponentInChildren<BotItemManager>();
        botPath = kartManager.GetComponentInChildren<BotPath>();
        humanDriver = kartManager.GetComponentInChildren<HumanDriver>();

    }

    public KartManager GetKartManager() { return kartManager; }
    public KartController GetKartController() { return kartCtrl; }
    public KartStateManager GetKartStateManager() { return kartStateManager; }
    public PositionTracker GetPositionTracker() { return posTracker; }
    public BotDriver GetBotDriver() { return botDriver; }
    public BotPath GetBotPath() { return botPath; }
    public HumanDriver GetHumanDriver() { return humanDriver; }

}
