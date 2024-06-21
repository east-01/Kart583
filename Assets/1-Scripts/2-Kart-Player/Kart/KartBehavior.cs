using System;
using FishNet.Connection;
using FishNet.Object;
using Unity.VisualScripting;
using UnityEngine;

/** This class will be a superclass to all going on the Kart. 
    It will check everything on the kart object and collect it.
    It's useful to have this because we can collect all of the separate
      components on the kart together here, then reference them from one thing. */
public class KartBehavior : NetworkBehaviour
{

    // Control
    protected Rigidbody rb;
    protected Collider coll;

#region Kart fields
    protected KartManager kartManager;
    protected KartController kartCtrl;
    protected KartItemManager kartItemManager;
    protected PositionTracker posTracker;
    protected KartVisualsManager kartVisualsManager;
    protected KartAudioManager kartAudioManager;
#endregion

#region Engine fields
    public EngineBase EngineBase { get; private set; }
	public EngineSteeringWheel EngineSteeringWheel { get; private set; }
	public EngineWheels EngineWheels { get; private set; }
	public EngineBoost EngineBoost { get; private set; }
	public KartVectorDrawer KartVectorDrawer { get; private set; }
#endregion

#region Shortcuts
    public KartSettings Settings => kartCtrl.settings;

    public bool CanMove => kartCtrl.CanMove;

    public Vector3 Up => EngineBase.Up;
    public bool Grounded => EngineWheels.Grounded;

    public float SpeedRatio => EngineBase.SpeedRatio;
    public float CurrentMaxSpeed => EngineBase.CurrentMaxSpeed;
    public float TrackSpeed => EngineBase.TrackSpeed;
    public Vector3 TrackVelocity => EngineBase.TrackVelocity;

    public bool ActivelyBoosting => EngineBoost.ActivelyBoosting;
    public float BoostRatio => EngineBoost.BoostRatio;
    public float BoostAmount => EngineBoost.BoostAmount;
    public float BoostDecayTime => EngineBoost.BoostDecayTime;

    public bool IsDriftEngaged => EngineWheels.IsDriftEngaged;
    public int DriftDirection => EngineWheels.DriftDirection;

    public float EngineStallTime => EngineBase.EngineStallTime;
    public float RequiredBoostPercentage => EngineBoost.RequiredBoostPercentage;
#endregion

#region Brain fields
    protected BotDriver botDriver;
    protected BotItemManager botItemManager;
    protected BotPath botPath;
    protected HumanDriver humanDriver;
#endregion

    protected void Awake() 
    {
        // Find manager
        kartManager = LocateManager(gameObject);

        // Objects on the same as manager
        rb = kartManager.GetComponent<Rigidbody>();
        coll = kartManager.GetComponent<Collider>();

        kartCtrl = kartManager.GetComponent<KartController>();
        kartItemManager = kartManager.GetComponent<KartItemManager>();
        posTracker = kartManager.GetComponent<PositionTracker>();
        kartVisualsManager = kartManager.GetComponent<KartVisualsManager>();
        kartAudioManager = kartManager.GetComponent<KartAudioManager>();

        EngineBase = kartManager.GetComponent<EngineBase>();
        EngineSteeringWheel = kartManager.GetComponent<EngineSteeringWheel>();
        EngineWheels = kartManager.GetComponent<EngineWheels>();
        EngineBoost = kartManager.GetComponent<EngineBoost>();
        KartVectorDrawer= kartManager.GetComponent<KartVectorDrawer>();

        // Objects on children of manager
        botDriver = kartManager.GetComponentInChildren<BotDriver>();
        botItemManager = kartManager.GetComponentInChildren<BotItemManager>();
        botPath = kartManager.GetComponentInChildren<BotPath>();
        humanDriver = kartManager.GetComponentInChildren<HumanDriver>();
    } 

    public static KartManager LocateManager(GameObject kartObject) {
        KartManager manager = kartObject.GetComponent<KartManager>();
        if(manager == null) {
            manager = kartObject.GetComponentInParent<KartManager>();
            if(manager == null) throw new InvalidOperationException("KartBehaviour failed to find the KartManager.");
        }
        return manager;
    }

    public KartManager GetKartManager() { return kartManager; }
    public KartController GetKartController() { return kartCtrl; }
    public KartItemManager GetKartItemManager() { return kartItemManager; }
    public PositionTracker GetPositionTracker() { return posTracker; }
    public KartVisualsManager GetKartVisualsManager() { return kartVisualsManager; }
    public BotDriver GetBotDriver() { return botDriver; }
    public BotPath GetBotPath() { return botPath; }
    public HumanDriver GetHumanDriver() { return humanDriver; }

    public PlayerObject OwnerPlayerObject { get { return kartManager.POIGDelegate.owner; } }

}
